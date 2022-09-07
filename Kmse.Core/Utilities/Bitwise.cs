namespace Kmse.Core.Utilities;

public static class Bitwise
{
    public static bool IsSet(byte value, int bit)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 7 inclusive");
        }
        return (value & (1 << bit)) != 0;
    }

    public static bool IsSet(int value, int bit)
    {
        if (bit is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 15 inclusive");
        }
        return (value & (1 << bit)) != 0;
    }

    public static void Set(ref byte value, int bit)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 7 inclusive");
        }
        value = (byte)(value | (1 << bit));
    }

    public static void SetIf(ref byte value, int bit, Func<bool> checkFunc)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 7 inclusive");
        }

        if (!checkFunc())
        {
            return;
        }

        Set(ref value, bit);
    }

    public static void Clear(ref byte value, int bit)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 7 inclusive");
        }

        value = (byte)(value & ~(1 << bit));
    }

    public static void ClearIf(ref byte value, int bit, Func<bool> checkFunc)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 7 inclusive");
        }

        if (!checkFunc())
        {
            return;
        }

        Clear(ref value, bit);
    }

    /// <summary>
    /// Sets bit if check function returns true or clears bit if returns false
    /// </summary>
    /// <param name="value">Value to set/clear bit on</param>
    /// <param name="bit">Bit number from 0-7</param>
    /// <param name="checkFunc">Func to check which returns a bool</param>
    public static void SetOrClearIf(ref byte value, int bit, Func<bool> checkFunc)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit), "Bit must be between 0 and 7 inclusive");
        }

        if (checkFunc())
        {
            Set(ref value, bit);
        }
        else
        {
            Clear(ref value, bit);
        }
    }
}