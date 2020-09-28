using System;

namespace TypedWorkflow
{
    public interface IResolver
    {
        object Resolve(Type type);
    }
}
