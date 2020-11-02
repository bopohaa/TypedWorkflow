using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.SimpleInputTest
{
    [TwSingleton]
    public class ConsumingProducedModelComponent
    {
        private static long _argSum;

        [TwEntrypoint]
        public Task ConsumeProducedModel(ConsumingInputArgComponent.ProducedModel arg)
        {
            Interlocked.Add(ref _argSum, arg.SomeArg);

            return Task.Delay(10);
        }

        public static bool Assert(IEnumerable<ConsumingInputArgComponent.ProducedModel> input_models)
        {
            var expected = input_models.Sum(e => e.SomeArg);
            return expected == _argSum;
        }
    }
}
