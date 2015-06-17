﻿using System;

namespace Arch.CMessaging.Client.Net.Core.Buffer
{
    public class ByteBufferAllocator : IoBufferAllocator
    {
        public static readonly ByteBufferAllocator Instance = new ByteBufferAllocator();

        public IoBuffer Allocate(Int32 capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity should be >= 0", "capacity");
            return new ByteBuffer(this, capacity, capacity);
        }

        public IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length)
        {
            try
            {
                return new ByteBuffer(this, array, offset, length);
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException();
            }
        }
        public IoBuffer Wrap(Byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }
    }
}
