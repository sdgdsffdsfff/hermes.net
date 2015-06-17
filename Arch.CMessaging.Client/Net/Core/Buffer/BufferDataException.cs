﻿using System;

namespace Arch.CMessaging.Client.Net.Core.Buffer
{

    [Serializable]
    public class BufferDataException : Exception
    {
        public BufferDataException() { }
        public BufferDataException(String message)
            : base(message) { }
        public BufferDataException(String message, Exception inner)
            : base(message, inner) { }
        protected BufferDataException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
