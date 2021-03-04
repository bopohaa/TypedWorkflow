using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TypedWorkflow.Common
{
    internal class TwContextBuilder
    {
        private readonly List<IEntrypoint> _entrypoints;
        private readonly List<(TwComponentFactory, List<int>)> _scopedInstances;
        private readonly List<int[]> _exportIndex;
        private readonly List<int[]> _importIndex;
        private readonly List<TwConstraintIndex[]> _constraintIndex;
        private readonly List<int> _executeList;
        private readonly List<ExpressionFactory.Activator> _exportOptionNoneFactories;
        private readonly List<ExpressionFactory.Activator> _exportOptionSomeFactories;

        private int _componentsCount = 0;
        private int _exportCnt;
        private TwContextMeta _contextmeta;
        private int _initialEntrypointIdx = -1;
        private int _resultEtrypointIdx = -1;
        private IResolver _resolver;

        public TwContextBuilder()
        {
            _entrypoints = new List<IEntrypoint>();
            _scopedInstances = new List<(TwComponentFactory, List<int>)>();
            _exportIndex = new List<int[]>();
            _importIndex = new List<int[]>();
            _constraintIndex = new List<TwConstraintIndex[]>();
            _executeList = new List<int>();
            _exportOptionNoneFactories = new List<ExpressionFactory.Activator>();
            _exportOptionSomeFactories = new List<ExpressionFactory.Activator>();
        }

        public TwContextBuilder AddEntrypoints(IEnumerable<IEntrypoint> entrypoints)
        {
            if (_componentsCount > 0)
                throw new InvalidOperationException("Already created");

            _entrypoints.AddRange(entrypoints);

            return this;
        }
      

        public TwContextBuilder CreateInstances(IResolver resolver)
        {
            if (_componentsCount > 0)
                throw new InvalidOperationException("Already created");

            _initialEntrypointIdx = _entrypoints.FindIndex(e => e is InitialEntrypoint);
            _resultEtrypointIdx = _entrypoints.FindIndex(e => e is ResultEntrypoint);

            _resolver = resolver;

            var exportTypes = new List<Type>();
            var instanceIdxs = new Dictionary<Type, int>();

            for (var i = 0; i < _entrypoints.Count; ++i)
            {
                var entryPoint = _entrypoints[i];
                if (!(entryPoint.InstanceType is null))
                {
                    if (!instanceIdxs.TryGetValue(entryPoint.InstanceType, out var idx))
                    {
                        idx = _scopedInstances.Count;

                        var constructor = entryPoint.InstanceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).OrderBy(c => c.GetCustomAttribute<TwConstructorAttribute>() != null ? 0 : 1).FirstOrDefault();
                        var initialization = entryPoint.InstanceType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.GetCustomAttribute<TwInjectAttribute>() != null);
                        var factory = new TwComponentFactory(initialization, constructor, resolver);
                        _scopedInstances.Add((factory, new List<int>()));
                        instanceIdxs.Add(entryPoint.InstanceType, _componentsCount++);
                    }

                    if (!entryPoint.IsSingleton)
                        _scopedInstances[idx].Item2.Add(i);
                }

                var exportIndex = GetExportIndex(exportTypes, entryPoint.Export, entryPoint.InstanceType ?? entryPoint.GetType());
                _exportIndex.Add(exportIndex);

                var (none, some) = GetExportOptionFactory(entryPoint.Export);
                _exportOptionNoneFactories.AddRange(none);
                _exportOptionSomeFactories.AddRange(some);
            }

            _exportCnt = exportTypes.Count;

            foreach (var entryPoint in _entrypoints)
            {
                var importIndex = GetImportIndex(exportTypes, entryPoint.Import, entryPoint.InstanceType ?? entryPoint.GetType());
                _importIndex.Add(importIndex);

                var constraintIndex = GetConstraintIndex(exportTypes, entryPoint.Constraints, entryPoint.InstanceType ?? entryPoint.GetType());
                _constraintIndex.Add(constraintIndex);
            }

            CheckUseExports(exportTypes, _importIndex, _exportIndex, _entrypoints);

            foreach (var group in _entrypoints.GroupBy(e => e.Priority).OrderBy(g => g.Key))
                foreach (var entry in group)
                {
                    var idx = _entrypoints.FindIndex(e => e == entry);
                    if (_executeList.Contains(idx))
                        continue;

                    ResolveDependencyRecurse(idx, new Stack<int>(), exportTypes);
                }

            _contextmeta = CreateContextMeta();

            return this;
        }

        public TwContext Build()
        {
            if (_componentsCount == 0)
                throw new InvalidOperationException("Is not a created");

            return new TwContext(_contextmeta, _resolver);
        }

        private TwContextMeta CreateContextMeta()
        {
            if (!(_contextmeta is null))
                throw new InvalidOperationException("Context meta already created");

            var scopedInstances = _scopedInstances.Where(e => e.Item2.Count > 0).Select(e => (e.Item1, e.Item2.ToArray()));
            return new TwContextMeta(_componentsCount, _entrypoints.ToArray(), scopedInstances.ToArray(), _exportIndex.ToArray(), _importIndex.ToArray(), _constraintIndex.ToArray(), _executeList.ToArray(), _exportCnt, _initialEntrypointIdx, _resultEtrypointIdx, _exportOptionNoneFactories.ToArray(), _exportOptionSomeFactories.ToArray());
        }

        private void ResolveDependencyRecurse(int idx, Stack<int> parent, List<Type> exported_types)
        {
            parent.Push(idx);

            var dependencies = _importIndex[idx].Concat(_constraintIndex[idx].Select(e => e.Index));
            foreach (var dep in dependencies)
            {
                var depIdx = _exportIndex.FindIndex(e => e.Contains(dep));
                if (depIdx < 0)
                    throw new InvalidOperationException($"This type '{ exported_types[dep] }' is not exported in any component");

                if (_executeList.Contains(depIdx))
                    continue;

                if (parent.Contains(depIdx))
                    throw new InvalidOperationException($"Circular dependency detected, for {string.Join("->", parent.Select(i => _entrypoints[i].ToString()))}");

                ResolveDependencyRecurse(depIdx, parent, exported_types);
            }

            _executeList.Add(idx);

            parent.Pop();
        }

        private static void CheckUseExports(List<Type> exportTypes, List<int[]> import_index, List<int[]> export_index, List<IEntrypoint> entrypoints)
        {
            var imports = import_index.SelectMany(e => e).Distinct().ToArray();
            var notusedexports = export_index.SelectMany(e => e).Except(imports).Where(e => exportTypes[e] != typeof(System.Threading.CancellationToken)).ToArray();
            if (notusedexports.Length > 0)
            {
                var badComponents = new StringBuilder("Find unused export(s) value in type(s):");
                badComponents.AppendLine();
                foreach (var export in notusedexports)
                {
                    var idx = export_index.FindIndex(e => e.Contains(export));
                    if (idx < 0) continue;
                    badComponents.Append('(');
                    badComponents.Append(string.Join(", ", export_index[idx].Intersect(notusedexports).Select(e => exportTypes[e].ToString())));
                    badComponents.Append(')');
                    badComponents.AppendLine((entrypoints[idx].InstanceType ?? entrypoints[idx].GetType()).ToString());
                }
                throw new InvalidOperationException(badComponents.ToString());
            }
        }

        private static int[] GetExportIndex(List<Type> exportTypes, Type[] export, Type component_type)
        {
            if (export == null)
                return Array.Empty<int>();

            var index = new int[export.Length];
            for (var i = 0; i < index.Length; i++)
            {
                if (exportTypes.Contains(export[i]))
                    throw new InvalidOperationException($"This type '({ export[i] }){component_type}' already exported in other component");

                var idx = exportTypes.Count;
                exportTypes.Add(export[i]);

                index[i] = idx;
            }

            return index;
        }

        private static int[] GetImportIndex(List<Type> exportTypes, Type[] import, Type component_type)
        {
            var index = new int[import.Length];
            for (var i = 0; i < index.Length; i++)
            {
                var idx = exportTypes.FindIndex(t => t == import[i]);
                if (idx < 0)
                    throw new IndexOutOfRangeException($"Import type '{component_type}({import[i]})' is not defined as exported");

                index[i] = idx;
            }

            return index;
        }

        private static TwConstraintIndex[] GetConstraintIndex(List<Type> exportTypes, TwConstraint[] constraints, Type component_type)
        {
            var index = new TwConstraintIndex[constraints.Length];
            for (var i = 0; i < index.Length; i++)
            {
                var idx = exportTypes.FindIndex(t => t == constraints[i].Constraint);
                if (idx < 0)
                    throw new IndexOutOfRangeException($"Constraint type '{component_type}({constraints[i].Constraint})' is not defined as exported");

                index[i] = new TwConstraintIndex(idx, constraints[i].HasNone);
            }

            return index;
        }

        private static (ExpressionFactory.Activator[] none, ExpressionFactory.Activator[] some) GetExportOptionFactory(Type[] exports)
        {
            var length = exports.Length;
            var resNone = new ExpressionFactory.Activator[length];
            var resSome = new ExpressionFactory.Activator[length];

            for (var i = 0; i < length; i++)
            {
                var export = exports[i];
                var optionExport = typeof(Option<>).MakeGenericType(export);
                var constructorNone = optionExport.GetConstructor(Array.Empty<Type>());
                var constructorSome = optionExport.GetConstructor(new[] { export });
                var (activatorNone, _) = ExpressionFactory.GetActivator(constructorNone);
                var (activatorSome, _) = ExpressionFactory.GetActivator(constructorSome);
                resNone[i] = activatorNone;
                resSome[i] = activatorSome;
            }

            return (resNone, resSome);
        }
    }
}
