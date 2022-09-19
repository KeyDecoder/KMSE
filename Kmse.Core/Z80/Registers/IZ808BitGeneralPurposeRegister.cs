namespace Kmse.Core.Z80.Registers;

/// <summary>
///     Interface for a Z80 8 bit general purpose register which can have operations on the value
///     This distinguishes is from the Flag register which cannot do various math operations on it
/// </summary>
public interface IZ808BitGeneralPurposeRegister : IZ808BitRegister
{
    public void Increment();
    public void Decrement();
    public void ClearBit(int bit);
    public void SetBit(int bit);
    public void RotateLeftCircular();
    public void RotateLeft();
    public void RotateRightCircular();
    public void RotateRight();
    public void ShiftLeftArithmetic();
    public void ShiftRightArithmetic();
    public void ShiftLeftLogical();
    public void ShiftRightLogical();
}