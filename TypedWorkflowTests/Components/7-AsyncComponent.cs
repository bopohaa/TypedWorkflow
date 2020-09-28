using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    [TwSingleton]
    public class AsyncComponent
    {
        private const int DATA1 = 1;
        private const int DATA2 = 2;
        private const int DATA3 = 3;

        private static int _result = 0;

        public struct Result1 { public int Data; }
        public struct Result2 { public int Data; }
        public struct Result3 { public int Data; }

        [TwEntrypoint]
        public Task<Result1> Run1()
            => Task.Delay(10).ContinueWith(t => new Result1 { Data = DATA1 });

        [TwEntrypoint]
        public async Task<(Result2, Result3)> Run2()
        {
            await Task.Delay(10);
            return (new Result2 { Data = DATA2 }, new Result3 { Data = DATA3 });
        }

        [TwEntrypoint]
        public void Run(Result1 res1, Result2 res2, Result3 res3)
            => Interlocked.Add(ref _result, res1.Data + res2.Data + res3.Data);

        public static bool Assert(int iteration_cnt)
            => _result == (DATA1 + DATA2 + DATA3) * iteration_cnt;

    }
}
