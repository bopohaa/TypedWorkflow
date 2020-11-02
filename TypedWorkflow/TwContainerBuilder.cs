﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TypedWorkflow.Common;

namespace TypedWorkflow
{
    public class TwContainerBuilder
    {
        private static Type[] _valueTupleTypes = new[] { typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>) };

        private HashSet<Assembly> _assemblies;
        private HashSet<string> _namespaces;
        private IResolver _resolver;

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

        public ITwContainer Build()
        {
            var factory = CreateContextFactory();

            return new TwContainer(factory);
        }

        public ITwContainer<T> Build<T>()
        {
            var importTypes  = GetImports<T>(out var importTupleFields);
            var factory = CreateContextFactory(importTypes);

            return new TwContainer<T>(factory, importTupleFields);
        }

        public ITwContainer<T, Tr> Build<T, Tr>()
        {
            var importTypes = GetImports<T>(out var importTupleFields);
            var export = typeof(Tr);
            var isTupleExport = export.IsGenericType ? _valueTupleTypes.Contains(export.GetGenericTypeDefinition()) : false;
            var exportTypes = isTupleExport ? export.GenericTypeArguments : new[] { export };

            var factory = CreateContextFactory(importTypes, exportTypes);

            return new TwContainer<T, Tr>(factory, importTupleFields, isTupleExport ? exportTypes : null);
        }

        public ITwContainer<Option.Void, Tr> BuildWithResult<Tr>() => Build<Option.Void, Tr>();

        private static Type[] GetImports<T>(out FieldInfo[] importTupleFields)
        {
            var import = typeof(T);
            var isVoidImport = import == typeof(Option.Void);
            var isTupeImport = import.IsGenericType ? _valueTupleTypes.Contains(import.GetGenericTypeDefinition()) : false;
            importTupleFields = isTupeImport ? import.GetFields() : null;
            var importTypes = isVoidImport ? null : isTupeImport ? import.GenericTypeArguments : new[] { import };
            return importTypes;
        }

        private ObjectPool<TwContext> CreateContextFactory(Type[] initial_imports = null, Type[] result_exports = null)
        {
            var contextBuilder = new TwContextBuilder();

            var namespaces = _namespaces.ToArray();
            MemberFilter entrypointFilter = (m, o) => m.CustomAttributes.Any(e => e.AttributeType == typeof(TwEntrypointAttribute));
            Func<Type, bool> nsComparer;
            if (namespaces.Length == 0)
                nsComparer = t => true;
            else
                nsComparer = t => namespaces.Any(ns => t.Namespace?.StartsWith(ns) ?? false);
            var entrypoints = _assemblies.SelectMany(a => a.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && nsComparer(t))
                    .SelectMany(c => c.FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, entrypointFilter, null))
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
    }

}
