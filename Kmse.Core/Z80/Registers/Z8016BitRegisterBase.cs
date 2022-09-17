using Kmse.Core.Memory;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

/// <summary>
///     Base class for a true 16 bit register
/// </summary>
public abstract class Z8016BitRegisterBase : IZ8016BitRegister
{
    private readonly IMasterSystemMemory _memory;
    protected Z80Register Register;

    protected Z8016BitRegisterBase(IMasterSystemMemory memory)
    {
        _memory = memory;
        Register = new Z80Register();
    }

    public ushort Value => Register.Word;
    public byte High => Register.High;
    public byte Low => Register.Low;

    public virtual void Reset()
    {
        Register.Word = 0x00;
    }

    /// <summary>
    ///     Set program counter to new value, but don't save the old value to the stack
    /// </summary>
    /// <param name="value">New address to set PC to</param>
    public void Set(ushort value)
    {
        Register.Word = value;
    }

    public void SetHigh(byte value)
    {
        Register.High = value;
    }

    public void SetLow(byte value)
    {
        Register.Low = value;
    }

    public void SetFromDataInMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        SetLow(_memory[location]);
        location++;
        SetHigh(_memory[location]);
    }

    public Z80Register AsRegister()
    {
        return Register;
    }
}