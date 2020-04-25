using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Emux.GameBoy.Cartridge 
{
	public class BufferedExternalMemory : IExternalMemory 
	{
		private byte[] _externalMemory;
		private FileStream _fileStream;

		public BufferedExternalMemory(string filePath) : this(File.Open(filePath, FileMode.OpenOrCreate))
		{ }

		public BufferedExternalMemory(FileStream fileStream)
		{
			_fileStream = fileStream;

			SetBufferSize((int)_fileStream.Length);
			_externalMemory = new byte[_fileStream.Length];

			_fileStream.Read(_externalMemory, 0, _externalMemory.Length);
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
			IsActive = false;
		}

		public void SetBufferSize(int length)
		{
			if (_externalMemory != null)
			{
				var backup = _externalMemory;
				_externalMemory = new byte[length];
				Array.Copy(backup, _externalMemory, length);
			} 
			else
			{
				_externalMemory = new byte[length];
			}
		}

		public byte ReadByte(int address)
		{
			return IsActive ? _externalMemory[address] : (byte)0;
		}

		public void ReadBytes(int address, byte[] buffer, int offset, int length)
		{
			Array.Copy(_externalMemory, address, buffer, 0, length);
		}

		public void WriteByte(int address, byte value)
		{
			if (IsActive)
				_externalMemory[address] = value;
		}

		public void Dispose()
		{
			if (_externalMemory == null || _fileStream == null) // Already disposed
				return;

			var em = _externalMemory;
			var fs = _fileStream;

			_externalMemory = null;
			_fileStream = null;

			fs.Position = 0;
			fs.Write(em, 0, em.Length);
			fs.Flush();
			fs.Close();
			fs.Dispose();
		}
	}
}
