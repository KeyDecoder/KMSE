namespace Kmse.Core.IO.Controllers;

[Flags]
public enum ControllerInputStatus : byte
{
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    LeftButton = 1 << 4,
    RightButton = 1 << 5,
}