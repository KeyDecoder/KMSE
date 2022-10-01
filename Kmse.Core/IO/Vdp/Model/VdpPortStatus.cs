﻿namespace Kmse.Core.IO.Vdp.Model;

public class VdpPortStatus
{
    public int HCounter { get; init; }
    public byte VCounter { get; init; }
    public ushort CommandWord { get; init; }
    public VdpStatusFlags StatusFlags { get; init; }
    public byte CodeRegister { get; init; }
    public ushort AddressRegister { get; init; }
    public byte[] VdpRegisters { get; init; }
    public byte ReadBuffer { get; init; }
    public DataPortWriteMode WriteMode { get; init; }
}