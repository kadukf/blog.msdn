using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IO;
using Newtonsoft.Json;

namespace PerfTest
{

    /*
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.3801.0
  Job-JBCAPO : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.3801.0

Runtime=Clr  MaxIterationCount=16

|         Method |      Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |----------:|----------:|----------:|-------:|------:|------:|----------:|
|    ComputeHash | 12.613 us | 0.5691 us | 0.5589 us | 0.1526 |     - |     - |     377 B |
| TransformBlock |  5.284 us | 0.1885 us | 0.1574 us |      - |     - |     - |         - |  


        BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.0.100-preview6-012264
  [Host]     : .NET Core 3.0.0-preview6-27804-01 (CoreCLR 4.700.19.30373, CoreFX 4.700.19.30308), 64bit RyuJIT
  Job-OTETGN : .NET Core 3.0.0-preview6-27804-01 (CoreCLR 4.700.19.30373, CoreFX 4.700.19.30308), 64bit RyuJIT

Runtime=Core  MaxIterationCount=16


|         Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|    ComputeHash | 2.171 us | 0.1395 us | 0.1370 us | 0.0687 |     - |     - |     144 B |
| TransformBlock | 1.238 us | 0.0283 us | 0.0278 us |      - |     - |     - |         - |


     */
    [MaxIterationCount(16)]
    //[ClrJob]
    [CoreJob]
    [MemoryDiagnoser]
    public class HmacBenchmarks
    {
        private readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();
        private readonly byte[] _data;
        private readonly HMACSHA384 _hmac;
        private readonly byte[] _output;
        private readonly RecyclableMemoryStreamManager _manager;
        private readonly ObjectPool<StringBuilder> _stringBuilderProvider = new DefaultObjectPoolProvider().CreateStringBuilderPool();
        private readonly ObjectPool<MemoryStream> _memstreamProvider = new DefaultObjectPoolProvider().Create<MemoryStream>(new MemoryStreamPooledObjectPolicy());
        private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
        private readonly ArrayPool<byte> _bytesPool = ArrayPool<byte>.Shared;

        private class MemoryStreamPooledObjectPolicy : IPooledObjectPolicy<MemoryStream>
        {
            public MemoryStream Create()
            {
                return new MemoryStream(32728);
            }

            public bool Return(MemoryStream obj)
            {
                obj.Seek(0, SeekOrigin.Begin);
                return true;
            }
        }

        public HmacBenchmarks()
        {
            _manager = new RecyclableMemoryStreamManager(500, 1048576, 134217728);
            
            _data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Profile
            {
                Mood = "I'm in good mood today",
                Language = "English",
                About = "Some few words about me",
                AvatarUrl = "https://avatar.mycompany.com/myid/myavatar",
                Website = "http://my.website.com",
                Birthday = new DateTime(2018, 5, 6),
                Locations = new[]
                {
                    new Location
                        {City = "London", Country = "United Kingdom", State = "UK"},
                    new Location
                        {City = "Prague", Country = "Czech Republic", State = "CZ"},
                }
            }));
            _hmac = new HMACSHA384();
            _output = new byte[_data.Length];
        }

        [Benchmark]
        public void ComputeHash()
        {
            _hmac.ComputeHash(_data);
        }


        [Benchmark]
        public void TransformBlock()
        {
            _hmac.TransformBlock(_data, 0, _data.Length, _output, 0);
        }

    }
}