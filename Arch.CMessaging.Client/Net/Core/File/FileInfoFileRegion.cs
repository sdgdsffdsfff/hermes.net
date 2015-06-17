﻿using System;
using System.IO;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Net.Core.File
{
    public class FileInfoFileRegion : IFileRegion
    {
        private readonly FileInfo _file;
        private readonly Int64 _originalPosition;
        private Int64 _position;
        private Int64 _remainingBytes;

        public FileInfoFileRegion(FileInfo fileInfo)
            : this(fileInfo, 0, fileInfo.Length)
        { }

        public FileInfoFileRegion(FileInfo fileInfo, Int64 position, Int64 remainingBytes)
        {
            if (fileInfo == null)
                throw new ArgumentNullException("fileInfo");
            if (position < 0L)
                throw new ArgumentException("position may not be less than 0", "position");
            if (remainingBytes < 0L)
                throw new ArgumentException("remainingBytes may not be less than 0", "remainingBytes");

            _file = fileInfo;
            _originalPosition = position;
            _position = position;
            _remainingBytes = remainingBytes;
        }

        public String FullName
        {
            get { return _file.FullName; }
        }

        public Int64 Length
        {
            get { return _file.Length; }
        }

        public Int64 Position
        {
            get { return _position; }
        }

        public Int64 RemainingBytes
        {
            get { return _remainingBytes; }
        }

        public Int64 WrittenBytes
        {
            get { return _position - _originalPosition; }
        }
        public Int32 Read(IoBuffer buffer)
        {
            using (FileStream fs = _file.OpenRead())
            {
                fs.Position = _position;
                Byte[] bytes = new Byte[buffer.Remaining];
                Int32 read = fs.Read(bytes, 0, bytes.Length);
                buffer.Put(bytes, 0, read);
                Update(read);
                return read;
            }
        }
        public void Update(Int64 amount)
        {
            _position += amount;
            _remainingBytes -= amount;
        }
    }
}
