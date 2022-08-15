using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kmse.Core.Memory
{
    public interface IMemoryLogger
    {
        void Information(string message);
        void Error(string message);
        void MemoryRead(ushort address, byte data);
        void CartridgeRead(ushort address, byte data);
        void RamBankMemoryRead(int bank, ushort address, byte data);
        void MemoryWrite(ushort address, byte oldData, byte newData);
        void RamBankMemoryWrite(int bank, ushort address, byte oldData, byte newData);
    }
}
