using System.Runtime.CompilerServices;
using Kmse.Core.Z80.Model;

namespace Kmse.Core.Z80.Instructions
{
    public interface IZ80CpuInstructions
    {
        Instruction GetInstruction(byte opCode);
    }
}
