using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PerfTest
{
    /*
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.3801.0
  Job-JBCAPO : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.8.3801.0

Runtime=Clr  MaxIterationCount=16

|                                      Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|                           SerializeOriginal | 5.656 us | 0.5383 us | 0.4772 us | 1.5488 |     - |     - |    3258 B |
|               SerializeToPooledMemoryStream | 5.845 us | 0.2205 us | 0.2063 us | 0.7019 |     - |     - |    1484 B |
|              SerializeToPooledStringBuilder | 5.660 us | 0.1895 us | 0.1773 us | 0.3052 |     - |     - |     642 B |
|                   SerializeToSystemTextJson | 5.023 us | 0.2703 us | 0.2655 us | 0.4654 |     - |     - |     987 B |
| SerializeToSystemTextJsonPooledMemoryStream | 4.826 us | 0.1401 us | 0.1170 us | 0.2213 |     - |     - |     465 B |


BenchmarkDotNet=v0.11.5, OS=Windows 10.0.18362
Intel Core i7-6600U CPU 2.60GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.0.100-preview6-012264
  [Host]     : .NET Core 3.0.0-preview6-27804-01 (CoreCLR 4.700.19.30373, CoreFX 4.700.19.30308), 64bit RyuJIT
  Job-USWLJY : .NET Core 3.0.0-preview6-27804-01 (CoreCLR 4.700.19.30373, CoreFX 4.700.19.30308), 64bit RyuJIT

Runtime=Core  MaxIterationCount=16

|                                      Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|                           SerializeOriginal | 4.923 us | 0.1784 us | 0.1752 us | 1.5259 |     - |     - |    3200 B |
|               SerializeToPooledMemoryStream | 5.022 us | 0.1479 us | 0.1453 us | 0.6561 |     - |     - |    1384 B |
|              SerializeToPooledStringBuilder | 4.792 us | 0.1587 us | 0.1485 us | 0.2823 |     - |     - |     592 B |
|                   SerializeToSystemTextJson | 3.583 us | 0.1206 us | 0.1184 us | 0.4501 |     - |     - |     944 B |
| SerializeToSystemTextJsonPooledMemoryStream | 3.554 us | 0.0966 us | 0.0949 us | 0.2174 |     - |     - |     456 B |

     */
    [MaxIterationCount(16)]
    [ClrJob]
    [CoreJob]
    [MemoryDiagnoser]
    public class JsonSerializeBenchmarks
    {
        private readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();
        private readonly Profile _data;
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

        public JsonSerializeBenchmarks()
        {
            _manager = new RecyclableMemoryStreamManager(500, 1048576, 134217728);

            _data = new Profile
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
            };
        }

        [Benchmark]
        public void SerializeOriginal()
        {
            var s = JsonConvert.SerializeObject(_data);
            var x = Encoding.UTF8.GetBytes(s);
        }

        [Benchmark]
        public void SerializeToPooledMemoryStream()
        {
            MemoryStream memory = null;
            try
            {
                memory = _memstreamProvider.Get();
                using (var writer = new StreamWriter(memory, Encoding.UTF8, 64, true))
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(writer)
                {
                    ArrayPool = JsonArrayPool.Instance
                })
                {
                    jsonTextWriter.Formatting = _serializer.Formatting;
                    _serializer.Serialize(jsonTextWriter, _data, null);
                }
            }
            finally
            {
                if (memory != null)
                {
                    _memstreamProvider.Return(memory);
                }
            }
        }

        [Benchmark]
        public void SerializeToPooledStringBuilder()
        {
            var sb = _stringBuilderProvider.Get();

            StringWriter stringWriter = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter)
            {
                ArrayPool = JsonArrayPool.Instance
            })
            {
                jsonTextWriter.Formatting = _serializer.Formatting;
                _serializer.Serialize(jsonTextWriter, _data, null);
            }
            
            var sbLength = sb.Length;
            
            char[] charBuffer = _charPool.Rent(sbLength);
            sb.CopyTo(0, charBuffer, 0, sbLength);

            var bytesLength = Encoding.UTF8.GetByteCount(charBuffer);
            var byteBuffer = _bytesPool.Rent(bytesLength);
            var utfBytesLength = Encoding.UTF8.GetBytes(charBuffer, 0, sbLength, byteBuffer, 0);
            _charPool.Return(charBuffer);
            _bytesPool.Return(byteBuffer);

            _stringBuilderProvider.Return(sb);
        }

        [Benchmark]
        public void SerializeToSystemTextJson()
        {
            var result = new MemoryStream();
            System.Text.Json.Serialization.JsonSerializer.WriteAsync(_data, result).GetAwaiter().GetResult();
        }

        [Benchmark]
        public void SerializeToSystemTextJsonPooledMemoryStream()
        {
            MemoryStream memory = null;
            try
            {
                memory = _memstreamProvider.Get();
                System.Text.Json.Serialization.JsonSerializer.WriteAsync(_data, memory).GetAwaiter().GetResult();
            }
            finally
            {
                if (memory != null)
                {
                    memory.Seek(0, SeekOrigin.Begin);
                    _memstreamProvider.Return(memory);
                }
            }
        }

    }
}