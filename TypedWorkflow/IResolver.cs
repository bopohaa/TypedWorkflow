using System;

namespace TypedWorkflow
{
    public interface ISimpleResolver
    {
        object Resolve(Type type);
    }

    public interface IResolver: ISimpleResolver
    {
        IScopedResolver CreateScope();
    }

    public interface IScopedResolver : ISimpleResolver, IDisposable { }
}
