namespace Kmse.Core.IO.Controllers;

public class ControllerPort : IControllerPort
{
    public void Reset()
    {
    }

    public byte ReadPort(ushort port)
    {
        return 0x00;
    }
}