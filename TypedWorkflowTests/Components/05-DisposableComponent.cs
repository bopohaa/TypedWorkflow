using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    public class DisposableComponent : IDisposable
    {
        private static int _constructCount = 0;
        private static int _disposingCount = 0;
        private static int _runCount = 0;

        public DisposableComponent()
        {
            Interlocked.Increment(ref _constructCount);
        }

        [TwEntrypoint]
        public void Run()
        {
            Interlocked.Increment(ref _runCount);
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposingCount);
        }

        public static bool Assert(int iteration_cnt)
            => _constructCount == iteration_cnt &&
               _runCount == iteration_cnt &&
               _disposingCount == iteration_cnt;


    }
}
