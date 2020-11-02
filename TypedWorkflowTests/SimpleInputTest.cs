using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using TypedWorkflow;
using TypedWorkflowTests.Components;
using TypedWorkflowTests.OtherComponents.SimpleInputTest;

namespace TypedWorkflowTests
{
    public class SimpleInputTest
    {
        private const int ITERATION_CNT = 100;

        private InputModel[] _inputModels = new InputModel[ITERATION_CNT];

        public SimpleInputTest()
        {
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(ConsumingInputArgComponent).Assembly)
                .AddNamespaces("TypedWorkflowTests.OtherComponents.SimpleInputTest")
                .Build<InputModel>();

            var tasks = new Task[ITERATION_CNT];
            for (var i = 0; i < ITERATION_CNT; i++)
            {
                _inputModels[i] = new InputModel { SomeProp = i };
                var input = _inputModels[i];
                tasks[i] = Task.Run(() => container.Run(input).AsTask());
            }
            Task.WaitAll(tasks);
        }

        [Test]
        public void Test()
        {
            var models = _inputModels.Select(e => new ConsumingInputArgComponent().ConsumeInputArgs(e));
            ConsumingProducedModelComponent.Assert(models);
        }

    }
}
