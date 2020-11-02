using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypedWorkflow;
using TypedWorkflowTests.OtherComponents.CompositeOutputTest;

namespace TypedWorkflowTests
{
    public class CompositeOutputTest
    {
        private const int ITERATION_CNT = 100;
        private static (OutputModel1, OutputModel2)[] _outputModels;

        public CompositeOutputTest()
        {
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(ProduceOutputArg1Component).Assembly)
                .AddNamespaces("TypedWorkflowTests.OtherComponents.CompositeOutputTest")
                .BuildWithResult<(OutputModel1, OutputModel2)>();

            var tasks = Enumerable.Range(0, ITERATION_CNT).Select(i => Task.Run(() => container.Run().AsTask())).ToArray();
            Task.WaitAll(tasks);
            _outputModels = tasks.Select(t => t.Result).ToArray();
        }

        [Test]
        public void Test()
        {
            ProduceOutputArg1Component.Assert(_outputModels.Select(e => e.Item1));
            ProduceOutputArg2Component.Assert(_outputModels.Select(e => e.Item2));
        }
    }
}
