using System;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal interface IEntrypoint
    {
        TwConstraint[] Constraints { get; }
        Type[] Export { get; }
        bool[] ExportIsOption { get; }
        Type[] Import { get; }
        bool[] ImportIsOption { get; }
        Type InstanceType { get; }
        TwEntrypointPriorityEnum Priority { get; }
        bool IsAsync { get; }

        void Execute(object instance, object[] args, object[] output);
        Task ExecuteAsync(object instance, object[] args, object[] output);
    }
}