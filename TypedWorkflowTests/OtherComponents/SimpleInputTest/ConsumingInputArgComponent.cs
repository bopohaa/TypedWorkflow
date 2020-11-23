using System;
using System.Collections.Generic;
using System.Text;
using TypedWorkflow;

namespace TypedWorkflowTests.OtherComponents.SimpleInputTest
{
    public class ConsumingInputArgComponent
    {
        public struct ProducedModel
        {
            public int SomeArg;
        }

        [TwEntrypoint]
        public ProducedModel ConsumeInputArgs(InputModel input_arg) 
            => new ProducedModel { SomeArg = input_arg.SomeProp };
    }
}
