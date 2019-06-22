using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PerfTest
{
    public partial class JsonSerializeBenchmarks
    {
        public class LongArrayConverter : JsonConverter<Memory<byte>>
        {
            public LongArrayConverter() { }

            public override Memory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return null;
            }

            public override void Write(Utf8JsonWriter writer, Memory<byte> value, JsonSerializerOptions options)
            {
                writer.WriteBase64StringValue(value.Span);
            }

            public override void Write(Utf8JsonWriter writer, long[] value, JsonEncodedText propertyName, JsonSerializerOptions options)
            {
                var builder = new StringBuilder();

                for (int i = 0; i < value.Length; i++)
                {
                    builder.Append(value[i].ToString());

                    if (i != value.Length - 1)
                    {
                        builder.Append(",");
                    }
                }

                writer.WriteString(propertyName, builder.ToString());
            }
        }
    }
}