using System;

using ProactiveCache;

namespace TypedWorkflow.Common
{
    internal class CacheSettings
    {
        public readonly bool BatchedCache;
        public readonly Type OriginalKeyType;
        public readonly Type OriginalValueType;
        public readonly Type[] KeyTypes;
        public readonly Type[] ValueTypes;
        public readonly TimeSpan ExpirationTtl;
        public readonly TimeSpan OutdateTtl;
        public readonly object ExternalCache;

        private CacheSettings(bool batched_cache, Type original_key_type, Type[] key_types, Type original_value_type, Type[] value_types, TimeSpan expiration_ttl, TimeSpan outdate_ttl, object external_cache)
        {
            BatchedCache = batched_cache;
            OriginalKeyType = original_key_type;
            KeyTypes = key_types;
            OriginalValueType = original_value_type;
            ValueTypes = value_types;
            ExpirationTtl = expiration_ttl;
            OutdateTtl = outdate_ttl;
            ExternalCache = external_cache;
        }

        public static CacheSettings Create<Tk, Tv>(bool batched_cache, TimeSpan expiration_ttl, TimeSpan outdate_ttl, ICache<Tk, Tv> external_cache = null)
            => Create(typeof(Tk), typeof(Tv), batched_cache, expiration_ttl, outdate_ttl, external_cache);

        public static CacheSettings Create(Type key, Type value, bool batched_cache, TimeSpan expiration_ttl, TimeSpan outdate_ttl, object external_cache = null)
        {
            if (!ValueTupleUtils.TryUnwrap(key, out var keyTypes))
                keyTypes = new[] { key };
            if (!ValueTupleUtils.TryUnwrap(value, out var valueTypes))
                valueTypes = new[] { value };

            return new CacheSettings(batched_cache, key, keyTypes, value, valueTypes, expiration_ttl, outdate_ttl, external_cache);
        }
    }
}
