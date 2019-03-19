﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET40
namespace DotNetty.Buffers
{
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using DotNetty.Common;

    public partial class CompositeByteBuffer
    {
        protected internal override ReadOnlyMemory<byte> _GetReadableMemory(int index, int count)
        {
            if (0u >= (uint)count) { return ReadOnlyMemory<byte>.Empty; }

            switch (this.componentCount)
            {
                case 0:
                    return ReadOnlyMemory<byte>.Empty;
                case 1:
                    ComponentEntry c = this.components[0];
                    IByteBuffer buf = c.Buffer;
                    if (buf.IoBufferCount == 1)
                    {
                        return buf.GetReadableMemory(c.Idx(index), count);
                    }
                    break;
            }

            var merged = new Memory<byte>(new byte[count]);
            var buffers = this.GetSequence(index, count);

            int offset = 0;
            foreach (ReadOnlyMemory<byte> buf in buffers)
            {
                Debug.Assert(merged.Length - offset >= buf.Length);

                buf.CopyTo(merged.Slice(offset));
                offset += buf.Length;
            }

            return merged;
        }

        protected internal override ReadOnlySpan<byte> _GetReadableSpan(int index, int count)
        {
            if (0u >= (uint)count) { return ReadOnlySpan<byte>.Empty; }

            switch (this.componentCount)
            {
                case 0:
                    return ReadOnlySpan<byte>.Empty;
                case 1:
                    //ComponentEntry c = this.components[0];
                    //return c.Buffer.GetReadableSpan(index, count);
                    ComponentEntry c = this.components[0];
                    IByteBuffer buf = c.Buffer;
                    if (buf.IoBufferCount == 1)
                    {
                        return buf.GetReadableSpan(c.Idx(index), count);
                    }
                    break;
            }

            var merged = new Memory<byte>(new byte[count]);
            var buffers = this.GetSequence(index, count);

            int offset = 0;
            foreach (ReadOnlyMemory<byte> buf in buffers)
            {
                Debug.Assert(merged.Length - offset >= buf.Length);

                buf.CopyTo(merged.Slice(offset));
                offset += buf.Length;
            }

            return merged.Span;
        }

        public override ReadOnlySequence<byte> GetSequence(int index, int count)
        {
            this.CheckIndex(index, count);
            if (0u >= (uint)count) { return ReadOnlySequence<byte>.Empty; }

            var buffers = ThreadLocalList<ReadOnlyMemory<byte>>.NewInstance(this.componentCount);
            try
            {
                int i = this.ToComponentIndex0(index);
                while (count > 0)
                {
                    ComponentEntry c = this.components[i];
                    IByteBuffer s = c.Buffer;
                    int localLength = Math.Min(count, c.EndOffset - index);
                    switch (s.IoBufferCount)
                    {
                        case 0:
                            ThrowHelper.ThrowNotSupportedException();
                            break;
                        case 1:
                            buffers.Add(s.GetReadableMemory(c.Idx(index), localLength));
                            break;
                        default:
                            var sequence = s.GetSequence(c.Idx(index), localLength);
                            foreach (var memory in sequence)
                            {
                                buffers.Add(memory);
                            }
                            break;
                    }

                    index += localLength;
                    count -= localLength;
                    i++;
                }

                return ReadOnlyBufferSegment.Create(buffers);
            }
            finally
            {
                buffers.Return();
            }
        }

        protected internal override Memory<byte> _GetMemory(int index, int count)
        {
            if (0u >= (uint)count) { return Memory<byte>.Empty; }

            switch (this.componentCount)
            {
                case 0:
                    return Memory<byte>.Empty;
                case 1:
                    ComponentEntry c = this.components[0];
                    return c.Buffer.GetMemory(index, count);
                default:
                    throw new NotSupportedException();
            }
        }

        protected internal override Span<byte> _GetSpan(int index, int count)
        {
            if (0u >= (uint)count) { return Span<byte>.Empty; }

            switch (this.componentCount)
            {
                case 0:
                    return Span<byte>.Empty;
                case 1:
                    ComponentEntry c = this.components[0];
                    return c.Buffer.GetSpan(index, count);
                default:
                    throw new NotSupportedException();
            }
        }

        public override IByteBuffer SetBytes(int index, ReadOnlySpan<byte> src)
        {
            var length = src.Length;
            this.CheckIndex(index, length);
            if (0u >= (uint)length) { return this; }

            var srcIndex = 0;
            int i = this.ToComponentIndex0(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                int localLength = Math.Min(length, c.EndOffset - index);
                c.Buffer.SetBytes(c.Idx(index), src.Slice(srcIndex, localLength));
                index += localLength;
                srcIndex += localLength;
                length -= localLength;
                i++;
            }
            return this;
        }

        public override IByteBuffer SetBytes(int index, ReadOnlyMemory<byte> src)
        {
            var length = src.Length;
            this.CheckIndex(index, length);
            if (0u >= (uint)length) { return this; }

            var srcIndex = 0;
            int i = this.ToComponentIndex0(index);
            while (length > 0)
            {
                ComponentEntry c = this.components[i];
                int localLength = Math.Min(length, c.EndOffset - index);
                c.Buffer.SetBytes(c.Idx(index), src.Slice(srcIndex, localLength));
                index += localLength;
                srcIndex += localLength;
                length -= localLength;
                i++;
            }
            return this;
        }
    }
}
#endif