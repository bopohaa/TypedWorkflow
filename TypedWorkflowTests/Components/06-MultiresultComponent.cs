using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    public class MultiresultComponent
    {
        private const int DATA1 = 2;
        private const int DATA2 = 4;

        private static int _result = 0;

        public struct SomeModel { public int Data; }
        public struct OtherModel { public int Data; }

        [TwEntrypoint]
        public (SomeModel, OtherModel) RunWithResult()
            => (new SomeModel { Data = DATA1 }, new OtherModel { Data = DATA2 });

        [TwEntrypoint]
        public void RunAfterConsume(PartialConsumer1Component.Model consumer1_res, PartialConsumer2Component.Model consumer2_res)
            => Interlocked.Add(ref _result, consumer1_res.Data + consumer2_res.Data);

        public static bool Assert(int iteration_cnt)
            => _result == (DATA1 + DATA1 + DATA2) * iteration_cnt;

    }

    public class PartialConsumer1Component
    {
        public struct Model { public int Data; }

        [TwEntrypoint]
        public Model Run(MultiresultComponent.SomeModel dependency)
            => new Model { Data = dependency.Data };
    }

    public class PartialConsumer2Component
    {
        public struct Model { public int Data; }

        [TwEntrypoint]
        public Model Run(MultiresultComponent.SomeModel dependency1, MultiresultComponent.OtherModel dependency2)
            => new Model { Data = dependency1.Data + dependency2.Data };
    }
}
