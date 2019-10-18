using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;
using System.ComponentModel;

using Neo.SmartContract.Framework.Services.System;


namespace PayResolver
{
    public class PayResolver : SmartContract
    {
        public class OpenChannelRequest
        {
            public byte[] channelInitializer;
            public byte[][] sigs;
        }
        public class CooperativeWithdrawRequest
        {
            public byte[] withdrawInfo;
            public byte[][] sigs;
        }
        public class CooperativeSettleRequest
        {
            public byte[] settleInfo;
            public byte[][] sigs;
        }
        public class ResolvePayByConditionsRequest
        {
            public byte[] condPay;
            public byte[][] hashPreimages;
        }
        public class SignedSimplexState
        {
            public byte[] simplexState;
            public byte[][] sigs;
        }
        public class SignedSimplexStateArray
        {
            public SignedSimplexState[] signedSimplexStates;
        }
        public class ChannelMigrationRequest
        {
            public byte[] channelMigrationInfo;
            public byte[][] sigs;
        }


        public class TokenType
        {
            public byte INVALID;
            public byte NEP5;
        }
        public class TransferFunctionType
        {
            public byte BOOLEAN_AND;
            public byte BOOLEAN_OR;
            public byte BOOLEAN_CIRCUIT;
            public byte NUMERIC_ADD;
            public byte NUMERIC_MAX;
            public byte NUMERIC_MIN;
        }
        public class ConditionType
        {
            public byte HASH_LOCK;
            public byte DEPLOYED_CONTRACT;
            public byte VIRTUAL_CONTRACT;
        }

        public class AccountAmtPair
        {
            public byte[] account;
            public BigInteger amt;
        }
        public class TokenInfo
        {
            public byte tokenType;
            public byte[] tokenAddress;
        }
        public class TokenDistribution
        {
            public TokenInfo token;
            public AccountAmtPair[] distribution;
        }
        public class TokenTransfer
        {
            public byte token;
            public AccountAmtPair receiver;
        }
        public class PayIdList
        {
            public byte[][] payIds;
            public byte[] nextListHash;
        }
        public class SimplexPaymentChannel
        {
            public byte[] channelId;
            public byte[] peerFrom;
            public BigInteger seqNum;
            public TokenTransfer transferToPeer;
            public PayIdList pendingPayIds;
            public BigInteger lastPayResolveDeadline;
            public BigInteger totalPendingAmount;
        }
        public class TransferFunction
        {
            public byte logicType;
            public TokenTransfer maxTransfer;
        }
        public class ConditionalPay
        {
            public BigInteger payTimestamp;
            public byte[] src;
            public byte[] dest;
            public Condition[] conditions;
            public TransferFunction transferFunc;
            public BigInteger resolveDeadline;
            public BigInteger resolveTimeout;
            public byte[] payResolver;
        }
        public class VouchedCondPayResult
        {
            public byte[] condPayResult;
            public byte[] sigOfSrc;
            public byte[] sigOfDest;
        }
        public class CondPayResult
        {
            public byte[] condPay;
            public BigInteger amount;
        }
        public class Condition
        {
            public byte conditionType;
            public byte[] hashLock;
            public byte[] deployedContractAddress;
            public byte[] virtualContractAddress;
            public byte[] argsQueryFinalization;
            public byte[] argsQueryOutcome;
        }
        public class CooperativeWithdrawInfo
        {
            public byte[] channelId;
            public BigInteger seqNum;
            public AccountAmtPair withdraw;
            public BigInteger withdrawDeadline;
            public byte[] recipientChannelId;
        }
        public class PaymentChannelInitializer
        {
            public TokenDistribution initDistribution;
            public BigInteger openDeadline;
            public BigInteger disputeTimeout;
            public BigInteger msgValueReceiver;
        }
        public class CooperativeSettleInfo
        {
            public byte[] channelId;
            public BigInteger seqNum;
            public AccountAmtPair[] settleBalance;
            public BigInteger settleDeadline;
        }
        public class ChannelMigrationInfo
        {
            public byte[] channelId;
            public byte[] fromLedgerAddress;
            public byte[] toLedgerAddress;
            public BigInteger migrationDeadline;
        }



        public static readonly byte[] PayRegistryHashKey = "payRegistry".AsByteArray();
        public static readonly byte[] VirtResolverHashKey = "virtResolver".AsByteArray();
        public static readonly byte[] AddressZero = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");
        public delegate object DynamicCallContract(string method, object[] args);


