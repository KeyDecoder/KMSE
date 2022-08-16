namespace Kmse.Core.IO.Controllers;

public interface IControllerPort
{
    byte ReadPort(ushort port);
}