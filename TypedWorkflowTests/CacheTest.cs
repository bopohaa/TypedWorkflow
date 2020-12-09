using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TypedWorkflow;

using TypedWorkflowTests.OtherComponents.CacheTest;

namespace TypedWorkflowTests
{
    public class CacheTest
    {
        private const int ITERATION_CNT = 100;
        private const int KEYS_CNT = 10;

        private long[] _outputModels = new long[ITERATION_CNT];

        public CacheTest()
        {
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(SlowProducerComponent).Assembly)
                .AddNamespaces(typeof(SlowProducerComponent).Namespace)
                .WithCache(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1))
                .Build<int, long>();

            var tasks = new Task<long>[ITERATION_CNT];
            for (var i = 0; i < ITERATION_CNT; i++)
            {
                var key = i % KEYS_CNT;
                if (key == 0)
                    Task.Delay(500).Wait();
                tasks[i] = Task.Run(() => container.Run(key).AsTask());
            }
            for (var i = 0; i < ITERATION_CNT; i++)
                _outputModels[i] = tasks[i].Result;

        }

        [Test]
        public void Test()
        {
            var res = _outputModels.GroupBy(e => e & 0xffffffff).ToDictionary(e => e.Key, e => e.Select(i => i >> 32).ToArray()).ToArray();
            var uniq = res.Select(e => (e.Key, Distinct: e.Value.Distinct().ToArray(), e.Value.Length)).ToArray();
            var total = uniq.SelectMany(e => e.Distinct).OrderBy(e=>e).ToArray();

            //There must be fewer unique values than non-unique values
            Assert.IsFalse(uniq.Any(e => e.Distinct.Length >= e.Length));

            //The total number of unique values must be equal to the total number of method executions
            Assert.IsTrue(total.Length == SlowProducerComponent.RunCnt);
            Assert.IsTrue(total.Length == total.ToHashSet().Count);
        }

    }
}
