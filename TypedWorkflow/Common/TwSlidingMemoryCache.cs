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

        public static TwSlidingMemoryCache<Tk, Tv> CreateCache<Tk, Tv>(this Options<Tk, Tv> options, Get<Tk, Tv> getter)
            => new TwSlidingMemoryCache<Tk, Tv>(getter, options.ExpireTtl, options.OutdateTtl, options.ExternalCache);
    }

    internal static class TwMemoryCache<Tk>
    {
        static object[] _locks = Enumerable.Range(0, 256).Select(_ => new object()).ToArray();

        public static object GetLock(int hash) => _locks[hash % 256];
    }

    internal class TwSlidingMemoryCache<Tk, Tv>
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

        public TwSlidingMemoryCache(TwCache.Get<Tk, Tv> get, TimeSpan expire_ttl, ICache<Tk, Tv> external_cache = null) : this(get, expire_ttl, TimeSpan.Zero, external_cache) { }

        public TwSlidingMemoryCache(TwCache.Get<Tk, Tv> get, TimeSpan expire_ttl, TimeSpan outdate_ttl, ICache<Tk, Tv> external_cache = null)
        {
            if (outdate_ttl > expire_ttl)
                throw new ArgumentException("Must be less expire ttl", nameof(outdate_ttl));

            _cache = external_cache ?? new TwMemoryCache<Tk, Tv>();
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
}
