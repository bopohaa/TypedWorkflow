using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.AsyncCancellationTest.WoInputAndOutput
{
    public class LongTimeExecutionComponent
    {
        [TwEntrypoint]
        public async Task Run(CancellationToken cancellation)
        {
            await Task.Delay(-1, cancellation);
        }
    }
}
