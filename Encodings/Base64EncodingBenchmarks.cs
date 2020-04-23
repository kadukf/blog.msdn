using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.IdentityModel.Tokens;

namespace Encodings
{

    /*
 
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18363
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT
  Job-TISOER : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4121.0
  Job-FHNXAH : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT

Server=True  IterationCount=16

|                       Method | Runtime | DataLen |       Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |-------- |-------- |-----------:|----------:|-----------:|-------:|------:|------:|----------:|
|        UsingBase64UrlEncoder |     Clr |      16 | 1,683.4 ns | 125.39 ns | 123.150 ns | 1.0052 |     - |     - |    2110 B |
|             UsingWebEncoders |     Clr |      16 | 1,235.9 ns | 129.04 ns | 120.702 ns | 0.2708 |     - |     - |     570 B |
|           UsingBuffersBase64 |     Clr |      16 | 1,024.0 ns |  41.60 ns |  40.861 ns | 0.1526 |     - |     - |     321 B |
| UsingBuffersBase64BinaryGuid |     Clr |      16 |   805.6 ns |  23.72 ns |  23.294 ns | 0.1259 |     - |     - |     265 B |

|        UsingBase64UrlEncoder |    Core |      16 | 1,164.2 ns |  35.89 ns |  33.568 ns | 0.0610 |     - |     - |    1480 B |
|             UsingWebEncoders |    Core |      16 |   970.8 ns |  14.75 ns |  14.488 ns | 0.0172 |     - |     - |     464 B |
|           UsingBuffersBase64 |    Core |      16 |   683.6 ns |  10.00 ns |   8.867 ns | 0.0134 |     - |     - |     320 B |
| UsingBuffersBase64BinaryGuid |    Core |      16 |   540.0 ns |  36.79 ns |  36.130 ns | 0.0105 |     - |     - |     264 B |

|        UsingBase64UrlEncoder |     Clr |     128 | 7,238.3 ns | 603.34 ns | 564.364 ns | 3.8757 |     - |     - |    8139 B |
|             UsingWebEncoders |     Clr |     128 | 4,159.8 ns | 266.15 ns | 235.935 ns | 0.9918 |     - |     - |    2094 B |
|           UsingBuffersBase64 |     Clr |     128 | 5,264.9 ns | 522.72 ns | 513.376 ns | 0.6714 |     - |     - |    1412 B |
| UsingBuffersBase64BinaryGuid |     Clr |     128 | 4,752.4 ns | 119.11 ns | 116.981 ns | 0.6485 |     - |     - |    1364 B |

|        UsingBase64UrlEncoder |    Core |     128 | 4,798.7 ns | 273.75 ns | 256.070 ns | 0.2213 |     - |     - |    5312 B |
|             UsingWebEncoders |    Core |     128 | 3,302.9 ns |  31.36 ns |  30.797 ns | 0.0801 |     - |     - |    1984 B |
|           UsingBuffersBase64 |    Core |     128 | 2,992.6 ns |  72.14 ns |  63.951 ns | 0.0572 |     - |     - |    1408 B |
| UsingBuffersBase64BinaryGuid |    Core |     128 | 2,992.0 ns | 435.92 ns | 428.133 ns | 0.0572 |     - |     - |    1360 B |

     *
     * */

    [GcServer(true)]
    [IterationCount(16)]
    [MemoryDiagnoser]
    [CoreJob, ClrJob]
    public class Base64EncodingBenchmarks
    {
        private Guid _guid;
        private string _part0;
        private string _part1;

        [Params(16, 128)]
        public int DataLen { get; set; } = 128;
        
