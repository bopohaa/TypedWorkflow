using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;
using TypedWorkflowTests.Common;

namespace TypedWorkflowTests.Components
{
    [TwSingleton]
    public class SingletonComponent
    {
        private static int _constructCount = 0;
        private static int _iterationCount = 0;

        public int _iterationCountInst = 0;

        public SingletonComponent()
        {
            Interlocked.Increment(ref _constructCount);
        }

        [TwEntrypoint]
        public void RunPerIteration()
        {
            Interlocked.Increment(ref _iterationCount); 
        }

        public static bool Assert(int iteration_cnt)
            => _constructCount == 1 &&
               _iterationCount == iteration_cnt;
    }
}