        [DisplayName("init")]
        public static object init(byte[] revPayRegistryAddr, byte[] revVirtResolverAddr)
        {
            assert(_isLegalAddress(revPayRegistryAddr), "invalid contract address");
            assert(_isLegalAddress(revVirtResolverAddr), "invalid contract address");
            Storage.Put(Storage.CurrentContext, PayRegistryHashKey, revPayRegistryAddr);
            Storage.Put(Storage.CurrentContext, VirtResolverHashKey, revVirtResolverAddr);
            return true;
        }

        public static resolvePaymentByConditions(byte[] resolvePayRequestBs)
        {
            ResolvePayByConditionsRequest resolvePayRequest = new ResolvePayByConditionsRequest();
            resolvePayRequest = Helper.Deserialize(resolvePayRequestBs) as ResolvePayByConditionsRequest;
            ConditionalPay pay = Helper.Deserialize(resolvePayRequest.condPay) as ConditionalPay;
            byte funcType = pay.transferFunc.logicType;
            BigInteger amount;
            TransferFunctionType TransferFunctionType = getTransferFunctionType();
            if (funcType == TransferFunctionType.BOOLEAN_AND)
            {
                amount = _calculateBooleanAndPayment(pay, resolvePayRequest.hashPreimages);
            }
        }

        private static BigInteger _calculateBooleanAndPayment(ConditionalPay _pay, byte[][] _preimages)
        {
            BigInteger j = 0;
            bool hasFalseContractCond = false;
            ConditionType ConditionType = getConditionType();
            for (var i = 0; i < _pay.conditions.Length; i++)
            {
                Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    assert(SmartContract.Sha256(_preimages[i]) == cond.hashLock, "wrong preimage");
                    j++;
                }
                else if (
                   cond.conditionType == ConditionType.DEPLOYED_CONTRACT ||
                   cond.conditionType == ConditionType.VIRTUAL_CONTRACT
                   )
                {
                    byte[] booleanCondHash = _getCondAddress(cond);
                    DynamicCallContract dyncall = (DynamicCallContract)booleanCondHash.ToDelegate();
                    assert((bool)dyncall("isFinalized", new object[] { cond.argsQueryFinalization }), "Condition is not finalized");
                    bool outcome = (bool)dyncall("getOutcome", new object[] { cond.argsQueryOutcome }){
                        hasFalseContractCond = true;
                    }
                }
                else
                {
                    assert(false, "condition type error");
                }
            }
            if (hasFalseContractCond)
            {
                return 0;
            }
            else
            {
                return _pay.transferFunc.maxTransfer.receiver.amt;
            }
        }
















        private static byte[] _getCondAddress(Condition _cond)
        {
            ConditionType ConditionType = getConditionType();
            if (_cond.conditionType == ConditionType.DEPLOYED_CONTRACT)
            {
                return _cond.deployedContractAddress;
            }
            else if (_cond.conditionType == ConditionType.VIRTUAL_CONTRACT)
            {
                byte[] virtResolverHash = Storage.Get(Storage.CurrentContext, VirtResolverHashKey);
                DynamicCallContract dyncall = (DynamicCallContract)virtResolverHash.ToDelegate();
                return (byte[])dyncall("resolve", new object[] { _cond.virtualContractAddress });
            }
            assert(false, "conditiontype error");
            return new byte[] { 0x00 };
        }



        private static byte[] TokenTypes(BigInteger[] arr)
        {
            byte[] arrb = new byte[arr.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                assert(arr[i] < 3, "token type error");
                arrb[i] = arr[i].AsByte();
            }
            return arrb;
        }

        private static TokenType getTokenType()
        {
            TokenType tt = new TokenType();
            tt.INVALID = 0.ToByte();
            tt.NEP5 = 1.ToByte();
            return tt;
        }
        private static TransferFunctionType getTransferFunctionType()
        {
            TransferFunctionType tft = new TransferFunctionType();
            tft.BOOLEAN_AND = 0.ToByte();
            tft.BOOLEAN_OR = 1.ToByte();
            tft.BOOLEAN_CIRCUIT = 2.ToByte();
            tft.NUMERIC_ADD = 3.ToByte();
            tft.NUMERIC_MAX = 4.ToByte();
            tft.NUMERIC_MIN = 5.ToByte();
            return tft;
        }
        private static ConditionType getConditionType()
        {
            ConditionType ct = new ConditionType();
            ct.HASH_LOCK = 0.ToByte();
            ct.DEPLOYED_CONTRACT = 1.ToByte();
            ct.VIRTUAL_CONTRACT = 2.ToByte();
            return ct;
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
            return addr.Length == 0 && addr != AddressZero;
        }
    }
}
