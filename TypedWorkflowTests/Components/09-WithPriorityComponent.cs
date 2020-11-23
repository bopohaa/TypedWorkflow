using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    public class WithPriorityComponent
    {
        private static int _result = 0;
        private static AsyncLocal<int> _resultInst = new AsyncLocal<int>();

        [TwEntrypoint(TwEntrypointPriorityEnum.Low)]
        public void LowPriority()
            => Interlocked.Add(ref _result, _resultInst.Value * 2);

        [TwEntrypoint(TwEntrypointPriorityEnum.Hight)]
        public void HightPriority()
            => _resultInst.Value = 2;


        public class WithPriorityComponent2
        {
            [TwEntrypoint(TwEntrypointPriorityEnum.Medium)]
            public void MediumPriority()
                => _resultInst.Value += 2;
        }

        public static bool Assert(int iteration_cnt)
            => _result == (2 + 2) * 2 * iteration_cnt;
    }
}
