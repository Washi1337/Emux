using System;
using System.IO;

namespace Emux.GameBoy.Cartridge
{
    public class StreamedExternalMemory : IExternalMemory
    {
        public StreamedExternalMemory(Stream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanRead || !baseStream.CanWrite || !baseStream.CanSeek)
                throw new ArgumentException("Stream must be readable, writeable and seekable.");
            BaseStream = baseStream;
        }

        public Stream BaseStream
        {
            get;
        }

        public bool IsActive
        {
            get;
            private set;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            BaseStream.Flush();
            IsActive = false;
        }

        public void SetBufferSize(int length)
        {
            BaseStream.SetLength(length);
        }

        public byte ReadByte(int address)
        {
            if (IsActive)
            {
                BaseStream.Position = address;
                return (byte) BaseStream.ReadByte();
            }
            return 0;
        }

        public void ReadBytes(int address, byte[] buffer, int offset, int length)
        {
            BaseStream.Position = address;
            BaseStream.Read(buffer, offset, length);
        }

        public void WriteByte(int address, byte value)
        {
            if (IsActive)
            {
                BaseStream.Position = address;
                BaseStream.WriteByte(value);
            }
        }

        public void Dispose()
        {
			BaseStream.Flush();
			BaseStream.Close();
            BaseStream.Dispose();
        }
    }
}