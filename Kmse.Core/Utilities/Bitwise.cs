namespace Kmse.Core.Utilities;

public static class Bitwise
{
    public static bool IsSet(byte value, int bit)
    {
        return (value & (1 << bit)) != 0;
    }

    public static bool IsSet(int value, int bit)
    {
        return (value & (1 << bit)) != 0;
    }

    public static void Set(ref byte value, int bit)
    {
        value = (byte)(value | (1 << bit));
    }

    public static void Clear(ref byte value, int bit)
    {
        value = (byte)(value & ~(1 << bit));
    }
}