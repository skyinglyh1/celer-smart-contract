using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;
using System.ComponentModel;

namespace PayRegistry
{
    public class PayRegistry : SmartContract
    {
        public class PayInfo
        {
            public BigInteger amount;
            public BigInteger resolveDeadline;
        }
        //public Map<byte[], PayInfo> payInfoMap;
        public static readonly byte[] PayInfoPrefix = "payInfo".AsByteArray();
        public static readonly byte[] AddressZero = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

        [DisplayName("setPayAmount")]
        public static event Action<byte[], BigInteger, BigInteger> PayInfoUpdate;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "calculatePayId")
                {
                    assert(args.Length == 2, "PayRegistry parameter error");
                    byte[] payHash = (byte[])args[0];
                    byte[] setter = (byte[])args[1];
                    return calculatePayId(payHash, setter);
                }
                if (operation == "setPayAmount")
                {
                    assert(args.Length == 3, "PayRegistry parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] payHash = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    return setPayAmount(invoker, payHash, amount);
                }
                if (operation == "setPayDeadline")
                {
                    assert(args.Length == 3, "PayRegistry parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] payHash = (byte[])args[1];
                    BigInteger deadline = (BigInteger)args[2];
                    return setPayDeadline(invoker, payHash, deadline);
                }
                if (operation == "setPayInfo")
                {
                    assert(args.Length == 4, "PayRegistry parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] payHash = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    BigInteger deadline = (BigInteger)args[3];
                    return setPayInfo(invoker, payHash, amount, deadline);
                }
                if (operation == "setPayAmounts")
                {
                    assert(args.Length == 4, "PayRegistry parameter error");
                    byte[][] invokers = (byte[][])args[0];
                    byte[][] payHashs = (byte[][])args[1];
                    BigInteger[] amounts = (BigInteger[])args[2];
                    return setPayAmounts(invokers, payHashs, amounts);
                }
                if (operation == "setPayDeadlines")
                {
                    assert(args.Length == 3, "PayRegistry parameter error");
                    byte[][] invokers = (byte[][])args[0];
                    byte[][] payHashs = (byte[][])args[1];
                    BigInteger[] deadlines = (BigInteger[])args[2];
                    return setPayDeadlines(invokers, payHashs, deadlines);
                }
                if (operation == "setPayInfos")
                {
                    assert(args.Length == 4, "PayRegistry parameter error");
                    byte[][] invokers = (byte[][])args[0];
                    byte[][] payHashs = (byte[][])args[1];
                    BigInteger[] amounts = (BigInteger[])args[2];
                    BigInteger[] deadlines = (BigInteger[])args[3];
                    return setPayInfos(invokers, payHashs, amounts, deadlines);
                }
                if (operation == "getPayAmounts")
                {
                    assert(args.Length == 2, "PayRegistry parameter error");
                    byte[][] payHashs = (byte[][])args[0];
                    BigInteger lastPayResolveDeadline = (BigInteger)args[1];
                    return getPayAmounts(payHashs, lastPayResolveDeadline);
                }
                if (operation == "getPayInfo")
                {
                    assert(args.Length == 1, "PayRegistry parameter error");
                    byte[] payId = (byte[])args[0];
                    return getPayInfo(payId);
                }
            }
            return false;
        }

        [DisplayName("calculatePayId")]
        public static byte[] calculatePayId(byte[] payHash, byte[] setter)
        {
            assert(_isByte32(payHash), "invalid hash");
            assert(_isLegalAddress(setter), "invalid address");
            return SmartContract.Hash256(payHash.Concat(setter));
        }

        [DisplayName("setPayAmount")]
        public static object setPayAmount(byte[] invoker, byte[] payHash, BigInteger amount)
        {
            assert(_isByte32(payHash), "invalid hash");
            assert(_isLegalAddress(invoker), "invalid address");
            assert(amount >= 0, "amount is less than zero");

            assert(Runtime.CheckWitness(invoker), "Checkwitness failed");
            byte[] payId = calculatePayId(payHash, invoker);
            byte[] payInfoBs = Storage.Get(Storage.CurrentContext, PayInfoPrefix.Concat(payId));
            PayInfo payInfo = new PayInfo();
            if (payInfoBs.Length > 0)
            {
                payInfo = Helper.Deserialize(payInfoBs) as PayInfo;
            }
            payInfo.amount = amount;
            Storage.Put(Storage.CurrentContext, PayInfoPrefix.Concat(payId), Helper.Serialize(payInfo));

            PayInfoUpdate(payId, amount, payInfo.resolveDeadline);

            return true;
        }

        [DisplayName("setPayDeadline")]
        public static object setPayDeadline(byte[] invoker, byte[] payHash, BigInteger deadline)
        {
            assert(_isByte32(payHash), "invalid hash");
            assert(_isLegalAddress(invoker), "invalid address");
            assert(deadline >= 0, "deadline is less than zero");

            assert(Runtime.CheckWitness(invoker), "Checkwitness failed");
            byte[] payId = calculatePayId(payHash, invoker);
            byte[] payInfoBs = Storage.Get(Storage.CurrentContext, PayInfoPrefix.Concat(payId));
            PayInfo payInfo = new PayInfo();
            if (payInfoBs.Length > 0)
            {
                payInfo = Helper.Deserialize(payInfoBs) as PayInfo;
            }
            payInfo.resolveDeadline = deadline;
            Storage.Put(Storage.CurrentContext, PayInfoPrefix.Concat(payId), Helper.Serialize(payInfo));

            PayInfoUpdate(payId, payInfo.amount, deadline);

            return true;
        }

        [DisplayName("setPayInfo")]
        public static object setPayInfo(byte[] invoker, byte[] payHash, BigInteger amount, BigInteger deadline)
        {
            assert(_isByte32(payHash), "invalid hash");
            assert(_isLegalAddress(invoker), "invalid address");
            assert(amount >= 0, "amount is less than zero");
            assert(deadline >= 0, "deadline is less than zero");

            assert(Runtime.CheckWitness(invoker), "Checkwitness failed");
            byte[] payId = calculatePayId(payHash, invoker);
            byte[] payInfoBs = Storage.Get(Storage.CurrentContext, PayInfoPrefix.Concat(payId));
            PayInfo payInfo = new PayInfo();
            if (payInfoBs.Length > 0)
            {
                payInfo = Helper.Deserialize(payInfoBs) as PayInfo;
            }
            payInfo.resolveDeadline = deadline;
            payInfo.amount = amount;
            Storage.Put(Storage.CurrentContext, PayInfoPrefix.Concat(payId), Helper.Serialize(payInfo));

            PayInfoUpdate(payId, payInfo.amount, deadline);

            return true;
        }

        [DisplayName("setPayAmounts")]
        public static object setPayAmounts(byte[][] invokers, byte[][] payHashs, BigInteger[] amounts)
        {
            BigInteger arrayLen = invokers.Length;
            assert(arrayLen == payHashs.Length, "length does not match");
            assert(arrayLen == amounts.Length, "length does not match");

            for (var i = 0; i < arrayLen; i++)
            {
                assert((bool)setPayAmount(invokers[i], payHashs[i], amounts[i]), "error");
            }
            return true;
        }

        [DisplayName("setPayDeadlines")]
        public static object setPayDeadlines(byte[][] invokers, byte[][] payHashs, BigInteger[] deadlines)
        {
            BigInteger arrayLen = invokers.Length;
            assert(arrayLen == payHashs.Length, "length does not match");
            assert(arrayLen == deadlines.Length, "length does not match");

            for (var i = 0; i < arrayLen; i++)
            {
                assert((bool)setPayDeadline(invokers[i], payHashs[i], deadlines[i]), "error");
            }
            return true;
        }

        [DisplayName("setPayInfos")]
        public static object setPayInfos(byte[][] invokers, byte[][] payHashs, BigInteger[] amounts, BigInteger[] deadlines)
        {
            BigInteger arrayLen = invokers.Length;
            assert(arrayLen == payHashs.Length, "length does not match");
            assert(arrayLen == amounts.Length, "length does not match");
            assert(arrayLen == deadlines.Length, "length does not match");

            for (var i = 0; i < arrayLen; i++)
            {
                assert((bool)setPayInfo(invokers[i], payHashs[i], amounts[i], deadlines[i]), "error");
            }

            return true;
        }


        [DisplayName("getPayAmounts")]
        public static BigInteger[] getPayAmounts(byte[][] payIds, BigInteger lastPayResolveDeadline)
        {
            assert(_isByte32s(payIds), "payIds invalid");
            assert(lastPayResolveDeadline >= 0, "lastPayResolveDeadline less than zero");

            BigInteger[] amounts = new BigInteger[payIds.Length];
            BigInteger now = Blockchain.GetHeight();
            for (var i = 0; i < payIds.Length; i++)
            {
                byte[] payInfoBs = Storage.Get(Storage.CurrentContext, PayInfoPrefix.Concat(payIds[i]));
                PayInfo payInfo = new PayInfo();
                if (payInfoBs.Length > 0)
                {
                    payInfo = Helper.Deserialize(payInfoBs) as PayInfo;
                }
                if (payInfo.resolveDeadline == 0)
                {
                    assert(now > lastPayResolveDeadline, "payment is not finalized");
                }
                else
                {
                    assert(now > payInfo.resolveDeadline, "payment is not finalized");
                }
                amounts[i] = payInfo.amount;
            }
            return amounts;
        }

        [DisplayName("getPayInfo")]
        public static BigInteger[] getPayInfo(byte[] payId)
        {
            assert(_isByte32(payId), "payId invalid");
            byte[] payInfoBs = Storage.Get(Storage.CurrentContext, PayInfoPrefix.Concat(payId));
            PayInfo payInfo = new PayInfo();
            if (payInfoBs.Length > 0)
            {
                payInfo = Helper.Deserialize(payInfoBs) as PayInfo;
            }
            return new BigInteger[2] { payInfo.amount, payInfo.resolveDeadline };
        }


        private static void assert(bool condition, string msg)
        {
            if (!condition)
            {
                throw new Exception((msg.HexToBytes().Concat(" error ".HexToBytes())).AsString());
            }
        }
        private static bool _isLegalAddress(byte[] addr)
        {
            return addr.Length == 20 && addr != AddressZero;
        }
        private static bool _isLegalAddresses(byte[][] addrs)
        {
            for (var i = 0; i < addrs.Length; i++)
            {
                if (_isLegalAddress(addrs[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }
        private static bool _isByte32(byte[] byte32)
        {
            return byte32.Length == 32;
        }
        private static bool _isByte32s(byte[][] byte32s)
        {
            for (var i = 0; i < byte32s.Length; i++)
            {
                if (_isByte32(byte32s[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
