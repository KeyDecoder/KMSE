using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;

namespace Kmse.Core.IO;

public interface IMasterSystemIoManager
{
    void Initialize(IVdpPort vdpPort, IControllerPort controllerPort, ISoundPort soundPort,
        IDebugConsolePort debugConsolePort);
    void Reset();

    byte ReadPort(ushort port);
    void WritePort(ushort port, byte value);
}