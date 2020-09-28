using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;
using TypedWorkflow;
using TypedWorkflowTests.Common;
using TypedWorkflowTests.Components;
using TypedWorkflowTests.Components.Composite;

namespace TypedWorkflowTests
{
    public class WorkflowBuilderTest
    {
        private const int ITERATION_CNT = 100;
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var resolver = new Resolver();
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(SingleComponent).Assembly)
                .AddNamespaces("TypedWorkflowTests.Components")
                .RegisterExternalDi(resolver)
                .Build();

            var tasks = new Task[ITERATION_CNT];
            for (var i = 0; i < ITERATION_CNT; i++)
                tasks[i] = Task.Run(async () => await container.Run());
            Task.WaitAll(tasks);
            var builderResult = resolver.Sb.ToString();

            Assert.AreEqual(1 + ITERATION_CNT, resolver.ResolveCount);
            Assert.IsTrue(SingleComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(DiCiComponent.Assert(builderResult, ITERATION_CNT));
            Assert.IsTrue(CompositeComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(SingletonComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(DisposableComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(MultiresultComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(AsyncComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(CustomConstructorComponent.Assert(builderResult, ITERATION_CNT));
            Assert.IsTrue(WithPriorityComponent.Assert(ITERATION_CNT));
        }
    }
}