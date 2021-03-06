using NUnit.Framework;

using System.Linq;
using System.Threading.Tasks;

using TypedWorkflow;

using TypedWorkflowTests.Common;
using TypedWorkflowTests.Components;
using TypedWorkflowTests.Components.Composite;

namespace TypedWorkflowTests
{
    public class BasicTest
    {
        private const int ITERATION_CNT = 100;

        private int _resolveCnt;
        private int _createScopeResolverCnt;
        private string _serviceResult;

        public BasicTest()
        {
            var resolver = new Resolver();
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(SingleComponent).Assembly)
                .AddNamespaces("TypedWorkflowTests.Components")
                .RegisterExternalDi(resolver)
                .Build();

            var tasks = Enumerable.Range(0, ITERATION_CNT).Select(i => Task.Run(() => container.Run().AsTask())).ToArray();
            //var tasks = Enumerable.Range(0, ITERATION_CNT).Select(i => Task.Run(() => DecompositionRunner.Run(resolver).AsTask())).ToArray();
            Task.WaitAll(tasks);

            _resolveCnt = resolver.ResolveCount;
            _createScopeResolverCnt = resolver.CreateScopeCnt;
            _serviceResult = resolver.Sb.ToString();
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ResolveTest()
        {
            // SingletonComponent.Init + MixedSingletonComponent.Init + MixedSingletonComponent*ITERATION_CNT + DiCiComponent*ITERATION_CNT + CustomConstructorComponent*ITERATION_CNT
            Assert.AreEqual(2 + ITERATION_CNT * 3, _resolveCnt);
            Assert.AreEqual(ITERATION_CNT, _createScopeResolverCnt);
        }

        [Test]
        public void SingleComponentTest() =>
            Assert.IsTrue(SingleComponent.Assert(ITERATION_CNT));

        [Test]
        public void DiCiComponentTest() =>
            Assert.IsTrue(DiCiComponent.Assert(_serviceResult, ITERATION_CNT));

        [Test]
        public void CompositeComponentTest() =>
            Assert.IsTrue(CompositeComponent.Assert(ITERATION_CNT));

        [Test]
        public void SingletonComponentTest() =>
            Assert.IsTrue(SingletonComponent.Assert(ITERATION_CNT));

        [Test]
        public void DisposableComponentTest() =>
            Assert.IsTrue(DisposableComponent.Assert(ITERATION_CNT));

        [Test]
        public void MultiresultComponentTest() =>
            Assert.IsTrue(MultiresultComponent.Assert(ITERATION_CNT));

        [Test]
        public void AsyncComponentTest() =>
            Assert.IsTrue(AsyncComponent.Assert(ITERATION_CNT));

        [Test]
        public void CustomConstructorComponentTest() =>
            Assert.IsTrue(CustomConstructorComponent.Assert(_serviceResult, ITERATION_CNT));

        [Test]
        public void WithPriorityComponentTest() =>
            Assert.IsTrue(WithPriorityComponent.Assert(ITERATION_CNT));

        [Test]
        public void OptionalComponentTest() =>
            Assert.IsTrue(OptionalComponent.Assert(ITERATION_CNT));

        [Test]
        public void ConstraintComponentTest()
        {
            Assert.IsTrue(ConstrainedComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(ConstraintComponent.Assert(ITERATION_CNT));
            Assert.IsTrue(ConsumerComponent.Assert(ITERATION_CNT));
        }


    }
}