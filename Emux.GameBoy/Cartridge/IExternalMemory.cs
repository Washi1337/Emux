using System;

namespace Emux.GameBoy.Cartridge
{
    public interface IExternalMemory : IDisposable {
        bool IsActive
        {
            get;
        }

        void Activate();
        void Deactivate();
        void SetBufferSize(int length);
        byte ReadByte(int address);
        void ReadBytes(int address, byte[] buffer, int offset, int length);
        void WriteByte(int address, byte value);
    }
}
