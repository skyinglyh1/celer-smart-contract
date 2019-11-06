using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace LedgerBalanceLimit
{
    public class LedgerBalanceLimit : SmartContract
    {
        private static readonly byte[] BalanceLimitsPrefix = "balanceLimit".AsByteArray();

        private static readonly byte[] BalanceLimitsEnabledPrefix = "balanceLimitsEnabled".AsByteArray();

        static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "setBalanceLimits")
                {
                    BasicMethods.assert(args.Length == 2, "setBalanceLimits parameter error");
                    byte[][] tokenAddrs = (byte[][])args[0];
                    BigInteger[] limits = (BigInteger[])args[1];
                    BasicMethods.assert(tokenAddrs.Length == limits.Length, "Lengths do not match");
                    return setBalanceLimits(tokenAddrs, limits);
                }
                else if (operation == "disableBalanceLimits")
                {
                    return disableBalanceLimits();
                }
                else if (operation == "enableBalanceLimits")
                {
                    return enableBalanceLimits();
                }
                else if (operation == "getBalanceLimit")
                {
                    BasicMethods.assert(args.Length == 1, "getBalanceLimit parameter error");
                    byte[] tokenAddrs = (byte[])args[0];
                    BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddrs), "tokenAddrs is illegal");
                    return getBalanceLimit(tokenAddrs);
                }
                else if (operation == "getBalanceLimitsEnabled")
                {
                    return getBalanceLimitsEnabled();
                }
            }
            return false;
        }

        [DisplayName("setBalanceLimits")]
        public static object setBalanceLimits(byte[][] tokenAddrs, BigInteger[] limits)
        {
            for (int i = 0; i < tokenAddrs.Length; i++)
            {
                BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddrs[i]), "Token address " + i + " is illegal");
                Storage.Put(Storage.CurrentContext, BalanceLimitsPrefix.Concat(tokenAddrs[i]), limits[i]);
            }
            return true;
        }

        [DisplayName("disableBalanceLimits")]
        public static object disableBalanceLimits()
        {
            Storage.Put(Storage.CurrentContext, BalanceLimitsEnabledPrefix, 1);
            return true;
        }

        [DisplayName("enableBalanceLimits")]
        public static object enableBalanceLimits()
        {
            Storage.Put(Storage.CurrentContext, BalanceLimitsEnabledPrefix, 0);
            return true;
        }

        [DisplayName("getBalanceLimit")]
        public static BigInteger getBalanceLimit(byte[] tokenAddr)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddr), "Token address is illegal");
            return Storage.Get(Storage.CurrentContext, BalanceLimitsPrefix.Concat(tokenAddr)).ToBigInteger();
        }

        [DisplayName("getBalanceLimitsEnabled")]
        public static bool getBalanceLimitsEnabled()
        {
            byte[] result = Storage.Get(Storage.CurrentContext, BalanceLimitsEnabledPrefix);
            if (result == null) return false;
            else return result.ToBigInteger() == 1;
        }
    }
}
