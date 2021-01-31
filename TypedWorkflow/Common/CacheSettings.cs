using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow.Common
{
    internal class CacheSettings
    {
        private readonly bool _batchedCache;
        private readonly Type _keyType;
        private readonly Type _valueType;
        private readonly TimeSpan _expirationTtl;
        private readonly TimeSpan _outdateTtl;

        public CacheSettings(bool batched_cache, Type key_type, Type value_type, TimeSpan expiration_ttl, TimeSpan outdate_ttl)
        {
            _batchedCache = batched_cache;
            _keyType = key_type;
            _valueType = value_type;
            _expirationTtl = expiration_ttl;
            _outdateTtl = outdate_ttl;
        }
    }
}
