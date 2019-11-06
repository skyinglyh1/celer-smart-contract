﻿using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using System.ComponentModel;
using Helper = Neo.SmartContract.Framework.Helper;
using Neo.SmartContract.Framework.Services.System;

namespace AssetPool
{
    public class AssetPool : SmartContract
    {
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("deposit")]
        public static event Action<byte[], BigInteger> DepositEvent;

        [DisplayName("approve")]
        public static event Action<byte[], byte[], BigInteger> Approved;

        private static readonly string Name = "NEP5Pool";//¿Podría ser como un parámetro de inicialización?
        private static readonly string Symbol = "NVP";//Eso también.
        private static readonly byte[] Admin = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

        private static readonly byte[] TotalSupplyKey = "total".AsByteArray();
        private static readonly byte[] DecimalsKey = "decimals".AsByteArray();
        private static readonly byte[] NEP5HashKey = "nep5hash".AsByteArray();
        private static readonly byte[] CelerWalletHashKey = "celerwallethash".AsByteArray();

        //public static Map<byte[], BigInteger> balances;
        //private static Map<byte[], Map<byte[], BigInteger>> allowed;
        private static readonly byte[] BalancePrefix = "balance".AsByteArray();
        private static readonly byte[] ApprovePrefix = "approve".AsByteArray();

        public delegate object DynamicCallContract(string method, object[] args);