        [GlobalSetup]
        public void GlobalSetup()
        {
            var rnd = new Random();
            
            _guid = Guid.NewGuid();
            var buffer = new byte[DataLen];
            rnd.NextBytes(buffer);
            _part0 = Encoding.UTF8.GetString(buffer);
            rnd.NextBytes(buffer);
            _part1 = Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /*
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18363
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT
  Job-TISOER : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4121.0
  Job-FHNXAH : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT

Server=True  IterationCount=16

|                       Method | Runtime | DataLen |       Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |-------- |-------- |-----------:|----------:|-----------:|-------:|------:|------:|----------:|
|        UsingBase64UrlEncoder |     Clr |      16 | 1,683.4 ns | 125.39 ns | 123.150 ns | 1.0052 |     - |     - |    2110 B |
|        UsingBase64UrlEncoder |    Core |      16 | 1,164.2 ns |  35.89 ns |  33.568 ns | 0.0610 |     - |     - |    1480 B |
|        UsingBase64UrlEncoder |     Clr |     128 | 7,238.3 ns | 603.34 ns | 564.364 ns | 3.8757 |     - |     - |    8139 B |
|        UsingBase64UrlEncoder |    Core |     128 | 4,798.7 ns | 273.75 ns | 256.070 ns | 0.2213 |     - |     - |    5312 B |
         
         */
        /// </summary>
        [Benchmark]
        public void UsingBase64UrlEncoder()
        {
            Base64UrlEncoder.Encode($"{_guid}{_part0}{_part1}");
        }

        /// <summary>
        /*
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18363
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT
  Job-TISOER : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4121.0
  Job-FHNXAH : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT

Server=True  IterationCount=16

|                       Method | Runtime | DataLen |       Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |-------- |-------- |-----------:|----------:|-----------:|-------:|------:|------:|----------:|
|             UsingWebEncoders |     Clr |      16 | 1,235.9 ns | 129.04 ns | 120.702 ns | 0.2708 |     - |     - |     570 B |
|             UsingWebEncoders |    Core |      16 |   970.8 ns |  14.75 ns |  14.488 ns | 0.0172 |     - |     - |     464 B |
|             UsingWebEncoders |     Clr |     128 | 4,159.8 ns | 266.15 ns | 235.935 ns | 0.9918 |     - |     - |    2094 B |
|             UsingWebEncoders |    Core |     128 | 3,302.9 ns |  31.36 ns |  30.797 ns | 0.0801 |     - |     - |    1984 B |
         
         */
        /// </summary>
        [Benchmark]
        public void UsingWebEncoders()
        {
            Base64Encoder.UsingWebEncoders($"{_guid}{_part0}{_part1}");
        }

        /// <summary>
        /*
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18363
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT
  Job-TISOER : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4121.0
  Job-FHNXAH : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT

Server=True  IterationCount=16

|                       Method | Runtime | DataLen |       Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |-------- |-------- |-----------:|----------:|-----------:|-------:|------:|------:|----------:|
|           UsingBuffersBase64 |     Clr |      16 | 1,024.0 ns |  41.60 ns |  40.861 ns | 0.1526 |     - |     - |     321 B |
|           UsingBuffersBase64 |    Core |      16 |   683.6 ns |  10.00 ns |   8.867 ns | 0.0134 |     - |     - |     320 B |
|           UsingBuffersBase64 |     Clr |     128 | 5,264.9 ns | 522.72 ns | 513.376 ns | 0.6714 |     - |     - |    1412 B |
|           UsingBuffersBase64 |    Core |     128 | 2,992.6 ns |  72.14 ns |  63.951 ns | 0.0572 |     - |     - |    1408 B |
         
         */
        /// </summary>
        [Benchmark]
        public void UsingBuffersBase64()
        {
            Base64Encoder.UsingSpanBase64(_guid, _part0, _part1);
        }

        /// <summary>
        /*

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18363
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT
  Job-TISOER : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.4121.0
  Job-FHNXAH : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), 64bit RyuJIT

Server=True  IterationCount=16

|                       Method | Runtime | DataLen |       Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |-------- |-------- |-----------:|----------:|-----------:|-------:|------:|------:|----------:|
| UsingBuffersBase64BinaryGuid |     Clr |      16 |   805.6 ns |  23.72 ns |  23.294 ns | 0.1259 |     - |     - |     265 B |
| UsingBuffersBase64BinaryGuid |    Core |      16 |   540.0 ns |  36.79 ns |  36.130 ns | 0.0105 |     - |     - |     264 B |
| UsingBuffersBase64BinaryGuid |     Clr |     128 | 4,752.4 ns | 119.11 ns | 116.981 ns | 0.6485 |     - |     - |    1364 B |
| UsingBuffersBase64BinaryGuid |    Core |     128 | 2,992.0 ns | 435.92 ns | 428.133 ns | 0.0572 |     - |     - |    1360 B |
         
         */
        /// </summary>
        [Benchmark]
        public void UsingBuffersBase64BinaryGuid()
        {
            Base64Encoder.UsingSpanBase64BinaryGuid(_guid, _part0, _part1);
        }

    }
}
