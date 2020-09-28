using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    public class SingleComponent
    {
        private static int _runCount = 0;

        [TwEntrypoint]
        public void SomeRun()
            => Interlocked.Increment(ref _runCount);

        public static bool Assert(int iteration_cnt)
            => iteration_cnt == _runCount;
    }
}
