using System.Threading;
using System.Threading.Tasks;

namespace TypedWorkflow
{
    public interface ITwContainer
    {
        ValueTask Run(CancellationToken cancellation = default(CancellationToken));
    }

    public interface ITwContainer<T>
    {
        ValueTask Run(T initial_imports, CancellationToken cancellation = default(CancellationToken));
    }

    public interface ITwContainer<T, Tr>
    {
        ValueTask<Tr> Run(T initial_imports, CancellationToken cancellation = default(CancellationToken));
    }

    public static class TwContainerExtensions
    {
        public static ValueTask<Tr> Run<Tr>(this ITwContainer<Option.Void, Tr> container, CancellationToken cancellation = default(CancellationToken))
            => container.Run(new Option.Void(), cancellation);
    }
}
