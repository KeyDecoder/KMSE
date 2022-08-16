namespace Kmse.Core.IO.Controllers;

public interface IControllerPort
{
    void Reset();
    byte ReadPort(ushort port);
}