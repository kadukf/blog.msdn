using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Encodings
{
    public static class Base64Encoder
    {
        private static int _guidSize = Marshal.SizeOf<Guid>();

        public static string UsingWebEncoders(string input)
        {
            byte[] inputBytes = null;
            char[] outputChars = null;

            try
            {
                var minimumLength = Encoding.UTF8.GetByteCount(input);
                inputBytes = ArrayPool<byte>.Shared.Rent(minimumLength);
                int inputBytesCount = Encoding.UTF8.GetBytes(input, 0, input.Length, inputBytes, 0);

                var outputSize = WebEncoders.GetArraySizeRequiredToEncode(inputBytesCount);
                outputChars = ArrayPool<char>.Shared.Rent(outputSize);

                var charsSize = WebEncoders.Base64UrlEncode(inputBytes, 0, outputChars, 0, inputBytesCount);
                return new string(outputChars, 0, charsSize);
            }
            finally
            {
                if (inputBytes != null) ArrayPool<byte>.Shared.Return(inputBytes);
                if (outputChars != null) ArrayPool<char>.Shared.Return(outputChars);
            }
        }

        public static string UsingSpanBase64(Guid oid, params string[] input)
        {
            var totalUtf8Bytes = 36; // GUID.Format(D) = 36 bytes
            for (int i = 0; i < input.Length; i++)
            {
                totalUtf8Bytes += Encoding.UTF8.GetByteCount(input[i]);
            }

            Span<byte> resultSpan = stackalloc byte[Base64.GetMaxEncodedToUtf8Length(totalUtf8Bytes)];
            if (!Utf8Formatter.TryFormat(oid, resultSpan, out int writtenBytes) || writtenBytes != 36)
            {
                throw new ArithmeticException();
            }

            for (int i = 0; i < input.Length; i++)
            {
                var ixs = EncodingHelper.GetUtf8Bytes(input[i].AsSpan(), resultSpan.Slice(writtenBytes));
                writtenBytes += ixs;
            }

            OperationStatus status = Base64.EncodeToUtf8InPlace(resultSpan, totalUtf8Bytes, out int base64Written);
            if (status != OperationStatus.Done)
            {
                throw new ArithmeticException();
            }

            var base64String = EncodingHelper.GetUrlSafeString(resultSpan.Slice(0, base64Written));
            return base64String;
        }

        public static string UsingSpanBase64BinaryGuid(Guid oid, params string[] input)
        {
            var totalUtf8Bytes = _guidSize;
            for (int i = 0; i < input.Length; i++)
            {
                totalUtf8Bytes += Encoding.UTF8.GetByteCount(input[i]);
            }

            Span<byte> resultSpan = stackalloc byte[Base64.GetMaxEncodedToUtf8Length(totalUtf8Bytes)];
            if (!MemoryMarshal.TryWrite(resultSpan, ref oid))
            {
                throw new ArithmeticException();
            }

            int writtenBytes = _guidSize;

            for (int i = 0; i < input.Length; i++)
            {
                var ixs = EncodingHelper.GetUtf8Bytes(input[i].AsSpan(), resultSpan.Slice(writtenBytes));
                writtenBytes += ixs;
            }

            OperationStatus status = Base64.EncodeToUtf8InPlace(resultSpan, totalUtf8Bytes, out int base64Written);
            if (status != OperationStatus.Done)
            {
                throw new ArithmeticException();
            }

            var base64String = EncodingHelper.GetUrlSafeString(resultSpan.Slice(0, base64Written));
            return base64String;
        }

        private static class EncodingHelper
        {
            private const byte EqualByte = (byte)'=';
            private const byte ForwardSlashByte = (byte)'/';
            private const byte PlusByte = (byte)'+';
            private const char Underscore = '_';
            private const char Dash = '-';

            public static string GetUrlSafeString(Span<byte> bytes)
            {
                Span<char> chars = stackalloc char[bytes.Length];

                for (var i = 0; i < bytes.Length; i++)
                {
                    switch (bytes[i])
                    {
                        case ForwardSlashByte:
                            chars[i] = Underscore;
                            break;
                        case PlusByte:
                            chars[i] = Dash;
                            break;
                        case EqualByte:
                            return chars.ToString();
                        default:
                            chars[i] = (char)bytes[i];
                            break;
                    }
                }

                return chars.ToString();
            }

            public static unsafe int GetUtf8Bytes(ReadOnlySpan<char> chars, Span<byte> bytes)
            {
                fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                {
                    return Encoding.UTF8.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
                }
            }
        }
    }
}