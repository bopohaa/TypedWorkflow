using BenchmarkDotNet.Running;
using System;

namespace TypedWorkflowBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MainBenchmark>();
        }
    }
}
