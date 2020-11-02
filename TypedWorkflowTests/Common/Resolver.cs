using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Common
{
    public class Resolver : IResolver
    {
        public IStringBuilder Sb { get; private set; } = new StringBuilderService();

        private int _resolveCount = 0;
        public int ResolveCount => _resolveCount;

        public object Resolve(Type type)
        {
            Interlocked.Increment(ref _resolveCount);

            if (type == typeof(IStringBuilder))
                return Sb;

            throw new NotSupportedException();
        }
    }
}
