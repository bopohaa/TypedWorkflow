using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{

    internal static class TwCache
    {
        public delegate ValueTask<Tv> Get<Tk, Tv>(Tk key);

        internal struct Options<Tk, Tv>
        {
            public readonly ICache<Tk, Tv> ExternalCache;
            public readonly TimeSpan ExpireTtl;
            public readonly TimeSpan OutdateTtl;

            public Options(ICache<Tk, Tv> external_cache, TimeSpan expire_ttl, TimeSpan outdate_ttl)
            {
                ExternalCache = external_cache;
                ExpireTtl = expire_ttl;
                OutdateTtl = outdate_ttl;
            }
        }

        public static Options<Tk, Tv> CreateOptions<Tk, Tv>(TimeSpan expire_ttl, TimeSpan outdate_ttl, ICache<Tk, Tv> external_cache = null)
            => new Options<Tk, Tv>(external_cache, expire_ttl, outdate_ttl);

        public static TwMemoryCache<Tk, Tv> CreateCache<Tk, Tv>(this Options<Tk, Tv> options, Get<Tk, Tv> getter)
            => new TwMemoryCache<Tk, Tv>(getter, options.ExpireTtl, options.OutdateTtl, options.ExternalCache);
    }

    internal static class TwMemoryCache<Tk>
    {
        static object[] _locks = Enumerable.Range(0, 256).Select(_ => new object()).ToArray();

        public static object GetLock(int hash) => _locks[hash % 256];
    }

    internal class TwMemoryCache<Tk, Tv>
    {
        private class TwCacheEntry : ICacheEntry<Tv>
        {
            private const long OUTDATED_FLAG = 0x4000000000000000;
            private long _outdatedSec;
            private object _value;
            private bool _hasValue;

            private readonly Task<Tv> _valueAsTask;

            public ValueTask<Tv> GetValue() => _hasValue ?
                new ValueTask<Tv>((Tv)_value) :
                new ValueTask<Tv>(_valueAsTask);

            internal TwCacheEntry(Task<Tv> value, TimeSpan outdated_ttl)
            {
                _valueAsTask = value;
                _outdatedSec = TwCacheTimer.NowSec + outdated_ttl.Ticks / TimeSpan.TicksPerSecond;
            }

            internal bool Outdated()
            {
                var outdated = Volatile.Read(ref _outdatedSec);
                if (outdated > TwCacheTimer.NowSec)
                    return false;

                return Interlocked.CompareExchange(ref _outdatedSec, outdated | OUTDATED_FLAG, outdated) == outdated;
            }

            internal void Reset(Tv value, TimeSpan outdated_ttl)
            {
                Volatile.Write(ref _value, value);
                Volatile.Write(ref _hasValue, true);
                Volatile.Write(ref _outdatedSec, TwCacheTimer.NowSec + outdated_ttl.Ticks / TimeSpan.TicksPerSecond);
            }

            internal void Reset()
                => _outdatedSec ^= OUTDATED_FLAG;
        }

        private readonly ICache<Tk, Tv> _cache;
        private readonly TimeSpan _outdateTtl;
        private readonly TimeSpan _expireTtl;
        private readonly TwCache.Get<Tk, Tv> _get;

        public TwMemoryCache(TwCache.Get<Tk, Tv> get, TimeSpan expire_ttl, ICache<Tk, Tv> external_cache = null) : this(get, expire_ttl, TimeSpan.Zero, external_cache) { }

        public TwMemoryCache(TwCache.Get<Tk, Tv> get, TimeSpan expire_ttl, TimeSpan outdate_ttl, ICache<Tk, Tv> external_cache = null)
        {
            if (outdate_ttl > expire_ttl)
                throw new ArgumentException("Must be less expire ttl", nameof(outdate_ttl));

            _cache = external_cache ?? new TwInternalMemoryCache<Tk, Tv>();
            _outdateTtl = outdate_ttl;
            _expireTtl = expire_ttl;
            _get = get;
        }

        public ValueTask<Tv> Get(Tk key)
        {
            if (!_cache.TryGet(key, out var res))
                return Add(key);

            var entry = (TwCacheEntry)res;
            if (_outdateTtl.Ticks > 0 && entry.Outdated())
                return UpdateOutdated(key, entry);

            return entry.GetValue();
        }

        private ValueTask<Tv> Add(Tk key)
        {
            TaskCompletionSource<Tv> completion;
            TwCacheEntry entry;
            var lockObject = TwMemoryCache<Tv>.GetLock(key.GetHashCode());
            lock (lockObject)
            {
                if (_cache.TryGet(key, out var res))
                    return ((TwCacheEntry)res).GetValue();

                completion = new TaskCompletionSource<Tv>();
                entry = new TwCacheEntry(completion.Task, _outdateTtl);
                _cache.Set(key, entry, _expireTtl);
            }
            FireAndForge(_get, key, completion);

            return entry.GetValue();
        }

        private async ValueTask<Tv> UpdateOutdated(Tk key, TwCacheEntry entry)
        {
            try
            {
                var res = await _get(key);
                entry.Reset(res, _outdateTtl);
                _cache.Set(key, entry, _expireTtl);
                return res;
            }
            catch
            {
                entry.Reset();
                throw;
            }
        }

        private static async void FireAndForge(TwCache.Get<Tk, Tv> get, Tk key, TaskCompletionSource<Tv> completion)
        {
            try
            {
                var res = await get(key);
                completion.SetResult(res);
            }
            catch (TaskCanceledException)
            {
                completion.SetCanceled();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }
    }

    internal static class TwCacheTimer
    {
        private static ulong _nowMs;
        public static ulong NowMs => _nowMs;
        private static uint _nowSec;
        public static uint NowSec => _nowSec;

        private readonly static System.Timers.Timer _timer;

        static TwCacheTimer()
        {
            _nowMs = (uint)Environment.TickCount;
            _nowSec = (uint)(_nowMs / 1000);
            _timer = new System.Timers.Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (sender, e) =>
            {
                var now = (uint)Environment.TickCount;
                var t = _nowMs;
                var f = t % 0x0100000000;
                if (now < f)
                    t += 0x0100000000;
                _nowMs = t & 0xffffffff00000000 | now;
                _nowSec = (uint)(_nowMs / 1000);
            };
            _timer.Start();
        }
    }

    internal class TwInternalMemoryCache<Tk, Tv> : ICache<Tk, Tv>
    {
        private const int ExpirationScanFrequencySec = 600;
        private readonly ConcurrentDictionary<Tk, CacheEntry> _entries = new ConcurrentDictionary<Tk, CacheEntry>();
        private long _nextExpirationScan = TwCacheTimer.NowSec + ExpirationScanFrequencySec;

        private struct CacheEntry
        {
            private readonly long _expireAt;
            public readonly ICacheEntry<Tv> Value;

            public CacheEntry(ICacheEntry<Tv> value, TimeSpan expire_ttl, long now_sec)
            {
                _expireAt = now_sec + expire_ttl.Ticks / TimeSpan.TicksPerSecond;
                Value = value;
            }

            public bool IsExpired(long now_sec) => now_sec > _expireAt;
        }

        public void Set(Tk key, ICacheEntry<Tv> value, TimeSpan expiration_time)
        {
            var nowSec = TwCacheTimer.NowSec;
            var entry = new CacheEntry(value, expiration_time, nowSec);
            _entries.AddOrUpdate(key, entry, (k, v) => entry);

            StartScanForExpiredItemsIfNeeded(nowSec);
        }

        public bool TryGet(Tk key, out ICacheEntry<Tv> value)
        {
            if (!_entries.TryGetValue(key, out var entry) || entry.IsExpired(TwCacheTimer.NowSec))
            {
                value = default;
                return false;
            }

            value = entry.Value;
            return true;
        }

        private void StartScanForExpiredItemsIfNeeded(long now_sec)
        {
            var nextExpirationScan = Volatile.Read(ref _nextExpirationScan);
            if (now_sec > nextExpirationScan && Interlocked.CompareExchange(ref _nextExpirationScan, now_sec + ExpirationScanFrequencySec, nextExpirationScan) == nextExpirationScan)
            {
                Task.Factory.StartNew(ScanForExpiredItems, this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void ScanForExpiredItems(object state)
        {
            var cache = (TwInternalMemoryCache<Tk, Tv>)state;
            var nowSec = TwCacheTimer.NowSec;
            foreach (var entry in cache._entries.ToArray())
            {
                if (entry.Value.IsExpired(nowSec))
                    cache._entries.TryRemove(entry.Key, out var _);
            }
        }
    }
}
