using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.CompositeOutputTest
{
    public class ProduceOutputArg1Component
    {
        private static int _runCnt;
        private static int _sum;

        [TwEntrypoint]
        public async Task<OutputModel1> Run()
        {
            Interlocked.Add(ref _sum, Interlocked.Increment(ref _runCnt));

            await Task.Delay(10);

            return new OutputModel1 { SomeProp = _sum };
        }

        public static bool Assert(IEnumerable<OutputModel1> models)
            => models.Sum(e => e.SomeProp) == _sum && models.Count() == _runCnt;
    }
}
