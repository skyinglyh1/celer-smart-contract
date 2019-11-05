using System;

public class BasicMethods
{
    private static readonly byte[] addressZero = Neo.SmartContract.Framework.Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

    private static readonly int legalLength = 20;

    public static void assert(bool condition, string msg)
    {
        if (!condition)
        {
            throw new Exception(msg + " error ");
        }
    }

    public static bool _isLegalAddress(byte[] addr)
    {
        return addr.Length == legalLength && addr != addressZero;
    }

    public static bool _isLegalLength(int len)
    {
        return len == legalLength;
    }

    public static bool _isByte32(byte[] byte32)
    {
        return byte32.Length == 32;
    }
}