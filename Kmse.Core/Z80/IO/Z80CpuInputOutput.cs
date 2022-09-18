using Kmse.Core.IO;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.IO;

public class Z80CpuInputOutput : IZ80CpuInputOutput
{
    private readonly IZ80FlagsManager _flags;
    private readonly IMasterSystemIoManager _io;

    public Z80CpuInputOutput(IMasterSystemIoManager io, IZ80FlagsManager flags)
    {
        _io = io;
        _flags = flags;
    }

    public byte Read(byte high, byte low, bool setFlags)
    {
        var address = Bitwise.ToUnsigned16BitValue(high, low);
        var data = _io.ReadPort(address);

        if (!setFlags)
        {
            return data;
        }

        // If high bit set, then negative so set sign flag
        _flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(data, 7));
        _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, data == 0);

        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetParityFromValue(data);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);

        return data;
    }

    public void ReadAndSetRegister(byte high, byte low, IZ808BitRegister register)
    {
        var data = Read(high, low, true);
        register.Set(data);
    }

    public void ReadAndSetRegister(IZ8016BitRegister addressRegister, IZ808BitRegister register)
    {
        ReadAndSetRegister(addressRegister.High, addressRegister.Low, register);
    }

    public void Write(byte high, byte low, IZ808BitRegister register)
    {
        var address = Bitwise.ToUnsigned16BitValue(high, low);
        _io.WritePort(address, register.Value);
    }
}