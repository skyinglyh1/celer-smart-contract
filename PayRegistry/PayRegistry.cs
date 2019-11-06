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
                    BasicMethods.assert(args.Length == 2, "PayRegistry parameter error");
                    byte[] payHash = (byte[])args[0];
                    byte[] setter = (byte[])args[1];
                    return calculatePayId(payHash, setter);
                }
                if (operation == "setPayAmount")
                {
                    BasicMethods.assert(args.Length == 3, "PayRegistry parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] payHash = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    return setPayAmount(invoker, payHash, amount);
                }
                if (operation == "setPayDeadline")
                {
                    BasicMethods.assert(args.Length == 3, "PayRegistry parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] payHash = (byte[])args[1];
                    BigInteger deadline = (BigInteger)args[2];
                    return setPayDeadline(invoker, payHash, deadline);
                }
                if (operation == "setPayInfo")
                {
                    BasicMethods.assert(args.Length == 4, "PayRegistry parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] payHash = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    BigInteger deadline = (BigInteger)args[3];
                    return setPayInfo(invoker, payHash, amount, deadline);
                }
                if (operation == "setPayAmounts")
                {
                    BasicMethods.assert(args.Length == 4, "PayRegistry parameter error");
                    byte[][] invokers = (byte[][])args[0];
                    byte[][] payHashs = (byte[][])args[1];
                    BigInteger[] amounts = (BigInteger[])args[2];
                    return setPayAmounts(invokers, payHashs, amounts);
                }
                if (operation == "setPayDeadlines")
                {
                    BasicMethods.assert(args.Length == 3, "PayRegistry parameter error");
                    byte[][] invokers = (byte[][])args[0];
                    byte[][] payHashs = (byte[][])args[1];
                    BigInteger[] deadlines = (BigInteger[])args[2];
                    return setPayDeadlines(invokers, payHashs, deadlines);
                }
                if (operation == "setPayInfos")
                {
                    BasicMethods.assert(args.Length == 4, "PayRegistry parameter error");
                    byte[][] invokers = (byte[][])args[0];
                    byte[][] payHashs = (byte[][])args[1];
                    BigInteger[] amounts = (BigInteger[])args[2];
                    BigInteger[] deadlines = (BigInteger[])args[3];
                    return setPayInfos(invokers, payHashs, amounts, deadlines);
                }
                if (operation == "getPayAmounts")
                {
                    BasicMethods.assert(args.Length == 2, "PayRegistry parameter error");
                    byte[][] payHashs = (byte[][])args[0];
                    BigInteger lastPayResolveDeadline = (BigInteger)args[1];
                    return getPayAmounts(payHashs, lastPayResolveDeadline);
                }
                if (operation == "getPayInfo")
                {
                    BasicMethods.assert(args.Length == 1, "PayRegistry parameter error");
                    byte[] payId = (byte[])args[0];
                    return getPayInfo(payId);
                }
            }
            return false;
        }

        [DisplayName("calculatePayId")]
        public static byte[] calculatePayId(byte[] payHash, byte[] setter)
        {
            BasicMethods.assert(BasicMethods._isByte32(payHash), "invalid hash");
            BasicMethods.assert(BasicMethods._isLegalAddress(setter), "invalid address");
            return SmartContract.Hash256(payHash.Concat(setter));
        }

        [DisplayName("setPayAmount")]
        public static object setPayAmount(byte[] invoker, byte[] payHash, BigInteger amount)
        {
            BasicMethods.assert(BasicMethods._isByte32(payHash), "invalid hash");
            BasicMethods.assert(BasicMethods._isLegalAddress(invoker), "invalid address");
            BasicMethods.assert(amount >= 0, "amount is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(invoker), "Checkwitness failed");
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
            BasicMethods.assert(BasicMethods._isByte32(payHash), "invalid hash");
            BasicMethods.assert(BasicMethods._isLegalAddress(invoker), "invalid address");
            BasicMethods.assert(deadline >= 0, "deadline is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(invoker), "Checkwitness failed");
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
            BasicMethods.assert(BasicMethods._isByte32(payHash), "invalid hash");
            BasicMethods.assert(BasicMethods._isLegalAddress(invoker), "invalid address");
            BasicMethods.assert(amount >= 0, "amount is less than zero");
            BasicMethods.assert(deadline >= 0, "deadline is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(invoker), "Checkwitness failed");
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
            BasicMethods.assert(arrayLen == payHashs.Length, "length does not match");
            BasicMethods.assert(arrayLen == amounts.Length, "length does not match");

            for (var i = 0; i < arrayLen; i++)
            {
                BasicMethods.assert((bool)setPayAmount(invokers[i], payHashs[i], amounts[i]), "error");
            }
            return true;
        }

        [DisplayName("setPayDeadlines")]
        public static object setPayDeadlines(byte[][] invokers, byte[][] payHashs, BigInteger[] deadlines)
        {
            BigInteger arrayLen = invokers.Length;
            BasicMethods.assert(arrayLen == payHashs.Length, "length does not match");
            BasicMethods.assert(arrayLen == deadlines.Length, "length does not match");

            for (var i = 0; i < arrayLen; i++)
            {
                BasicMethods.assert((bool)setPayDeadline(invokers[i], payHashs[i], deadlines[i]), "error");
            }
            return true;
        }

        [DisplayName("setPayInfos")]
        public static object setPayInfos(byte[][] invokers, byte[][] payHashs, BigInteger[] amounts, BigInteger[] deadlines)
        {
            BigInteger arrayLen = invokers.Length;
            BasicMethods.assert(arrayLen == payHashs.Length, "length does not match");
            BasicMethods.assert(arrayLen == amounts.Length, "length does not match");
            BasicMethods.assert(arrayLen == deadlines.Length, "length does not match");

            for (var i = 0; i < arrayLen; i++)
            {
                BasicMethods.assert((bool)setPayInfo(invokers[i], payHashs[i], amounts[i], deadlines[i]), "error");
            }

            return true;
        }


        [DisplayName("getPayAmounts")]
        public static BigInteger[] getPayAmounts(byte[][] payIds, BigInteger lastPayResolveDeadline)
        {
            BasicMethods.assert(BasicMethods._isByte32s(payIds), "payIds invalid");
            BasicMethods.assert(lastPayResolveDeadline >= 0, "lastPayResolveDeadline less than zero");

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
                    BasicMethods.assert(now > lastPayResolveDeadline, "payment is not finalized");
                }
                else
                {
                    BasicMethods.assert(now > payInfo.resolveDeadline, "payment is not finalized");
                }
                amounts[i] = payInfo.amount;
            }
            return amounts;
        }

        [DisplayName("getPayInfo")]
        public static BigInteger[] getPayInfo(byte[] payId)
        {
            BasicMethods.assert(BasicMethods._isByte32(payId), "payId invalid");
            byte[] payInfoBs = Storage.Get(Storage.CurrentContext, PayInfoPrefix.Concat(payId));
            PayInfo payInfo = new PayInfo();
            if (payInfoBs.Length > 0)
            {
                payInfo = Helper.Deserialize(payInfoBs) as PayInfo;
            }
            return new BigInteger[2] { payInfo.amount, payInfo.resolveDeadline };
        }
    }
}
