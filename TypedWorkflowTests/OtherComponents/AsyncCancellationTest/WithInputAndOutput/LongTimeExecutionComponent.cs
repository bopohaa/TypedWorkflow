using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.AsyncCancellationTest.WithInputAndOutput
{
    public class LongTimeExecutionComponent
    {
        [TwEntrypoint]
        public async Task<long> Run(int time, CancellationToken cancellation)
        {
            await Task.Delay(time, cancellation);

            return time;
        }
    }
}
