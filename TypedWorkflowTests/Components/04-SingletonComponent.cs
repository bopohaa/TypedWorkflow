using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using TypedWorkflow;

using TypedWorkflowTests.Common;

namespace TypedWorkflowTests.Components
{
    public static class SingletonComponent
    {
        private static int _initCount = 0;
        private static int _iterationCount = 0;

        public static int _iterationCountInst = 0;

        [TwInject]
        public static void Init(IStringBuilder sb)
        {
            Interlocked.Increment(ref _initCount);
        }

        [TwEntrypoint]
        public static void RunPerIteration()
        {
            Interlocked.Increment(ref _iterationCount);
        }

        public static bool Assert(int iteration_cnt)
            => _initCount == 1 &&
               _iterationCount == iteration_cnt;
    }

    public class MixedSingletonComponent
    {
        private static int _initCount = 0;
        private static int _constructCount = 0;
        private static int _iterationCount = 0;

        public static int _iterationCountInst = 0;

        [TwInject]
        public static void Init(IStringBuilder sb)
        {
            Interlocked.Increment(ref _initCount);
        }

        public MixedSingletonComponent(IStringBuilder scoped_sb)
        {
            Interlocked.Increment(ref _constructCount);
        }

        [TwEntrypoint]
        public static void RunPerIteration()
        {
            Interlocked.Increment(ref _iterationCount);
        }

        [TwEntrypoint]
        public void RunPerIterationWithScope()
        {
            Interlocked.Increment(ref _iterationCount);
        }

        public static bool Assert(int iteration_cnt)
            => _initCount == 1 &&
               _constructCount == iteration_cnt &&
               _iterationCount == iteration_cnt * 2;

    }
}
