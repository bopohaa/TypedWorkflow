using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.CompositeInputTest
{
    public class ConsumingProducedModelComponent
    {
        private static long _model1Sum;
        private static long _model2Sum;

        [TwEntrypoint]
        public Task ConsumeProducedModel(InputModel2 model2, ConsumingInputArgComponent.ProducedModel arg)
        {
            Interlocked.Add(ref _model1Sum, arg.SomeArg);
            Interlocked.Add(ref _model2Sum, model2.SomeProp);

            return Task.Delay(10);
        }

        public static bool Assert(IEnumerable<ConsumingInputArgComponent.ProducedModel> input1_models, IEnumerable<InputModel2> input2_models)
        {
            var expected1 = input1_models.Sum(e => e.SomeArg);
            var expected2 = input2_models.Sum(e => e.SomeProp);
            return expected1 == _model1Sum && expected2 == _model2Sum;
        }
    }
}
