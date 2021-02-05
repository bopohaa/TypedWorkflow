using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow.Common
{
    internal class ExecutionDomain
    {
        public readonly ExecutionDomainSettings Settings;
        public IEntrypoint[] Entrypoints { get; private set; }

        private ExecutionDomain(ExecutionDomainSettings setting)
        {
            Settings = setting;
        }


        public static ExecutionDomain Create(ExecutionDomainSettings settings, IReadOnlyList<IEntrypoint> entrypoints, Type[] initial_imports, Type[] result_exports)
        {
            throw new NotImplementedException();
            //return new ExecutionDomain(settings);
        }
    }
}
