using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TypedWorkflow;
using TypedWorkflowTests.Common;

namespace TypedWorkflowTests.Components
{
    public static class DecompositionRunner
    {
        private static SingletonComponent _singletonComponent = new SingletonComponent();
        private static MultiresultComponent _multiresultComponent = new MultiresultComponent();
        private static PartialConsumer1Component _partialConsumer1Component = new PartialConsumer1Component();
        private static PartialConsumer2Component _partialConsumer2Component = new PartialConsumer2Component();
        private static AsyncComponent _asyncComponent = new AsyncComponent();
        private static DiCiComponent _diCiComponent;
        private static WithPriorityComponent _withPriorityComponent = new WithPriorityComponent();
        private static WithPriorityComponent.WithPriorityComponent2 _withPriorityComponent2 = new WithPriorityComponent.WithPriorityComponent2();
        private static OptionalComponent _optionalComponent = new OptionalComponent();
        private static ConstrainedComponent _constrainedComponent = new ConstrainedComponent();
        private static ConstraintComponent _constraintComponent = new ConstraintComponent();
        private static ConsumerComponent _consumerComponent = new ConsumerComponent();
        private static Func<IResolver, IStringBuilder> _getStringBuilder = resolver => (IStringBuilder)resolver.Resolve(typeof(IStringBuilder));
        private static object _lock = new object();

        public static async ValueTask Run(IResolver resolver)
        {
            var singleComponent = new SingleComponent();
            if (_diCiComponent == null)
            {
                lock (_lock)
                    _diCiComponent = _diCiComponent ?? new DiCiComponent(_getStringBuilder(resolver));
            }
            var compositeComponent = new Composite.CompositeComponent();
            using var disposableComponent = new DisposableComponent();
            var сustomConstructorComponent = new CustomConstructorComponent(_getStringBuilder(resolver));

            _withPriorityComponent.HightPriority();
            _withPriorityComponent2.MediumPriority();

            singleComponent.SomeRun();

            _diCiComponent.AppendText();

            compositeComponent.SomeRun();
            compositeComponent.OtherRun();

            _singletonComponent.RunPerIteration();

            disposableComponent.Run();

            var (res1, res2) = _multiresultComponent.RunWithResult();
            var res3 = _partialConsumer1Component.Run(res1);
            var res4 = _partialConsumer2Component.Run(res1, res2);
            _multiresultComponent.RunAfterConsume(res3, res4);

            var res5 = await _asyncComponent.Run1();
            var (res6, res7) = await _asyncComponent.Run2();
            _asyncComponent.Run(res5, res6, res7);

            сustomConstructorComponent.Run();
            var res8 = _optionalComponent.ReturnNone();
            var res9 = _optionalComponent.ReturnSome();
            var res10 = res8.HasValue ? Option.Create(_optionalComponent.NeverRun1(res8.Model)) : Option<OptionalComponent.Model2>.None;
            if (res10.HasValue)
                _optionalComponent.NeverRun2(res10.Model);
            if (res9.HasValue)
                _optionalComponent.AllwaysRun(res8, res10, res9.Model);

            var res11 = _constraintComponent.GetModelFromCache();
            var res12 = res11.HasValue ? Option<ConsumerComponent.FromDb>.None: Option.Create(_constrainedComponent.GetModelFromDb());
            _consumerComponent.UseModel(res12, res11);

            _withPriorityComponent.LowPriority();
        }
    }
}
