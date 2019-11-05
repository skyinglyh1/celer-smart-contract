using Neo.SmartContract.Framework;
using System;

public class BasicMethods
{
    private static readonly byte[] AddressZero = Neo.SmartContract.Framework.Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

    public static void assert(bool condition, string msg)
    {
        if (!condition)
        {
            throw new Exception(msg + " error ");
        }
    }

    public static bool _isLegalAddress(byte[] addr)
    {
        return addr.Length == 20 && addr != AddressZero;
    }

    public static bool _isByte32(byte[] byte32)
    {
        return byte32.Length == 32;
    }
}