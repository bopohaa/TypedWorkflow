using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.CompositeOutputTest
{
    [TwSingleton]
    public class ProduceOutputArg2Component
    {
        private static int _runCnt;
        private static int _sum;

        [TwEntrypoint]
        public OutputModel2 Run()
        {
            Interlocked.Add(ref _sum, Interlocked.Increment(ref _runCnt));
            return new OutputModel2 { SomeProp = _sum };
        }

        public static bool Assert(IEnumerable<OutputModel2> models)
            => models.Sum(e => e.SomeProp) == _sum && models.Count() == _runCnt;

    }
}
