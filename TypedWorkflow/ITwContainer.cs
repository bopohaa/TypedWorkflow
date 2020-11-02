using System.Threading;
using System.Threading.Tasks;

namespace TypedWorkflow
{
    public interface ITwContainer
    {
        ValueTask Run();
    }

    public interface ITwContainer<T>
    {
        ValueTask Run(T initial_imports);
    }

    public interface ITwContainer<T, Tr>
    {
        ValueTask<Tr> Run(T initial_imports);
    }

    public static class TwContainerExtensions
    {
        public static ValueTask<Tr> Run<Tr>(this ITwContainer<Option.Void, Tr> container)
            => container.Run(new Option.Void());
    }
}
