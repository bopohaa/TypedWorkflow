using System;

using ProactiveCache;

namespace TypedWorkflow.Common
{
    internal class CacheSettings
    {
        public readonly bool BatchedCache;
        public readonly Type[] KeyTypes;
        public readonly Type[] ValueTypes;
        public readonly TimeSpan ExpirationTtl;
        public readonly TimeSpan OutdateTtl;
        public readonly object ExternalCache;

        private CacheSettings(bool batched_cache, Type[] key_types, Type[] value_types, TimeSpan expiration_ttl, TimeSpan outdate_ttl, object external_cache)
        {
            BatchedCache = batched_cache;
            KeyTypes = key_types;
            ValueTypes = value_types;
            ExpirationTtl = expiration_ttl;
            OutdateTtl = outdate_ttl;
            ExternalCache = external_cache;
        }

        public static CacheSettings Create<Tk, Tv>(bool batched_cache, TimeSpan expiration_ttl, TimeSpan outdate_ttl, ICache<Tk, Tv> external_cache = null)
        {
            var keyType = typeof(Tk);
            var valueType = typeof(Tv);
            if (!ValueTupleUtils.TryUnwrap(keyType, out var keyTypes))
                keyTypes = new[] { keyType };
            if (!ValueTupleUtils.TryUnwrap(valueType, out var valueTypes))
                valueTypes = new[] { valueType };

            return new CacheSettings(batched_cache, keyTypes, valueTypes, expiration_ttl, outdate_ttl, external_cache);
        }
    }
}
