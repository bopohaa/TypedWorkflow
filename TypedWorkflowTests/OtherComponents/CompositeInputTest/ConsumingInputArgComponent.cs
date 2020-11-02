using System;
using System.Collections.Generic;
using System.Text;
using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.CompositeInputTest
{
    [TwSingleton]
    public class ConsumingInputArgComponent
    {
        public struct ProducedModel
        {
            public int SomeArg;
        }

        [TwEntrypoint]
        public ProducedModel ConsumeInputArgs(InputModel1 input_arg) 
            => new ProducedModel { SomeArg = input_arg.SomeProp };
    }
}
