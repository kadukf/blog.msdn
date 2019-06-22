using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace PerfTest
{
    public class PooledMemoryStream : Stream
    {
        private struct ByteLocation
        {
            public ByteLocation(byte[] segment, int byteIndex, ushort segmentIndex)
            {
                Segment = segment;
                SegmentIndex = segmentIndex;
                ByteIndex = byteIndex;
            }

            public byte[] Segment { get; }
            public int ByteIndex { get; }
            public ushort SegmentIndex { get; }
        }

        private readonly IList<byte[]> segments;
        private readonly int segmentSize;
        private int length;
        private int position;

        public PooledMemoryStream() : this(32768)
        {

        }

        /// <summary>
        /// Creates new instance of <see cref="PooledMemoryStream"/> with a given segment length.
        /// </summary>
        /// <param name="segmentSize">Allowed values are 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 and 65536</param>
        public PooledMemoryStream(int segmentSize)
            : this(segmentSize, new List<byte[]>(), 0)
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="PooledMemoryStream"/> from the given segments and with a given segment length.
        /// </summary>
        /// <param name="segments">List of segments the stream is constructed from.</param>
        /// <param name="segmentSize">Allowed values are 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 and 65536.</param>
        /// <param name="length">Length of the pooled memory stream. Must fit within segmentSize * segments.Count.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="segmentSize"/> is not from an allowed set.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="segments"/> is null.</exception>
        private PooledMemoryStream(int segmentSize, IList<byte[]> segments, int length)
        {
            ValidateParameters(segmentSize, segments, length);

            this.segmentSize = segmentSize;
            this.segments = segments ?? throw new ArgumentNullException(nameof(segments));
            this.length = length;
            position = 0;
        }

        private static void ValidateParameters(int segmentSize, IList<byte[]> segments, int length)
        {
            if (segmentSize < 128 ||
                segmentSize > 65536 ||
                !IsPowerOf2(segmentSize))
            {
                throw new ArgumentException($"{nameof(segmentSize)} must be power of 2, at least 1024 and less or equal to 65536", nameof(segmentSize));
            }

            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            if (length < 0)
            {
                throw new ArgumentException("Length can't be negative.", nameof(length));
            }

            var segmentCount = (int)Math.Ceiling(length / (double)segmentSize);
            if (segments.Count != segmentCount)
            {
                throw new ArgumentException("Segments count mismatch.");
            }

            foreach (var segment in segments)
            {
                if (segment.Length != segmentSize)
                {
                    throw new ArgumentException("All segments must be of the same size.", nameof(segments));
                }
            }
        }

        private static void ValidateReadWriteParameters(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentException("count outside of buffer");
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var segment in segments)
                {
                    ArrayPool<byte>.Shared.Return(segment);
                }
                segments.Clear();
            }
        }

        public override void Flush()
        {
        }

        public override int ReadByte()
        {
            if (position == length)
            {
                return -1;
            }

            var location = EnsureSegmentIsCreatedAndGetByteLocation(position++);
            return location.Segment[location.ByteIndex];
        }

        public override void WriteByte(byte value)
        {
            var location = EnsureSegmentIsCreatedAndGetByteLocation(position);
            location.Segment[location.ByteIndex] = value;

            position += 1;
            length += 1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteParameters(buffer, offset, count);

            if (position == length)
            {
                return 0;
            }

            ByteLocation location = EnsureSegmentIsCreatedAndGetByteLocation(position);
            int written = 0;
            int byteIndex = location.ByteIndex;
            int nextSegmentIndex = location.SegmentIndex - 1;
            var bytesToRead = Math.Min(count, length - position);

            while (true)
            {
                if (++nextSegmentIndex >= segments.Count)
                {
                    // there are no more segments left
                    return written;
                }

                // advance to the next segment
                byte[] nextSegment = segments[nextSegmentIndex];
                var toWrite = bytesToRead - written;
                var toWriteFromCurrentSegment = nextSegment.Length - byteIndex;
                var writeCount = Math.Min(toWrite, toWriteFromCurrentSegment);

                Buffer.BlockCopy(nextSegment, byteIndex, buffer, offset + written, writeCount);
                position += writeCount;
                written += writeCount;

                if (toWrite <= toWriteFromCurrentSegment)
                {
                    // if the remainder of data was within the current segment, we're done
                    return written;
                }

                // reset byte index as we either end up at a new segment or return to caller on the next iteration
                byteIndex = 0;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            int intOffset = GetInt32Offset(offset);

            switch (origin)
            {
                case SeekOrigin.Begin: return SeekFromBegining(intOffset);
                case SeekOrigin.Current: return SeekFromCurrent(intOffset);
                case SeekOrigin.End: return SeekFromEnd(intOffset);
            }

            throw new NotImplementedException();
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteParameters(buffer, offset, count);

            int toWrite = count;
            int localOffset = offset;
            while (true)
            {
                ByteLocation location = EnsureSegmentIsCreatedAndGetByteLocation(position);

                var remainingSpace = segmentSize - location.ByteIndex;
                if (toWrite <= remainingSpace)
                {
                    Buffer.BlockCopy(buffer, localOffset, location.Segment, location.ByteIndex, toWrite);
                    position += toWrite;
                    length += toWrite;

                    return;
                }

                Buffer.BlockCopy(buffer, localOffset, location.Segment, location.ByteIndex, remainingSpace);
                toWrite -= remainingSpace;
                position += remainingSpace;
                length += remainingSpace;
                localOffset += remainingSpace;
            }
        }

        private static int GetInt32Offset(long offset)
        {
            int intOffset = 0;
            if (offset > int.MaxValue)
            {
                intOffset = int.MaxValue;
            }
            else if (offset < int.MinValue)
            {
                intOffset = int.MinValue;
            }
            else
            {
                intOffset = (int)offset;
            }

            return intOffset;
        }

        private int SeekFromBegining(int offset)
        {
            if (offset <= 0)
            {
                position = 0;
            }
            else
            {
                var lastIndex = GetLastIndex();
                position = offset > lastIndex ? lastIndex : offset;
            }

            return position;
        }

        private int SeekFromCurrent(int offset)
        {
            if (offset < 0)
            {
                var newPosition = position + offset;
                position = newPosition > 0 ? newPosition : 0;
            }
            else
            {
                var lastIndex = GetLastIndex();
                var nextPosition = position + offset;
                position = nextPosition > lastIndex ? lastIndex : nextPosition;
            }

            return position;
        }

        private int SeekFromEnd(int offset)
        {
            if (offset == 0)
            {
                position = length > 1 ? length - 1 : 0;
            }
            else if (offset < 0)
            {
                var newPosition = length + offset;
                position = newPosition > 0 ? newPosition : 0;
            }
            else
            {
                position = length > 0 ? length - 1 : 0;
            }

            return position;
        }

        private int GetLastIndex() => length - 1 > 0 ? length - 1 : 0;

        private ByteLocation EnsureSegmentIsCreatedAndGetByteLocation(int index)
        {
            var byteIndex = index;
            var segmentIndex = byteIndex / segmentSize;
            var segmentOffset = byteIndex % segmentSize;

            byte[] segment = null;
            if (segments.Count > segmentIndex)
            {
                segment = segments[segmentIndex];
            }
            else
            {
                segment = ArrayPool<byte>.Shared.Rent(segmentSize);
                segments.Add(segment);
            }

            return new ByteLocation(segment, segmentOffset, (ushort)segmentIndex);
        }

        private static bool IsPowerOf2(int value) => (value & value - 1) == 0;
    }
}