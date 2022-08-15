namespace Kmse.Core.Utilities;

public static class Bitwise
{
    public static bool IsSet(byte value, int bit)
    {
        return (value & (1 << bit)) == 1;
    }

    public static bool IsSet(short value, int bit)
    {
        return (value & (1 << bit)) == 1;
    }

    public static bool IsSet(ushort value, int bit)
    {
        return (value & (1 << bit)) == 1;
    }

    public static bool IsSet(int value, int bit)
    {
        return (value & (1 << bit)) == 1;
    }

    public static bool IsSet(uint value, int bit)
    {
        return (value & (1 << bit)) == 1;
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