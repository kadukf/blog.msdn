using System.Buffers;
using Newtonsoft.Json;

namespace PerfTest
{
    public class JsonArrayPool : IArrayPool<char>
    {
        public static readonly JsonArrayPool Instance = new JsonArrayPool();

        public char[] Rent(int minimumLength)
        {
            return ArrayPool<char>.Shared.Rent(minimumLength);
        }

        public void Return(char[] array)
        {
            ArrayPool<char>.Shared.Return(array);
        }
    }
}