        static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "init")
                {
                    BasicMethods.assert(args.Length == 3, "AssetPool parameter error");
                    byte[] reversedNEP5Hash = (byte[])args[0];
                    BigInteger decimals = (BigInteger)args[1];
                    byte[] reversedCelerWalletHash = (byte[])args[2];
                    return init(reversedNEP5Hash, decimals, reversedCelerWalletHash);
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
                if (operation == "balanceOf")
                {
                    BasicMethods.assert(args.Length == 1, "AssetPool parameter error");
                    byte[] address = (byte[])args[0];
                    return balanceOf(address);
                }
                if (operation == "allowance")
                {
                    BasicMethods.assert(args.Length == 2, "AssetPool parameter error");
                    byte[] owner = (byte[])args[0];
                    byte[] spender = (byte[])args[1];
                    return allowance(owner, spender);
                }
                if (operation == "deposit")
                {
                    BasicMethods.assert(args.Length == 3, "AssetPool parameter error");
                    byte[] depositer = (byte[])args[0];
                    byte[] receiver = (byte[])args[1];
                    BigInteger amount = (BigInteger)args[2];
                    return deposit(depositer, receiver, amount);
                }
                if (operation == "withdraw")
                {
                    BasicMethods.assert(args.Length == 2, "AssetPool parameter error");
                    byte[] withdrawer = (byte[])args[0];
                    BigInteger value = (BigInteger)args[1];
                    return withdraw(withdrawer, value);
                }
                if (operation == "approve")
                {
                    BasicMethods.assert(args.Length == 3, "AssetPool parameter error");
                    byte[] owner = (byte[])args[0];
                    byte[] spender = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return approve(owner, spender, value);
                }
                if (operation == "transferFrom")
                {
                    BasicMethods.assert(args.Length == 4, "AssetPool parameter error");
                    byte[] spender = (byte[])args[0];
                    byte[] from = (byte[])args[1];
                    byte[] to = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return transferFrom(spender, from, to, amount);
                }
                if (operation == "transferToCelerWallet")
                {
                    BasicMethods.assert(args.Length == 5, "AssetPool parameter error");
                    byte[] invoker = (byte[])args[0];
                    byte[] from = (byte[])args[1];
                    byte[] walletAddr = (byte[])args[2];
                    byte[] walletId = (byte[])args[3];
                    BigInteger value = (BigInteger)args[4];
                    return transferToCelerWallet(invoker, from, walletAddr, walletId, value);
                }
                if (operation == "increaseAllowance")
                {
                    BasicMethods.assert(args.Length == 3, "AssetPool parameter error");
                    byte[] owner = (byte[])args[0];
                    byte[] spender = (byte[])args[1];
                    BigInteger addValue = (BigInteger)args[2];
                    return increaseAllowance(owner, spender, addValue);
                }
                if (operation == "decreaseAllowance")
                {
                    BasicMethods.assert(args.Length == 3, "AssetPool parameter error");
                    byte[] owner = (byte[])args[0];
                    byte[] spender = (byte[])args[1];
                    BigInteger subtractedValue = (BigInteger)args[2];
                    return decreaseAllowance(owner, spender, subtractedValue);
                }
            }
            return false;
        }

        [DisplayName("init")]
        public static object init(byte[] reversedNEP5Hash, BigInteger decimals, byte[] reversedCelerWalletHash)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(reversedNEP5Hash), "nep5 contract hash illegal");
            BasicMethods.assert(decimals >= 0, "decimals is less than 0");
            BasicMethods.assert(BasicMethods._isLegalAddress(reversedCelerWalletHash), "celer wallet contract hash illegal");

            BasicMethods.assert(Runtime.CheckWitness(Admin), "is not initialized by admin");

            Storage.Put(Storage.CurrentContext, NEP5HashKey, reversedNEP5Hash);
            Storage.Put(Storage.CurrentContext, DecimalsKey, decimals);
            Storage.Put(Storage.CurrentContext, CelerWalletHashKey, reversedCelerWalletHash);

            //TODO notify the event
            return true;
        }

        [DisplayName("name")]
        public static string name() => Name;

        [DisplayName("symbol")]
        public static string symbol() => Symbol;

        [DisplayName("decimals")]
        public static BigInteger decimals()
        {
            return Storage.Get(Storage.CurrentContext, DecimalsKey).AsBigInteger();
        }

        [DisplayName("balanceOf")]
        public static BigInteger balanceOf(byte[] owner)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(owner), "owner address is illegal");
            return Storage.Get(Storage.CurrentContext, BalancePrefix.Concat(owner)).AsBigInteger();
        }

        [DisplayName("allowance")]
        public static BigInteger allowance(byte[] owner, byte[] spender)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(owner), "owner address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(spender), "spender address is illegal");
            return Storage.Get(Storage.CurrentContext, ApprovePrefix.Concat(owner).Concat(spender)).AsBigInteger();
        }

        [DisplayName("deposit")]
        public static object deposit(byte[] depositer, byte[] receiver, BigInteger amount)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(depositer), "depositer address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(receiver), "receiver address is illegal");
            BasicMethods.assert(amount >= 0, "amount is less than 0");

            BasicMethods.assert(Runtime.CheckWitness(depositer), "Checkwitness failed");

            byte[] nep5Hash = Storage.Get(Storage.CurrentContext, NEP5HashKey);
            DynamicCallContract dyncall = (DynamicCallContract)nep5Hash.ToDelegate();
            BasicMethods.assert((bool)dyncall("transfer", new object[] { depositer, ExecutionEngine.ExecutingScriptHash, amount }), "transfer NEP5 token to the contract failed");
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(receiver), balanceOf(receiver) + amount);

            DepositEvent(receiver, amount);
            return true;
        }

        [DisplayName("withdraw")]
        public static object withdraw(byte[] withdrawer, BigInteger value)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(withdrawer), "withdrawer address is illegal");
            BasicMethods.assert(value >= 0, "amount is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(withdrawer), "Checkwitness failed");

            byte[] nep5Hash = Storage.Get(Storage.CurrentContext, NEP5HashKey);
            DynamicCallContract dyncall = (DynamicCallContract)nep5Hash.ToDelegate();
            BasicMethods.assert((BigInteger)dyncall("balanceOf", new object[] { ExecutionEngine.ExecutingScriptHash }) >= value, "the contract accout nep5 balance not enough");
            BasicMethods.assert(balanceOf(withdrawer) >= value, "withdrawer does not have enough balance");

            BasicMethods.assert(_transfer(withdrawer, withdrawer, value), "withdraw nep5 token failed");
            return true;
        }

        [DisplayName("approve")]
        public static object approve(byte[] owner, byte[] spender, BigInteger value)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(owner), "owner address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(spender), "spender address is illegal");
            BasicMethods.assert(value >= 0, "amount is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(owner), "Checkwitness failed");

            BasicMethods.assert(value <= balanceOf(owner), "value is greater than balance of owner");

            Storage.Put(Storage.CurrentContext, ApprovePrefix.Concat(owner).Concat(spender), value);

            Approved(owner, spender, value);

            return true;
        }

        [DisplayName("transferFrom")]
        public static object transferFrom(byte[] spender, byte[] from, byte[] to, BigInteger amount)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(spender), "spender address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(from), "from address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(to), "to address is illegal");
            BasicMethods.assert(amount >= 0, "amount is less than 0");

            BasicMethods.assert(Runtime.CheckWitness(spender), "CheckWitness failed");

            BigInteger approvedBalance = allowance(from, spender);
            BigInteger fromBalance = balanceOf(from);
            BigInteger toBalance = balanceOf(to);
            BasicMethods.assert(amount <= approvedBalance, "amount is greater than allowance of spender allowed to spend");
            BasicMethods.assert(amount <= fromBalance, "amount is greater than the owner's balance");

            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(from), fromBalance - amount);
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(to), toBalance + amount);
            Storage.Put(Storage.CurrentContext, ApprovePrefix.Concat(from).Concat(spender), approvedBalance - amount);

            Approved(from, spender, allowance(from, spender));
            Transferred(from, to, amount);
            return true;
        }

        [DisplayName("transferToCelerWallet")]
        public static object transferToCelerWallet(byte[] invoker, byte[] from, byte[] walletAddr, byte[] walletId, BigInteger value)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(invoker), "invoker or spender address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(from), "from address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(walletAddr), "to address is illegal");
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId is not byte32");
            BasicMethods.assert(value >= 0, "amount is less than 0");

            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");

            BigInteger approvedBalance = allowance(from, invoker);
            BasicMethods.assert(value <= approvedBalance, "value is greater than allowance of spender allowed to spend");
            Storage.Put(Storage.CurrentContext, ApprovePrefix.Concat(from).Concat(invoker), approvedBalance - value);
            Approved(from, invoker, allowance(from, invoker));

            BigInteger fromBalance = balanceOf(from);
            BasicMethods.assert(value <= fromBalance, "value is greater than the owner's balance");
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(from), fromBalance - value);
            Transferred(from, walletAddr, value);


            byte[] celerWalletHash = Storage.Get(Storage.CurrentContext, CelerWalletHashKey);
            byte[] nep5Hash = Storage.Get(Storage.CurrentContext, NEP5HashKey);
            DynamicCallContract dyncall = (DynamicCallContract)celerWalletHash.ToDelegate();
            BasicMethods.assert((bool)dyncall("depositNEP5", new object[] { invoker, walletId, nep5Hash, value }), "transfer NEP5 token to the to the celer wallet failed");

            return true;
        }

        [DisplayName("increaseAllowance")]
        public static object increaseAllowance(byte[] owner, byte[] spender, BigInteger addValue)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(owner), "owner address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(spender), "spender address is illegal");
            BasicMethods.assert(addValue >= 0, "addValue is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(owner), "Checkwitness failed");

            Storage.Put(Storage.CurrentContext, ApprovePrefix.Concat(owner).Concat(spender), allowance(owner, spender) + addValue);

            Approved(owner, spender, allowance(owner, spender));

            return true;
        }

        [DisplayName("decreaseAllowance")]
        public static object decreaseAllowance(byte[] owner, byte[] spender, BigInteger subtractedValue)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(owner), "owner address is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(spender), "spender address is illegal");
            BasicMethods.assert(subtractedValue >= 0, "addValue is less than zero");

            BasicMethods.assert(Runtime.CheckWitness(owner), "Checkwitness failed");

            BigInteger approvedBalance = allowance(owner, spender);
            BasicMethods.assert(approvedBalance >= subtractedValue, "not enough allowance to be subtracted");
            Storage.Put(Storage.CurrentContext, ApprovePrefix.Concat(owner).Concat(spender), allowance(owner, spender) - subtractedValue);

            Approved(owner, spender, allowance(owner, spender));

            return true;
        }

        //Transfer NEP5 for a specified addresses
        private static bool _transfer(byte[] from, byte[] to, BigInteger value)
        {
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(from), balanceOf(from) - value);

            byte[] nep5Hash = Storage.Get(Storage.CurrentContext, NEP5HashKey);
            DynamicCallContract dyncall = (DynamicCallContract)nep5Hash.ToDelegate();
            BasicMethods.assert((bool)dyncall("transfer", new object[] { ExecutionEngine.ExecutingScriptHash, to, value }), "transfer NEP5 token to the to as the withdrawer'd like to failed");

            Transferred(from, to, value);
            return true;
        }
    }
}
