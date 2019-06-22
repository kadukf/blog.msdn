using BenchmarkDotNet.Running;

namespace PerfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            new JsonSerializeBenchmarks().SerializeToxx();
           // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();
        }
    }
}
