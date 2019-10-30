using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using System.ComponentModel;
using Helper = Neo.SmartContract.Framework.Helper;
namespace NEP5
{
    public class NEP5 : SmartContract
    {
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("approve")]
        public static event Action<byte[], byte[], BigInteger> Approved;

        private static readonly string Name = "NEP5Token";
        private static readonly string Symbol = "NVT";
        private static readonly BigInteger Factor = 100000000;
        private static readonly BigInteger Decimals = 8;
        private static readonly BigInteger InitialSupply = 100000000;
        private static readonly byte[] Admin = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

        private static readonly byte[] AddressZero = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

        private static readonly byte[] TotalSupplyKey = "total".AsByteArray();

        //public static Map<byte[], BigInteger> balances;
        //private static Map<byte[], Map<byte[], BigInteger>> allowed;
        private static readonly byte[] BalancePrefix = "balance".AsByteArray();
        private static readonly byte[] ApprovePrefix = "approve".AsByteArray();

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "init")
                {
                    return init();
                }
                if (operation == "symbol")
                {
                    return symbol();
                }
                if (operation == "name")
                {
                    return name();
                }
                if (operation == "decimals")
                {
                    return decimals();
                }
                if (operation == "totalSupply")
                {
                    return totalSupply();
                }
                if (operation == "balanceOf")
                {
                    assert(args.Length == 1, "NEP5 parameter error");
                    byte[] address = (byte[])args[0];
                    return balanceOf(address);
                }
                if (operation == "transfer")
                {
                    assert(args.Length == 3, "NEP5 parameter error");
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    return transfer(from, to, amount);
                }
                if (operation == "transferMulti")
                {
                    return transferMulti(args);
                }
            }
            return false;
        }


        [DisplayName("init")]
        public static object init()
        {
            assert(totalSupply() == 0, "contract has already been initilaized");
            BigInteger supply = Factor * InitialSupply;
            Storage.Put(Storage.CurrentContext, TotalSupplyKey, supply);
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(Admin), supply);
            Transferred(null, Admin, supply);
            return true;
        }

        [DisplayName("name")]
        public static string name() => Name;

        [DisplayName("symbol")]
        public static string symbol() => Symbol;

        [DisplayName("decimals")]
        public static BigInteger decimals() => Decimals;

        [DisplayName("totalSupply")]
        public static BigInteger totalSupply()
        {
            return Storage.Get(Storage.CurrentContext, TotalSupplyKey).AsBigInteger();
        }

        [DisplayName("balanceOf")]
        public static BigInteger balanceOf(byte[] address)
        {
            assert(_isLegalAddress(address), "address is illegal");
            return Storage.Get(Storage.CurrentContext, BalancePrefix.Concat(address)).AsBigInteger();
        }

        [DisplayName("transfer")]
        public static object transfer(byte[] from, byte[] to, BigInteger amount)
        {
            assert(_isLegalAddress(from), "from address is illegal");
            assert(_isLegalAddress(to), "to address is illegal");
            assert(amount >= 0, "amount is less than 0");

            assert(Runtime.CheckWitness(from), "CheckWitness failed");

            BigInteger fromBalance = balanceOf(from);
            assert(fromBalance >= amount, "from address not enough balance");
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(from), fromBalance - amount);
            BigInteger toBalance = balanceOf(to);
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(to), toBalance + amount);

            Transferred(from, to, amount);
            return true;
        }

        public struct State
        {
            public byte[] from;
            public byte[] to;
            public BigInteger amount;
        }
        [DisplayName("transferMulti")]
        public static object transferMulti(object[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                State state = (State)args[i];
                assert((bool)transfer(state.from, state.to, state.amount), "transfer failed");
            }
            return true;
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
    }
}
