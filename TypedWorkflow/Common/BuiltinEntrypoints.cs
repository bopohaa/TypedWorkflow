using System;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class InitialEntrypoint : IEntrypoint
    {
        private readonly Type[] _export;
        public Type[] Export => _export;
        private readonly bool[] _exportIsOption;
        public bool[] ExportIsOption => _exportIsOption;

        public Type[] Import => Array.Empty<Type>();
        public bool[] ImportIsOption => Array.Empty<bool>();

        public Type InstanceType => typeof(InitialEntrypoint);

        public TwEntrypointPriorityEnum Priority => (TwEntrypointPriorityEnum)(int.MinValue);

        public bool IsAsync => false;

        public TwConstraint[] Constraints => Array.Empty<TwConstraint>();

        public InitialEntrypoint(Type[] exports)
        {
            var (export, exportIsOptions) = ExpressionFactory.GetExpandedTypes(exports);
            _export = export;
            _exportIsOption = exportIsOptions;
        }

        public void Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != _export.Length)
                throw new InvalidOperationException("Invalid import");
            Array.Copy(args, output, args.Length);
        }

        public Task ExecuteAsync(object instance, object[] args, object[] output)
        {
            throw new NotImplementedException();
        }
    }

    internal class ResultEntrypoint : IEntrypoint
    {
        private readonly Type[] _import;
        public Type[] Import => _import;
        private readonly bool[] _importIsOption;
        public bool[] ImportIsOption => _importIsOption;

        public Type[] Export => Array.Empty<Type>();
        public bool[] ExportIsOption => Array.Empty<bool>();

        public Type InstanceType => typeof(InitialEntrypoint);

        public TwEntrypointPriorityEnum Priority => (TwEntrypointPriorityEnum)(int.MaxValue);

        public bool IsAsync => false;

        public TwConstraint[] Constraints => Array.Empty<TwConstraint>();

        public ResultEntrypoint(Type[] imports)
        {
            var (import, importIsOptions) = ExpressionFactory.GetExpandedTypes(imports);
            _import = import;
            _importIsOption = importIsOptions;
        }

        public void Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != _import.Length)
                throw new InvalidOperationException("Invalid import");
        }

        public Task ExecuteAsync(object instance, object[] args, object[] output)
        {
            throw new NotImplementedException();
        }
    }
}
