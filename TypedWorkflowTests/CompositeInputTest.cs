using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using TypedWorkflow;
using TypedWorkflowTests.OtherComponents.CompositeInputTest;

namespace TypedWorkflowTests
{
    public class CompositeInputTest
    {
        private const int ITERATION_CNT = 100;

        private (InputModel1, InputModel2)[] _inputModels = new (InputModel1, InputModel2)[ITERATION_CNT];

        public CompositeInputTest()
        {
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(ConsumingInputArgComponent).Assembly)
                .AddNamespaces("TypedWorkflowTests.OtherComponents.CompositeInputTest")
                .Build<(InputModel1, InputModel2)>();

            var tasks = new Task[ITERATION_CNT];
            for (var i = 0; i < ITERATION_CNT; i++)
            {
                var input = (new InputModel1 { SomeProp = i }, new InputModel2 { SomeProp = ITERATION_CNT + i });
                _inputModels[i] = input;
                tasks[i] = Task.Run(() => container.Run(input).AsTask());
            }
            Task.WaitAll(tasks);
        }

        [Test]
        public void Test()
        {
            var models1 = _inputModels.Select(e => new ConsumingInputArgComponent().ConsumeInputArgs(e.Item1));
            var models2 = _inputModels.Select(e => e.Item2);
            ConsumingProducedModelComponent.Assert(models1, models2);
        }
    }
}
