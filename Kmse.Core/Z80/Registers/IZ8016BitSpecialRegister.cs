namespace Kmse.Core.Z80.Registers;

public interface IZ8016BitSpecialRegister : IZ8016BitRegister
{
    void Increment();
    void Decrement();
    void ResetBitByRegisterLocation(int bit, int offset);
    void SetBitByRegisterLocation(int bit, int offset);
    void TestBitByRegisterLocation(int bit, int offset);
    void Add(ushort source, bool withCarry = false);
    void Add(IZ8016BitRegister register, bool withCarry = false);
    void Subtract(ushort source);
    void Subtract(IZ8016BitRegister register);
    void RotateLeftCircular(int offset);
    void RotateLeft(int offset);
    void RotateRightCircular(int offset);
    void RotateRight(int offset);
    void ShiftLeftArithmetic(int offset);
    void ShiftRightArithmetic(int offset);
    void ShiftLeftLogical(int offset);
    void ShiftRightLogical(int offset);
}