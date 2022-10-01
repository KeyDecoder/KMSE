namespace Kmse.Core.IO.Controllers;

public interface IControllerPort
{
    void Reset();
    void SetIoStatus(bool enabled);
    void SetIoPortControl(byte value);
    byte ReadPort(ushort port);

    void ChangeResetButtonState(bool pressed);
    void ChangeInputAControlState(ControllerInputStatus status, bool pressed);
    void ChangeInputBControlState(ControllerInputStatus status, bool pressed);
}