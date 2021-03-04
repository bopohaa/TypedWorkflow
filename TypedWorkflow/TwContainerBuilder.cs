using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using TypedWorkflow.Common;

namespace TypedWorkflow
{
    public sealed class TwContainerBuilder
    {
        private readonly HashSet<Assembly> _assemblies;
        private readonly HashSet<string> _namespaces;
        private readonly List<IEntrypoint> _entrypoints;
        private readonly Dictionary<string, ExecutionDomainSettings> _executionDomainsSettings;
        private IResolver _resolver;

        public TwContainerBuilder()
        {
            _assemblies = new HashSet<Assembly>();
            _namespaces = new HashSet<string>();
            _entrypoints = new List<IEntrypoint>();
            _executionDomainsSettings = new Dictionary<string, ExecutionDomainSettings>();
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

        public TwContainerBuilder AddCacheDomain<Tk, Tv>(string name, TimeSpan expire_ttl, TimeSpan outdate_ttl, ProactiveCache.ICache<Tk, Tv> external_cache = null)
        {
            AddCacheDomain(name, false, typeof(Tk), typeof(Tv), expire_ttl, outdate_ttl, external_cache);
            return this;
        }
        public TwContainerBuilder AddCacheBatchDomain<Tk, Tv>(string name, TimeSpan expire_ttl, TimeSpan outdate_ttl, ProactiveCache.ICache<Tk, Tv> external_cache = null)
        {
            AddCacheDomain(name, true, typeof(Tk), typeof(Tv), expire_ttl, outdate_ttl, external_cache);
            return this;
        }

        private void AddCacheDomain(string name, bool batched, Type key, Type value, TimeSpan expire_ttl, TimeSpan outdate_ttl, object external_cache)
            => _executionDomainsSettings.Add(name, ExecutionDomainSettings.Create(name, key, value, (batched, expire_ttl, outdate_ttl, external_cache)));


        public ITwContainer Build()
        {
            var factory = CreateContextFactory(GetImports<Option.Void>(out var _));

            return new TwContainer(factory);
        }

        public ITwContainer<T> Build<T>()
        {
            var importTypes = GetImports<T>(out var importTupleFields);
            var factory = CreateContextFactory(importTypes);

            return new TwContainer<T>(factory, importTupleFields);
        }

        public ITwContainer<Option.Void, Tr> BuildWithResult<Tr>()
            => Build<Option.Void, Tr>();

        public ITwContainer<T, Tr> Build<T, Tr>()
            => new TwContainer<T, Tr>(CreateContextFactory<T, Tr>(out var importTupleFields, out var exportTupleTypes), importTupleFields, exportTupleTypes);

        public ITwContainer<Tk, Tv> BuildWithCache<Tk, Tv>(TimeSpan expire_ttl, TimeSpan outdate_ttl, ProactiveCache.ICache<Tk, Tv> external_cache = null)
        {
            var factory = CreateContextFactory<Tk, Tv>(out var importTupleFields, out var exportTupleTypes);
            return new TwContainerWithCache<Tk, Tv>(factory, importTupleFields, exportTupleTypes, ProCacheFactory.CreateOptions(expire_ttl, outdate_ttl, external_cache));
        }

        public ITwContainer<IEnumerable<Tk>, IEnumerable<KeyValuePair<Tk, Tv>>> BuildWithCacheBatch<Tk, Tv>(TimeSpan expire_ttl, TimeSpan outdate_ttl, ProactiveCache.ICache<Tk, Tv> external_cache = null)
        {
            var factory = CreateContextFactory<IEnumerable<Tk>, IEnumerable<KeyValuePair<Tk, Tv>>>(out var importTupleFields, out var exportTupleTypes);
            return new TwContainerWithCacheBatch<Tk, Tv>(factory, importTupleFields, exportTupleTypes, ProCacheFactory.CreateOptions(expire_ttl, outdate_ttl, external_cache));
        }

        private ObjectPool<TwContext> CreateContextFactory<T, Tr>(out FieldInfo[] import_tuple_fields, out Type[] export_tuple_types)
        {
            var importTypes = GetImports<T>(out import_tuple_fields);
            var export = typeof(Tr);
            var isTupleExport = ValueTupleUtils.TryUnwrap(export, out export_tuple_types);
            return CreateContextFactory(importTypes, isTupleExport ? export_tuple_types : new[] { export });
        }

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

            var allEntrypoints = new List<IEntrypoint>(_entrypoints);
            allEntrypoints.AddRange(entrypoints.Select(m => ComponentEntrypoint.Create(m, m.GetCustomAttribute<TwEntrypointAttribute>().Priority)));
            AddBuiltinEntrypoints(initial_imports, result_exports, allEntrypoints);

            CheckDomainWithInitialImports(initial_imports);
            CheckDomainWithResultExports(result_exports);
            var executionDomains = CreateExecutionDomains(allEntrypoints, new[] { typeof(CancellationToken) });
            var rootEntrypoints = allEntrypoints.Except(executionDomains.SelectMany(d => d.Entrypoints));

            var contextBuilder = new TwContextBuilder();
            contextBuilder
                .AddEntrypoints(rootEntrypoints)
                .CreateInstances(_resolver);

            return new ObjectPool<TwContext>(contextBuilder.Build);
        }

