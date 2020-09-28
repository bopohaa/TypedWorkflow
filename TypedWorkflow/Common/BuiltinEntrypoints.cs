using System;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class InitialEntrypoint : IEntrypoint
    {
        public Type[] Export { get; }

        public Type[] Import => Array.Empty<Type>();

        public Type InstanceType => typeof(InitialEntrypoint);

        public TwEntrypointPriorityEnum Priority => (TwEntrypointPriorityEnum)(int.MinValue);

        public bool IsAsync => false;

        public InitialEntrypoint(Type[] exports)
        {
            Export = exports;
        }

        public int Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != Export.Length)
                throw new InvalidOperationException("Invalid import");

            for (var i = 0; i < args.Length; ++i)
                output[i] = args[i];
            return args.Length;
        }

        public Task<int> ExecuteAsync(object instance, object[] args, object[] output)
        {
            throw new NotImplementedException();
        }
    }

    internal class ResultEntrypoint : IEntrypoint
    {
        public Type[] Import { get; }

        public Type[] Export => Array.Empty<Type>();

        public Type InstanceType => typeof(InitialEntrypoint);

        public TwEntrypointPriorityEnum Priority => (TwEntrypointPriorityEnum)(int.MaxValue);

        public bool IsAsync => false;

        public ResultEntrypoint(Type[] imports)
        {
            Import = imports;
        }

        public int Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid import");
            return 0;
        }

        public Task<int> ExecuteAsync(object instance, object[] args, object[] output)
        {
            throw new NotImplementedException();
        }
    }
}
