using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypedWorkflow;
using TypedWorkflowTests.Common;
using TypedWorkflowTests.Components;

namespace TypedWorkflowBenchmark
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    public class MainBenchmark
    {
        private IResolver _resolver;
        private ITwContainer _container;

        public MainBenchmark()
        {
            _resolver = new Resolver();
            var builder = new TwContainerBuilder();
            _container = builder
                .AddAssemblies(typeof(SingleComponent).Assembly)
                .AddNamespaces("TypedWorkflowTests.Components")
                .RegisterExternalDi(_resolver)
                .Build();
        }

        [Benchmark]
        public void DecompositionMultithread()
        {
            var tasks = Enumerable.Range(0, 4).Select(i => Task.Run(() => DecompositionRunner.Run(_resolver).AsTask())).ToArray();
            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void WorkflowMultithread()
        {
            var tasks = Enumerable.Range(0, 4).Select(i => Task.Run(() => _container.Run().AsTask())).ToArray();
            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void DecompositionSinglethread()
        {
            var res = DecompositionRunner.Run(_resolver);
            if (!res.IsCompleted)
                res.AsTask().Wait();
        }

        [Benchmark]
        public void WorkflowSinglethread()
        {
            var res = _container.Run();
            if (!res.IsCompleted)
                res.AsTask().Wait();
        }
    }
}
