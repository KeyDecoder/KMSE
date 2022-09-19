namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public interface IZ80StackManager : IZ8016BitSpecialRegister
{
    void IncrementStackPointer();
    void DecrementStackPointer();
    void PushRegisterToStack(IZ8016BitRegister register);

    void PopRegisterFromStack(IZ8016BitRegister register);
    void SwapRegisterWithDataAtStackPointerAddress(IZ8016BitRegister register);
}