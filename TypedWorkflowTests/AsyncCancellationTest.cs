using TypedWorkflow;

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

namespace TypedWorkflowTests
{
    public class AsyncCancellationTest
    {
        [Test]
        public void WoInputAndOutput()
        {
            var componentType = typeof(TypedWorkflowTests.OtherComponents.AsyncCancellationTest.WoInputAndOutput.LongTimeExecutionComponent);
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(componentType.Assembly)
                .AddNamespaces(componentType.Namespace)
                .Build();

            var cancellation = new CancellationTokenSource();
            var t = container.Run(cancellation.Token).AsTask();

            Task.Delay(500).Wait();

            cancellation.Cancel();

            var ex = Assert.CatchAsync<TaskCanceledException>(() => t);
        }

        [Test]
        public void WithInput()
        {
            var componentType = typeof(TypedWorkflowTests.OtherComponents.AsyncCancellationTest.WithInput.LongTimeExecutionComponent);
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(componentType.Assembly)
                .AddNamespaces(componentType.Namespace)
                .Build<int>();

            var cancellation = new CancellationTokenSource();
            var t = container.Run(-1, cancellation.Token).AsTask();

            Task.Delay(500).Wait();

            cancellation.Cancel();

            var ex = Assert.CatchAsync<TaskCanceledException>(() => t);
        }

        [Test]
        public void WithInputAndOutput()
        {
            var componentType = typeof(TypedWorkflowTests.OtherComponents.AsyncCancellationTest.WithInputAndOutput.LongTimeExecutionComponent);
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(componentType.Assembly)
                .AddNamespaces(componentType.Namespace)
                .Build<int,long>();

            var cancellation = new CancellationTokenSource();
            var t = container.Run(-1, cancellation.Token).AsTask();

            Task.Delay(500).Wait();

            cancellation.Cancel();

            var ex = Assert.CatchAsync<TaskCanceledException>(() => t);
        }
    }
}
