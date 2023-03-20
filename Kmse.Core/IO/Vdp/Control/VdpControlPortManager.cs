using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;

namespace Kmse.Core.IO.Vdp.Control;

public class VdpControlPortManager : IVdpControlPortManager
{
    private readonly IVdpRam _ram;
    private readonly IVdpRegisters _registers;
    private bool _commandWordSecondByte;

    public VdpControlPortManager(IVdpRam ram, IVdpRegisters registers)
    {
        _ram = ram;
        _registers = registers;
    }

    public ushort CommandWord { get; private set; }
    public byte CodeRegister { get; private set; }

    public void Reset()
    {
        CommandWord = 0x00;
        _commandWordSecondByte = false;
        CodeRegister = 0x00;
    }

    public void ResetControlByte()
    {
        _commandWordSecondByte = false;
    }

    public void WriteToVdpControlPort(byte value)
    {
        // Command word structure
        // This is two bytes but written into two I/O writes
        // So we keep track of if we have written the first or second byte yet
        //MSB LSB
        //CD1 CD0 A13 A12 A11 A10 A09 A08    Second byte written
        //A07 A06 A05 A04 A03 A02 A01 A00    First byte written

        // Update address in command word
        // Normally the address is part of the command word but since this is split out into the RAM class
        // When we write the port, we keep it in sync just in case
        CommandWord = (ushort)((CommandWord & 0xC000) + (_ram.AddressRegister));

        if (!_commandWordSecondByte)
        {
            // Keep upper 8 bits and set lower
            // We maintain the upper 8 bits while writing rather than clearing (not sure why?)
            CommandWord &= 0xFF00;
            CommandWord |= value;

            // When the first byte is written, the lower 8 bits of the address register are updated
            // Clear lower 8 bits and set from first byte of command word but preserve the upper 6 bits until the next write
            _ram.UpdateAddressRegisterLowerByte(value);
        }
        else
        {
            // Clear top 8 bits and set lower to add to first write
            CommandWord &= 0x00FF;
            CommandWord |= (ushort)(value << 8);

            // When the second byte is written, the upper 6 bits of the address
            // register and the code register are updated

            // Add the bottom 6 bits of the second command word byte to the address register
            var upperAddressValue = (byte)(value & 0x3F);
            _ram.UpdateAddressRegisterUpperByte(upperAddressValue);

            // Set code register to value in top 2 bits of second command word byte
            CodeRegister = (byte)((CommandWord >> 14) & 0x03);

            ProcessCodeRegisterChange();
        }

        _commandWordSecondByte = !_commandWordSecondByte;
    }

    private void ProcessCodeRegisterChange()
    {
        switch (CodeRegister)
        {
            case 0:
            {
                _ram.ReadFromVideoRamIntoBuffer();
                _ram.SetWriteModeToVideoRam();
            }
                break;
            case 1:
            {
                // Writes to the data port go to VRAM.
                _ram.SetWriteModeToVideoRam();
            }
                break;
            case 2:
            {
                // VDP register write
                // Writes to the data port go to VRAM.
                _ram.SetWriteModeToVideoRam();

                // MSB LSB
                // 1   0 ?   ? R03 R02 R01 R00 Second byte written
                // D07 D06 D05 D04 D03 D02 D01 D00    First byte written
                // Rxx: VDP register number
                // Dxx : VDP register data
                var registerNumber = (byte)((CommandWord & 0x0F00) >> 8);
                var registerData = (byte)(CommandWord & 0x00FF);
                ProcessVdpRegisterWrite(registerNumber, registerData);
            }
                break;
            case 3:
            {
                // Writes to the data port go to CRAM.
                _ram.SetWriteModeToColourRam();
            }
                break;
            default: throw new InvalidOperationException($"VDP code register value '{CodeRegister}' is not valid");
        }
    }

    private void ProcessVdpRegisterWrite(byte registerNumber, byte registerData)
    {
        if (registerNumber > 11)
        {
            // There are only 11 registers, values 11 through 15 have no effect when written to.
            return;
        }

        _registers.SetRegister(registerNumber, registerData);
    }
}