using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TypedWorkflow;

namespace TypedWorkflowTests
{
    public class CacheTest
    {
        private const int ITERATION_CNT = 100;
        private const int KEYS_CNT = 10;
        private const int BATCH_KEYS_CNT = 5;

        [Test]
        public void SimpleCacheTest()
        {
            var outputModels = StartTwWithCache();

            var res = outputModels.GroupBy(e => e & 0xffffffff).ToDictionary(e => e.Key, e => e.Select(i => i >> 32).ToArray()).ToArray();
            var uniq = res.Select(e => (e.Key, Distinct: e.Value.Distinct().ToArray(), e.Value.Length)).ToArray();
            var total = uniq.SelectMany(e => e.Distinct).OrderBy(e => e).ToArray();

            //There must be fewer unique values than non-unique values
            Assert.IsFalse(uniq.Any(e => e.Distinct.Length >= e.Length));

            //The total number of unique values must be equal to the total number of method executions
            Assert.IsTrue(total.Length == OtherComponents.CacheTest.SlowProducerComponent.RunCnt);
            Assert.IsTrue(total.Length == total.ToHashSet().Count);
        }

        [Test]
        public void CacheBatchTest()
        {
            var outputModels = StartTwWithCacheBatch();

            var res = outputModels.SelectMany(e=>e).GroupBy(e => e.Key).ToDictionary(e => e.Key, e => e.Select(i=>i.Value).ToArray()).ToArray();
            var uniq = res.Select(e => (e.Key, Distinct: e.Value.Distinct().ToArray(), e.Value.Length)).ToArray();
            var total = uniq.SelectMany(e => e.Distinct).OrderBy(e => e).ToArray();

            //There must be fewer unique values than non-unique values
            Assert.IsFalse(uniq.Any(e => e.Distinct.Length >= e.Length));

            //The total number of unique values must be equal to the total number of method executions
            Assert.IsTrue(total.Length == OtherComponents.CacheBatchTest.SlowProducerComponent.RunCnt);
            Assert.IsTrue(total.Length == total.ToHashSet().Count);
        }


        private long[] StartTwWithCache()
        {
            var outputModels = new long[ITERATION_CNT];
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(OtherComponents.CacheTest.SlowProducerComponent).Assembly)
                .AddNamespaces(typeof(OtherComponents.CacheTest.SlowProducerComponent).Namespace)
                .BuildWithCache<int, long>(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));

            var tasks = new Task<long>[ITERATION_CNT];
            for (var i = 0; i < ITERATION_CNT; i++)
            {
                var key = i % KEYS_CNT;
                if (key == 0)
                    Task.Delay(500).Wait();
                tasks[i] = Task.Run(() => container.Run(key).AsTask());
            }
            for (var i = 0; i < ITERATION_CNT; i++)
                outputModels[i] = tasks[i].Result;

            return outputModels;
        }

        private KeyValuePair<int, long>[][] StartTwWithCacheBatch()
        {
            var outputModels = new KeyValuePair<int, long>[ITERATION_CNT / BATCH_KEYS_CNT][];
            var builder = new TwContainerBuilder();
            var container = builder
                .AddAssemblies(typeof(OtherComponents.CacheBatchTest.SlowProducerComponent).Assembly)
                .AddNamespaces(typeof(OtherComponents.CacheBatchTest.SlowProducerComponent).Namespace)
                .BuildWithCacheBatch<int, long>(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));

            var iterations = ITERATION_CNT / BATCH_KEYS_CNT;
            var tasks = new Task<IEnumerable<KeyValuePair<int, long>>>[iterations];
            for (var i = 0; i < ITERATION_CNT; i += BATCH_KEYS_CNT)
            {
                var key = i % KEYS_CNT;
                if (key == 0)
                    Task.Delay(500).Wait();

                tasks[i / BATCH_KEYS_CNT] = Task.Run(() => container.Run(Enumerable.Range(key, BATCH_KEYS_CNT)).AsTask());
            }
            for (var i = 0; i < iterations; i++)
                outputModels[i] = tasks[i].Result.ToArray();

            return outputModels;
        }

    }
}
