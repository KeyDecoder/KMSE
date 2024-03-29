﻿namespace Kmse.Core.IO.Vdp;

public interface IVdpPort
{
    void Reset();
    byte ReadPort(byte port);
    void WritePort(byte port, byte value);
    void Execute(int cycles);

    VdpPortStatus GetStatus();
    byte[] DumpVideoRam();
    byte[] DumpColourRam();
}