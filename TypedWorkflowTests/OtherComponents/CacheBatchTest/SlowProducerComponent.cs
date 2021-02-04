using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.CacheBatchTest
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
        public async Task<IEnumerable<KeyValuePair<int, long>>> Run(IEnumerable<int> keys)
        {
            await Task.Delay(700);

            return keys.Select(k => new KeyValuePair<int, long>(k, Interlocked.Increment(ref _runCnt)));
        }
    }
}
