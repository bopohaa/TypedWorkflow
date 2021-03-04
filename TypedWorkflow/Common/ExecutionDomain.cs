using System;
using System.Collections.Generic;
using System.Linq;

namespace TypedWorkflow.Common
{
    internal class ExecutionDomain
    {
        private class TypeEater
        {
            private readonly HashSet<Type> _total;
            private readonly HashSet<Type> _planned;
            private readonly HashSet<Type> _eatted;

            public int Count => _planned.Count;

            public TypeEater() : this(Array.Empty<Type>()) { }

            public TypeEater(IEnumerable<Type> types)
            {
                _total = new HashSet<Type>(types);
                _planned = new HashSet<Type>(types);
                _eatted = new HashSet<Type>();
            }

            public bool TryEat(IEnumerable<Type> types, TypeEater eat_up)
            {
                if (!CanEat(types))
                    return false;

                foreach (var t in types)
                {
                    if (!_total.Contains(t))
                        eat_up.PlanToEat(t);
                    else if (_planned.Remove(t))
                        _eatted.Add(t);
                }

                return true;
            }

            public void PlanToEat(IEnumerable<Type> types)
            {
                foreach (var t in types)
                    PlanToEat(t);
            }

            private bool CanEat(IEnumerable<Type> types)
            {
                foreach (var t in types)
                    if (_total.Contains(t)) return true;
                return false;
            }

            private void PlanToEat(Type type)
            {
                if (_total.Add(type))
                    _planned.Add(type);
            }
        }

        public readonly ExecutionDomainSettings Settings;
        public readonly IEntrypoint[] Entrypoints;

        private readonly List<ExecutionDomain> _innerDomain;
        public IReadOnlyList<ExecutionDomain> InnerDomains => _innerDomain;

        private ExecutionDomain(ExecutionDomainSettings setting, IEntrypoint[] entripoints)
        {
            Settings = setting;
            Entrypoints = entripoints;
            _innerDomain = new List<ExecutionDomain>();
        }

        public bool TryAddAsInnerDomain(ExecutionDomain domain)
        {
            var contains = Entrypoints.Intersect(domain.Entrypoints).Count();
            
            if (contains == 0)
                return false;

            if(contains != domain.Entrypoints.Length)
                throw new ArgumentException($"Partial intersections of execution domains '{Settings.Name}' and '{domain.Settings.Name}' is not allowed", nameof(domain));

            _innerDomain.Add(domain);
            return true;
        }

        public static ExecutionDomain Create(ExecutionDomainSettings settings, IReadOnlyList<IEntrypoint> entrypoints, Type[] optional_imports)
        {
            var notUsedInput = settings.KeyTypes.Except(entrypoints.SelectMany(e => e.Export));
            if (notUsedInput.Any())
                throw new ArgumentException($"Given input domain types '{string.Join(", ", notUsedInput)}' for `{settings.Name}` are not used in this workflow", nameof(settings));

            var notUsedOutput = settings.ValueTypes.Except(entrypoints.SelectMany(e => e.Import));
            if (notUsedOutput.Any())
                throw new ArgumentException($"Given output domain types '{string.Join(", ", notUsedOutput)}' for `{settings.Name}` are not used in this workflow", nameof(settings));

            var input = new TypeEater();
            var output = new TypeEater(settings.ValueTypes);
            var domain = new HashSet<IEntrypoint>();
            var imports = settings.KeyTypes.Concat(optional_imports).ToArray();

            while (input.Count > 0 || output.Count > 0)
            {
                foreach (var entry in entrypoints)
                {
                    if (output.TryEat(entry.Export, input))
                    {
                        output.PlanToEat(entry.Import.Except(imports));
                        domain.Add(entry);
                    }
                    if (input.TryEat(entry.Import, output))
                    {
                        input.PlanToEat(entry.Export);
                        domain.Add(entry);
                    }
                }
            }

            var domainEntrypoints = domain.ToArray();

            var ignoredInitialImports = settings.KeyTypes.Except(domainEntrypoints.SelectMany(e => e.Import));
            if (ignoredInitialImports.Any())
                throw new ArgumentException($"Input types '{string.Join(", ", ignoredInitialImports)}' are not consumed in this '{settings.Name}' domain");
            
            var importAsInternalExports = settings.KeyTypes.Intersect(domainEntrypoints.SelectMany(e => e.Export));
            if (importAsInternalExports.Any())
                throw new ArgumentException($"Input types '{string.Join(", ", importAsInternalExports)}' cannot be produced in this '{settings.Name}' domain");


            return new ExecutionDomain(settings, domainEntrypoints);
        }
    }
}
