using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TypedWorkflow;

namespace TypedWorkflowTests.Components
{
    [TwConstraint(typeof(ConsumerComponent.FromCache), HasNone = true)]
    public class ConstrainedComponent
    {
        private static long _runCnt;

        [TwEntrypoint]
        public Option<ConsumerComponent.FromDb> GetModelFromDb()
        {
            Interlocked.Increment(ref _runCnt);
            return Option.Create(new ConsumerComponent.FromDb(new ConsumerComponent.SomeModel()));
        }

        public static bool Assert(int iteration_cnt)
            => iteration_cnt / 10 == _runCnt;
    }

    public class ConstraintComponent
    {
        private static long _runCnt;

        [TwEntrypoint]
        public Option<ConsumerComponent.FromCache> GetModelFromCache()
            => Interlocked.Increment(ref _runCnt) % 10 == 0 ?
                Option<ConsumerComponent.FromCache>.None :
                Option.Create(new ConsumerComponent.FromCache(new ConsumerComponent.SomeModel()));

        public static bool Assert(int iteration_cnt)
            => iteration_cnt == _runCnt;
    }

    public class ConsumerComponent
    {
        private static long _fromDb;
        private static long _fromCache;

        public class SomeModel { }
        public class FromDb : ModelWrapper<SomeModel>
        {
            public FromDb(SomeModel value) : base(value) { }
        }
        public class FromCache : ModelWrapper<SomeModel>
        {
            public FromCache(SomeModel value) : base(value) { }
        }

        [TwEntrypoint]
        public void UseModel(Option<FromDb> from_db, Option<FromCache> or_from_cache)
        {
            if (from_db.HasValue)
                Interlocked.Increment(ref _fromDb);

            if (or_from_cache.HasValue)
                Interlocked.Increment(ref _fromCache);
        }

        public static bool Assert(int iteration_cnt)
            => (iteration_cnt / 10 == _fromDb) && (iteration_cnt == (_fromCache + _fromDb));
    }
}
