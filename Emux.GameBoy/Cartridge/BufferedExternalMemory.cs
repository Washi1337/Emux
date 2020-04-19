using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Emux.GameBoy.Cartridge 
{
	public class BufferedExternalMemory : IExternalMemory 
	{
		private Stream memoryStream, fileStream;

		public BufferedExternalMemory(string filePath) : this(File.Open(filePath, FileMode.OpenOrCreate))
		{ }

		public BufferedExternalMemory(Stream fileStream)
		{
			this.fileStream = fileStream;

			memoryStream = new MemoryStream();

			this.fileStream.CopyTo(memoryStream);
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
			memoryStream?.Flush();
			IsActive = false;
		}

		public void SetBufferSize(int length)
		{
			memoryStream?.SetLength(length);
		}

		public byte ReadByte(int address)
		{
			if (memoryStream == null)
				return 0;

			if (IsActive)
			{
				memoryStream.Position = address;
				return (byte)memoryStream.ReadByte();
			}
			return 0;
		}

		public void ReadBytes(int address, byte[] buffer, int offset, int length)
		{
			memoryStream.Position = address;
			memoryStream.Read(buffer, offset, length);
		}

		public void WriteByte(int address, byte value)
		{
			if (memoryStream == null)
				return;

			if (IsActive)
			{
				memoryStream.Position = address;
				memoryStream.WriteByte(value);
			}
		}

		public void Dispose()
		{
			if (memoryStream == null || fileStream == null) // Already disposed
				return;

			var ms = memoryStream;
			var fs = fileStream;

			memoryStream = null;
			fileStream = null;

			ms.Position = 0;
			fs.Position = 0;
			ms.Flush();
			ms.CopyTo(fs);
			ms.Close();
			ms.Dispose();
			fs.Flush();
			fs.Close();
			fs.Dispose();
		}
	}
}
