using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public interface IZ80StackManager : IZ80Register
{
    void IncrementStackPointer();
    void DecrementStackPointer();
    void PushRegisterToStack(Z80Register register);
    void PopRegisterFromStack(ref Z80Register register);
    void SwapRegisterWithStackPointerLocation(ref Z80Register register);
    void SetStackPointer(ushort value);
    void SetStackPointerFromDataInMemory(ushort memoryLocation);
}