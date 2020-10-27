using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    public class OptionalComponent
    {
        private static int _neverRunCount = 0;
        private static int _allwaysRunCount = 0;

        public struct Model1 { }
        public struct Model2 { }
        public struct Model3 { }

        [TwEntrypoint]
        public Option<Model1> ReturnNone()
            => Option<Model1>.None;

        [TwEntrypoint]
        public Option<Model3> ReturnSome()
            => new Option<Model3>(new Model3());


        [TwEntrypoint]
        public Model2 NeverRun1(Model1 _)
        {
            Interlocked.Increment(ref _neverRunCount);
            return new Model2();
        }

        [TwEntrypoint]
        public void NeverRun2(Model2 _)
            => Interlocked.Increment(ref _neverRunCount);

        [TwEntrypoint]
        public void AllwaysRun(Option<Model1> none1, Option<Model2> none2, Model3 _)
        {
            if (none1.HasValue || none2.HasValue)
                return;

            Interlocked.Increment(ref _allwaysRunCount);
        }

        public static bool Assert(int iteration_cnt)
            => iteration_cnt == _allwaysRunCount && _neverRunCount == 0;

    }
}
