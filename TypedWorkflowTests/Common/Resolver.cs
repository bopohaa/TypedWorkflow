using System;
using System.Threading;

using TypedWorkflow;

namespace TypedWorkflowTests.Common
{
    public class Resolver : IResolver
    {
        public IStringBuilder Sb { get; private set; } = new StringBuilderService();

        private int _resolveCount = 0;
        public int ResolveCount => _resolveCount;

        private int _createScopeCnt = 0;
        public int CreateScopeCnt => _createScopeCnt;

        private class ResolverScope : IScopedResolver
        {
            private readonly ISimpleResolver _resolver;

            public ResolverScope(ISimpleResolver resolver) => _resolver = resolver;

            public void Dispose() { }

            public object Resolve(Type type) => _resolver.Resolve(type);
        }


        public object Resolve(Type type)
        {
            Interlocked.Increment(ref _resolveCount);

            if (type == typeof(IStringBuilder))
                return Sb;

            throw new NotSupportedException();
        }

        public IScopedResolver CreateScope()
        {
            Interlocked.Increment(ref _createScopeCnt);
            return new ResolverScope(this);
        }
    }
}
