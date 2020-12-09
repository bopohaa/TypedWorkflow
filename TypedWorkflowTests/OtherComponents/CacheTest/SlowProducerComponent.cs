using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.CacheTest
{
    public class SlowProducerComponent
    {
        private static long _runCnt;
        public static long RunCnt => _runCnt;

        [TwInject]
        public static void Init()
        {
            _runCnt = 0;
        }

        [TwEntrypoint]
        public async Task<long> Run(int key)
        {
            await Task.Delay(600);

            var res = Interlocked.Increment(ref _runCnt);

            return (res << 32) | (uint)key;
        }
    }
}
