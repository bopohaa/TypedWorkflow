using System;

using ProactiveCache;

namespace TypedWorkflow.Common
{
    internal class ExecutionDomainSettings
    {
        public readonly Type OriginalKeyType;
        public readonly Type OriginalValueType;
        public readonly Type[] KeyTypes;
        public readonly Type[] ValueTypes;
        public readonly (bool Batched, TimeSpan ExpirationTtl, TimeSpan OutdateTtl, object ExternalCache)? Caching;

        private ExecutionDomainSettings(Type original_key_type, Type[] key_types, Type original_value_type, Type[] value_types, (bool Batched, TimeSpan ExpirationTtl, TimeSpan OutdateTtl, object ExternalCache)? caching)
        {
            OriginalKeyType = original_key_type;
            KeyTypes = key_types;
            OriginalValueType = original_value_type;
            ValueTypes = value_types;
            Caching = caching;
        }

        public static ExecutionDomainSettings Create<Tk, Tv>((bool batched, TimeSpan expirationTtl, TimeSpan outdateTtl, ICache<Tk, Tv> externalCache)? caching)
            => Create(typeof(Tk), typeof(Tv), caching);

        public static ExecutionDomainSettings Create(Type key, Type value, (bool batched, TimeSpan expirationTtl, TimeSpan outdateTtl, object externalCache)? caching)
        {
            if (!ValueTupleUtils.TryUnwrap(key, out var keyTypes))
                keyTypes = new[] { key };
            if (!ValueTupleUtils.TryUnwrap(value, out var valueTypes))
                valueTypes = new[] { value };

            return new ExecutionDomainSettings(key, keyTypes, value, valueTypes, caching);
        }
    }
}
