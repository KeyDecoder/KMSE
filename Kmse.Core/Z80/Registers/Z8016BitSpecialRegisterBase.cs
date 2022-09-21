using Kmse.Core.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers;

/// <summary>
///     Base class for a true 16 bit register
/// </summary>
public abstract class Z8016BitSpecialRegisterBase : Z8016BitRegisterBase, IZ8016BitRegister
{
    protected Z80Register Register;

    protected Z8016BitSpecialRegisterBase(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory, flags)
    {
        Register = new Z80Register();
    }

    public override ushort Value => Register.Word;
    public override byte High => Register.High;
    public override byte Low => Register.Low;

    public virtual void Reset()
    {
        Register.Word = 0x00;
    }

    /// <summary>
    ///     Set register to new value
    /// </summary>
    /// <param name="value">New value to this register to</param>
    public override void Set(ushort value)
    {
        Register.Word = value;
    }

    public void Set(IZ8016BitRegister register)
    {
        Register.Word = register.Value;
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
        SetLow(Memory[location]);
        location++;
        SetHigh(Memory[location]);
    }

    public void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0)
    {
        SetFromDataInMemory(register.Value, offset);
    }

    public void SaveToMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        Memory[location] = Register.Low;
        Memory[(ushort)(location + 1)] = Register.High;
    }

    public Z80Register AsRegister()
    {
        return Register;
    }

    public void Increment()
    {
        Register.Word++;
    }

    public void Decrement()
    {
        Register.Word--;
    }
}