        private void CheckDomainWithInitialImports(Type[] initial_imports)
        {
            if (initial_imports is null || initial_imports.Length == 0)
                return;
            var invalidDomain = _executionDomainsSettings.Where(e => e.Value.ValueTypes.Intersect(initial_imports).Any()).Select(e => e.Value).FirstOrDefault();
            if (invalidDomain is null)
                return;

            throw new ArgumentException($"Execution domain '{invalidDomain.Name}' produce '{string.Join(", ", invalidDomain.ValueTypes.Intersect(initial_imports))}' of initial import types");
        }

        private void CheckDomainWithResultExports(Type[] result_exports)
        {
            if (result_exports is null || result_exports.Length == 0)
                return;
            var invalidDomain = _executionDomainsSettings.Where(e => e.Value.KeyTypes.Intersect(result_exports).Any()).Select(e => e.Value).FirstOrDefault();
            if (invalidDomain is null)
                return;

            throw new ArgumentException($"Execution domain '{invalidDomain.Name}' conusume '{string.Join(", ", invalidDomain.ValueTypes.Intersect(result_exports))}' of result export types");
        }

        private IReadOnlyList<ExecutionDomain> CreateExecutionDomains(IReadOnlyList<IEntrypoint> entrypoints, Type[] optional_imports)
        {
            if (_executionDomainsSettings.Count == 0)
                return Array.Empty<ExecutionDomain>();

            var orderedExecutionDomains = _executionDomainsSettings
                .Select(e => ExecutionDomain.Create(e.Value, entrypoints, optional_imports))
                .OrderByDescending(e => e.Entrypoints.Length)
                .ToList();

            GroupingNestedExecutionDomains(orderedExecutionDomains);

            return orderedExecutionDomains;
        }

        private static void GroupingNestedExecutionDomains(List<ExecutionDomain> ordered_execution_domains)
        {
            var cnt = ordered_execution_domains.Count;
            for (var i = 0; i < cnt; i++)
            {
                var current = ordered_execution_domains[i];
                for (var j = i + 1; j < cnt; j++)
                {
                    var next = ordered_execution_domains[j];
                    if (current.TryAddAsInnerDomain(next))
                    {
                        ordered_execution_domains.RemoveAt(j);
                        cnt--;
                        j--;
                    }
                }
            }
        }

        private static bool IsNamespacePrefix(string source_ns, string prefix_ns)
        {
            if (source_ns is null || !source_ns.StartsWith(prefix_ns)) return false;

            return source_ns.Length == prefix_ns.Length || source_ns[prefix_ns.Length] == '.';
        }

        private static void AddBuiltinEntrypoints(Type[] initial_imports, Type[] result_exports, List<IEntrypoint> allEntrypoints)
        {
            if (initial_imports?.Length > 0)
                allEntrypoints.Add(new InitialEntrypoint(initial_imports));
            if (result_exports?.Length > 0)
                allEntrypoints.Add(new ResultEntrypoint(result_exports));
        }
    }
}
