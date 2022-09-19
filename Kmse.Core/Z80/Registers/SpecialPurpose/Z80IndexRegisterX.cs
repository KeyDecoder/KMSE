﻿using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80IndexRegisterX : Z8016BitSpecialRegisterBase, IZ80IndexRegisterX
{
    public Z80IndexRegisterX(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}