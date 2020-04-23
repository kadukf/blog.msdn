using BenchmarkDotNet.Running;

namespace Encodings
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var app = new Base64EncodingBenchmarks();
            //app.GlobalSetup();
            //app.UsingWebEncoders();
            var summary = BenchmarkRunner.Run<Base64EncodingBenchmarks>();
        }
    }
}