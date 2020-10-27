using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;
using TypedWorkflowTests.Common;

namespace TypedWorkflowTests.Components.Composite
{
    public class CompositeComponent
    {
        private static int _constructCount = 0;
        private static int _someRunCount = 0;
        private static int _otherRunCount = 0;
        private static int _runPerIteration = 0;

        private int _runPerIterationInst = 0;

        public CompositeComponent()
        {
            Interlocked.Increment(ref _constructCount);
        }

        [TwEntrypoint]
        public void SomeRun()
        {
            Interlocked.Increment(ref _someRunCount);
            _runPerIterationInst++;
            _runPerIteration = _runPerIterationInst;
        }

        [TwEntrypoint]
        public void OtherRun()
        {
            Interlocked.Increment(ref _otherRunCount);
            _runPerIterationInst++;
            _runPerIteration = _runPerIterationInst;
        }

        public void NewerRun()
        {
            Interlocked.Increment(ref _someRunCount);
            Interlocked.Increment(ref _otherRunCount);
        }

        public static bool Assert(int iteration_cnt)
            => _constructCount == iteration_cnt &&
               _someRunCount == iteration_cnt &&
               _otherRunCount == iteration_cnt &&
               _runPerIteration == 2;
    }
}
