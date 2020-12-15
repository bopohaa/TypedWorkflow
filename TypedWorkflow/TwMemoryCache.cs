using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TypedWorkflow.Common;

namespace TypedWorkflow
{
    public class TwMemoryCache<Tk, Tv> : ICache<Tk, Tv>
    {
        private readonly int _expirationScanFrequencySec;
        private readonly ConcurrentDictionary<Tk, CacheEntry> _entries;
        private long _nextExpirationScan;

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

        public TwMemoryCache(int expiration_scan_frequency_sec = 600)
        {
            _expirationScanFrequencySec = expiration_scan_frequency_sec;
            _nextExpirationScan = TwCacheTimer.NowSec + _expirationScanFrequencySec;
            _entries = new ConcurrentDictionary<Tk, CacheEntry>();
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
            if (now_sec > nextExpirationScan && Interlocked.CompareExchange(ref _nextExpirationScan, now_sec + _expirationScanFrequencySec, nextExpirationScan) == nextExpirationScan)
            {
                Task.Factory.StartNew(ScanForExpiredItems, this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void ScanForExpiredItems(object state)
        {
            var cache = (TwMemoryCache<Tk, Tv>)state;
            var nowSec = TwCacheTimer.NowSec;
            foreach (var entry in cache._entries.ToArray())
            {
                if (entry.Value.IsExpired(nowSec))
                    cache._entries.TryRemove(entry.Key, out var _);
            }
        }
    }
}
