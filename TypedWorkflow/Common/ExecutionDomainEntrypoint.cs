using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class ExecutionDomainEntrypoint : IEntrypoint
    {
        public bool IsSingleton => true;

        public TwConstraint[] Constraints => Array.Empty<TwConstraint>();

        private Type[] _export;
        public Type[] Export => _export;

        private bool[] _exportIsOption;
        public bool[] ExportIsOption => _exportIsOption;

        private Type[] _import;
        public Type[] Import => _import;

        private bool[] _importIsOption;
        public bool[] ImportIsOption => _importIsOption;

        public Type InstanceType => null;

        public TwEntrypointPriorityEnum Priority => throw new NotImplementedException();

        public bool IsAsync => throw new NotImplementedException();

        public void Execute(object instance, object[] args, object[] output)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteAsync(object instance, object[] args, object[] output)
        {
            throw new NotImplementedException();
        }
    }
}
