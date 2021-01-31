using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using TypedWorkflow.Common;

namespace TypedWorkflow
{
    public class TwContainerBuilder
    {
        private HashSet<Assembly> _assemblies;
        private HashSet<string> _namespaces;
        private IResolver _resolver;

        private bool _cachePresent;
        private object _externalCache;
        private TimeSpan _outdateTtl;
        private TimeSpan _expireTtl;

        public TwContainerBuilder()
        {
            _assemblies = new HashSet<Assembly>();
            _namespaces = new HashSet<string>();
        }

        public TwContainerBuilder AddAssemblies(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                _assemblies.Add(assembly);

            return this;
        }
        public TwContainerBuilder AddNamespaces(params string[] namespaces)
        {
            foreach (var ns in namespaces)
                _namespaces.Add(ns);

            return this;
        }

        public TwContainerBuilder RegisterExternalDi(IResolver resolver)
        {
            _resolver = resolver;

            return this;
        }

        public TwContainerBuilder SetCache<Tkey, Tval>(TimeSpan expire_ttl, TimeSpan outdate_ttl, ProactiveCache.ICache<Tkey, Tval> memory_cache)
        {
            if (_cachePresent)
                throw new NotSupportedException("Only one cache present on this time");

            _expireTtl = expire_ttl;
            _outdateTtl = outdate_ttl;
            _externalCache = memory_cache;
            _cachePresent = true;

            return this;
        }

        public TwContainerBuilder WithCache(TimeSpan expire_ttl, TimeSpan outdate_ttl)
        {
            if (_cachePresent)
                throw new NotSupportedException("Only one cache present on this time");

            _expireTtl = expire_ttl;
            _outdateTtl = outdate_ttl;
            _externalCache = null;
            _cachePresent = true;

            return this;
        }


        public ITwContainer Build()
        {
            if (_cachePresent)
                throw new NotSupportedException("Cache");

            var importTypes = GetImports<Option.Void>(out var _);
            var factory = CreateContextFactory(importTypes);

            return new TwContainer(factory);
        }

        public ITwContainer<T> Build<T>()
        {
            if (_cachePresent)
                throw new NotSupportedException("Cache");

            var importTypes = GetImports<T>(out var importTupleFields);
            var factory = CreateContextFactory(importTypes);

            return new TwContainer<T>(factory, importTupleFields);
        }

        public ITwContainer<T, Tr> Build<T, Tr>()
        {
            var cacheOptions = CreateCacheOptionsIfNeded<T, Tr>();

            var importTypes = GetImports<T>(out var importTupleFields);
            var export = typeof(Tr);
            var isTupleExport = ValueTupleUtils.TryUnwrap(export, out var exportTupleTypes);
            var factory = CreateContextFactory(importTypes, isTupleExport ? exportTupleTypes : new[] { export });

            return new TwContainer<T, Tr>(factory, importTupleFields, isTupleExport ? exportTupleTypes : null, cacheOptions);
        }

        public ITwContainer<Option.Void, Tr> BuildWithResult<Tr>() => Build<Option.Void, Tr>();

        private static Type[] GetImports<T>(out FieldInfo[] importTupleFields)
        {
            var import = typeof(T);
            var isVoidImport = import == typeof(Option.Void);
            var importTypes = ValueTupleUtils.TryUnwrap(import, out var tupleTypes, out importTupleFields) ? tupleTypes :
                isVoidImport ? Array.Empty<Type>() : Enumerable.Repeat(import, 1);
            return importTypes.Concat(Enumerable.Repeat(typeof(CancellationToken), 1)).ToArray();
        }

        private ObjectPool<TwContext> CreateContextFactory(Type[] initial_imports, Type[] result_exports = null)
        {
            var contextBuilder = new TwContextBuilder();

            var namespaces = _namespaces.ToArray();
            MemberFilter entrypointFilter = (m, o) => m.CustomAttributes.Any(e => e.AttributeType == typeof(TwEntrypointAttribute));
            Func<Type, bool> nsComparer;
            if (namespaces.Length == 0)
                nsComparer = t => true;
            else
                nsComparer = t => namespaces.Any(prefix => IsNamespacePrefix(t.Namespace, prefix));
            var entrypoints = _assemblies.SelectMany(a => a.GetTypes()
                    .Where(t => t.IsClass && (!t.IsAbstract || t.IsSealed) && nsComparer(t))
                    .SelectMany(c => c.FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, entrypointFilter, null))
                    .Cast<MethodInfo>());

            foreach (var entrypoint in entrypoints)
            {
                var infoAtt = entrypoint.GetCustomAttribute<TwEntrypointAttribute>();
                contextBuilder.AddEntrypointMethod(entrypoint, infoAtt.Priority);
            }

            contextBuilder
                .RegisterInitialImports(initial_imports)
                .RegisterResultExports(result_exports)
                .CreateInstances(_resolver);

            return new ObjectPool<TwContext>(contextBuilder.Build);
        }

        private ProCacheFactory.Options<Tk, Tv> CreateCacheOptionsIfNeded<Tk, Tv>()
        {
            if (!_cachePresent)
                return null;

            if (_externalCache == null)
                return ProCacheFactory.CreateOptions<Tk, Tv>(_expireTtl, _outdateTtl);

            var externalCache = _externalCache as ProactiveCache.ICache<Tk, Tv> ?? throw new NotSupportedException();

            return ProCacheFactory.CreateOptions(_expireTtl, _outdateTtl, externalCache);
        }

        private static bool IsNamespacePrefix(string source_ns, string prefix_ns)
        {
            if (source_ns is null || !source_ns.StartsWith(prefix_ns)) return false;

            return source_ns.Length == prefix_ns.Length || source_ns[prefix_ns.Length] == '.';
        }
    }

}
