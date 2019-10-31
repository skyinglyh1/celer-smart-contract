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
        public struct OpenChannelRequest
        {
            public byte[] channelInitializer;
            public byte[][] sigs;
        }

        public struct CooperativeWithdrawRequest
        {
            public byte[] withdrawInfo;
            public byte[][] sigs;
        }

        public struct CooperativeSettleRequest
        {
            public byte[] settleInfo;
            public byte[][] sigs;
        }

        public class ResolvePayByConditionsRequest
        {
            public byte[] condPay;
            public byte[][] hashPreimages;
        }

        public struct SignedSimplexState
        {
            public byte[] simplexState;
            public byte[][] sigs;
        }

        public struct SignedSimplexStateArray
        {
            public SignedSimplexState[] signedSimplexStates;
        }

        public struct ChannelMigrationRequest
        {
            public byte[] channelMigrationInfo;
            public byte[][] sigs;
        }

        public struct TokenType
        {
            public UInt16 INVALID;
            public UInt16 NEO;
            public UInt16 NEP5;
            public UInt16 GAS;
        }

        public struct TransferFunctionType
        {
            public byte BOOLEAN_AND;
            public byte BOOLEAN_OR;
            public byte BOOLEAN_CIRCUIT;
            public byte NUMERIC_ADD;
            public byte NUMERIC_MAX;
            public byte NUMERIC_MIN;
        }

        public struct ConditionType
        {
            public byte HASH_LOCK;
            public byte DEPLOYED_CONTRACT;
            public byte VIRTUAL_CONTRACT;
        }

        public struct AccountAmtPair
        {
            public byte[] account;
            public BigInteger amt;
        }

        public struct TokenInfo
        {
            public byte tokenType;
            public byte[] tokenAddress;
        }

        public struct TokenDistribution
        {
            public TokenInfo token;
            public AccountAmtPair[] distribution;
        }

        public struct TokenTransfer
        {
            public byte token;
            public AccountAmtPair receiver;
        }

        public struct PayIdList
        {
            public byte[][] payIds;
            public byte[] nextListHash;
        }

        public struct SimplexPaymentChannel
        {
            public byte[] channelId;
            public byte[] peerFrom;
            public BigInteger seqNum;
            public TokenTransfer transferToPeer;
            public PayIdList pendingPayIds;
            public BigInteger lastPayResolveDeadline;
            public BigInteger totalPendingAmount;
        }

        public struct TransferFunction
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

        public struct Condition
        {
            public byte conditionType;
            public byte[] hashLock;
            public byte[] deployedContractAddress;
            public byte[] virtualContractAddress;
            public byte[] argsQueryFinalization;
            public byte[] argsQueryOutcome;
        }

        public struct CooperativeWithdrawInfo
        {
            public byte[] channelId;
            public BigInteger seqNum;
            public AccountAmtPair withdraw;
            public BigInteger withdrawDeadline;
            public byte[] recipientChannelId;
        }

        public struct PaymentChannelInitializer
        {
            public TokenDistribution initDistribution;
            public BigInteger openDeadline;
            public BigInteger disputeTimeout;
            public BigInteger msgValueReceiver;
        }

        public struct CooperativeSettleInfo
        {
            public byte[] channelId;
            public BigInteger seqNum;
            public AccountAmtPair[] settleBalance;
            public BigInteger settleDeadline;
        }

        public struct ChannelMigrationInfo
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

        [DisplayName("resolvePayment")]
        public static event Action<byte[], BigInteger, BigInteger> ResolvePayment;

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
                    assert(args.Length == 2, "PayResolver parameter error");
                    byte[] revPayRegistryAddr = (byte[])args[0];
                    byte[] revVirtResolverAddr = (byte[])args[1];
                    return init(revPayRegistryAddr, revVirtResolverAddr);
                }
                if (operation == "getPayRegistryHash")
                {
                    return getVirtResolverHash();
                }
                if (operation == "resolvePaymentByConditions")
                {
                    assert(args.Length == 1, "PayResolver parameter error");
                    byte[] resolvePayRequestBs = (byte[])args[0];
                    return resolvePaymentByConditions(resolvePayRequestBs);
                }
                if (operation == "resolvePaymentByVouchedResult")
                {
                    assert(args.Length == 1, "PayResolver parameter error");
                    byte[] vouchedPayResultBs = (byte[])args[0];
                    return resolvePaymentByVouchedResult(vouchedPayResultBs);
                }
            }
            return false;
        }

        [DisplayName("init")]
        public static object init(byte[] revPayRegistryAddr, byte[] revVirtResolverAddr)
        {
            assert(_isLegalAddress(revPayRegistryAddr), "invalid contract address");
            assert(_isLegalAddress(revVirtResolverAddr), "invalid contract address");
            Storage.Put(Storage.CurrentContext, PayRegistryHashKey, revPayRegistryAddr);
            Storage.Put(Storage.CurrentContext, VirtResolverHashKey, revVirtResolverAddr);
            return true;
        }
        
        [DisplayName("getVirtResolverHash")]
        public static byte[] getVirtResolverHash()
        {
            byte[] payRegistryHash = Storage.Get(Storage.CurrentContext, VirtResolverHashKey);
            assert(_isLegalAddress(payRegistryHash), "empty pay registry contract hash");
            return payRegistryHash;
        }

        [DisplayName("getPayRegistryHash")]
        public static byte[] getPayRegistryHash()
        {
            byte[] payRegistryHash = Storage.Get(Storage.CurrentContext, PayRegistryHashKey);
            assert(_isLegalAddress(payRegistryHash), "empty pay registry contract hash");
            return payRegistryHash;
        }

        [DisplayName("resolvePaymentByConditions")]
        public static object resolvePaymentByConditions(byte[] resolvePayRequestBs)
        {
            ResolvePayByConditionsRequest resolvePayRequest = new ResolvePayByConditionsRequest();
            resolvePayRequest = Helper.Deserialize(resolvePayRequestBs) as ResolvePayByConditionsRequest;
            ConditionalPay pay = Helper.Deserialize(resolvePayRequest.condPay) as ConditionalPay;
            byte funcType = pay.transferFunc.logicType;
            BigInteger amount = 0;
            TransferFunctionType TransferFunctionType = getTransferFunctionType();
            if (funcType == TransferFunctionType.BOOLEAN_AND)
            {
                amount = _calculateBooleanAndPayment(pay, resolvePayRequest.hashPreimages);
            }else if (funcType == TransferFunctionType.BOOLEAN_OR)
            {
                amount = _calculateBooleanOrPayment(pay, resolvePayRequest.hashPreimages);
            } else if (_isNumericLogic(funcType))
            {
                amount = _calculateNumericLogicPayment(pay, resolvePayRequest.hashPreimages, funcType);
            }else
            {
                assert(false, "error");
            }
            byte[] payHash = SmartContract.Sha256(resolvePayRequest.condPay);
            _resolvePayment(pay, payHash, amount);
            return true;
        }

        [DisplayName("resolvePaymentByVouchedResult")]
        public static object resolvePaymentByVouchedResult(byte[] vouchedPayResultBs)
        {
            VouchedCondPayResult vouchedPayResult = new VouchedCondPayResult();
            vouchedPayResult = Helper.Deserialize(vouchedPayResultBs) as VouchedCondPayResult;
            CondPayResult payResult = Helper.Deserialize(vouchedPayResult.condPayResult) as CondPayResult;
            ConditionalPay pay = Helper.Deserialize(payResult.condPay) as ConditionalPay;

            assert(payResult.amount <= pay.transferFunc.maxTransfer.receiver.amt, "exceed max transfer amount");
            byte[] hash = SmartContract.Sha256(vouchedPayResult.condPayResult);
            bool srcVerifiedRes = SmartContract.VerifySignature(hash, vouchedPayResult.sigOfSrc, pay.src);
            bool destVerifiedRes = SmartContract.VerifySignature(hash, vouchedPayResult.sigOfDest, pay.dest);
            assert(srcVerifiedRes && destVerifiedRes, "verify signature failed");

            byte[] payHash = SmartContract.Sha256(payResult.condPay);

            _resolvePayment(pay, payHash, payResult.amount);

            return true;
        }

        public static void _resolvePayment(ConditionalPay _pay, byte[] _payHash, BigInteger _amount)
        {
            assert(_amount >= 0, "amount is less than zero");

            BigInteger now = Blockchain.GetHeight();
            assert(now <= _pay.resolveDeadline, "passed pay resolve deadline in condPay msg");

            byte[] payId = _calculatePayId(_payHash, ExecutionEngine.ExecutingScriptHash);

            byte[] payRegistryHash = getPayRegistryHash();
            DynamicCallContract dyncall = (DynamicCallContract)payRegistryHash.ToDelegate();
            BigInteger[] res = (BigInteger[]) dyncall("getPayInfo", new object[] { payId });
            BigInteger currentAmt = res[0];
            BigInteger currentDeadline = res[1];

            assert(
                currentDeadline == 0 || now <= currentDeadline,
                "Passed onchain resolve pay deadline"
                );
            if (currentDeadline > 0)
            {
                assert(_amount > currentAmt, "New amount is not larger");
                if (_amount == _pay.transferFunc.maxTransfer.receiver.amt)
                {
                    
                    assert((bool)dyncall("setPayInfo", new object[] { _payHash, _amount, now}), "setPayInfo error");
                    ResolvePayment(payId, _amount, now);
                }
                else
                {
                    assert((bool)dyncall("setPayAmount", new object[] { _payHash, _amount }), "setPayAmount error");
                    ResolvePayment(payId, _amount, currentDeadline);
                }
            }
            else
            {
                BigInteger newDeadline = 0;
                if (_amount == _pay.transferFunc.maxTransfer.receiver.amt)
                {
                    newDeadline = now;
                }
                else
                {
                    newDeadline = min(now + _pay.resolveTimeout, _pay.resolveDeadline);
                    assert(newDeadline > 0, "new resolve deadline is not greater than 0");
                }
                
                assert((bool)dyncall("setPayInfo", new object[] { _payHash, _amount, newDeadline }), "setPayInfo error");
                ResolvePayment(payId, _amount, currentDeadline);
            }
        }

        private static BigInteger _calculateBooleanAndPayment(ConditionalPay _pay, byte[][] _preimages)
        {
            int j = 0;
            bool hasFalseContractCond = false;
            ConditionType ConditionType = getConditionType();
            for (var i = 0; i < _pay.conditions.Length; i++)
            {
                Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    assert(SmartContract.Sha256(_preimages[j]) == cond.hashLock, "wrong preimage");
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
                    bool outcome = (bool)dyncall("getOutcome", new object[] { cond.argsQueryOutcome });
                    if (!outcome){
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


        private static BigInteger _calculateBooleanOrPayment(ConditionalPay _pay, byte[][] _preimages)
        {
            int j = 0;
            bool hasContractCond = false;
            bool hasTrueContractCond = false;
            ConditionType ConditionType = getConditionType();
            for (var i = 0; i < _pay.conditions.Length; i++)
            {
                Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    assert(SmartContract.Sha256(_preimages[j]) == cond.hashLock, "wrong preimage");
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
                    hasContractCond = true;

                    bool outcome = (bool)dyncall("getOutcome", new object[] { cond.argsQueryOutcome });
                    if (outcome)
                    {
                        hasTrueContractCond = true;
                    }
                }
                else
                {
                    assert(false, "condition type error");
                }
            }
            if (!hasContractCond || hasTrueContractCond)
            {
                return _pay.transferFunc.maxTransfer.receiver.amt;
            }
            else
            {
                return 0;
            }
        }

        private static BigInteger _calculateNumericLogicPayment(ConditionalPay _pay, byte[][] _preimages, byte _funcType)
        {
            int j = 0;
            BigInteger amount = 0;
            bool hasContracCond = false;
            ConditionType ConditionType = getConditionType();
            TransferFunctionType TransferFunctionType = getTransferFunctionType();
            for (var i = 0; i < _pay.conditions.Length; i++)
            {
                Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    assert(SmartContract.Sha256(_preimages[j]) == cond.hashLock, "wrong preimage");
                    j++;
                }
                else if (
                   cond.conditionType == ConditionType.DEPLOYED_CONTRACT ||
                   cond.conditionType == ConditionType.VIRTUAL_CONTRACT
                   )
                {
                    byte[] numericCondHash = _getCondAddress(cond);
                    DynamicCallContract dyncall = (DynamicCallContract)numericCondHash.ToDelegate();
                    assert((bool)dyncall("isFinalized", new object[] { cond.argsQueryFinalization }), "Condition is not finalized");

                    BigInteger outcome = (BigInteger)dyncall("getOutcome", new object[] { cond.argsQueryOutcome });

                    if (_funcType == TransferFunctionType.NUMERIC_ADD)
                    {
                        amount = amount + outcome;
                    }else if (_funcType == TransferFunctionType.NUMERIC_MAX)
                    {
                        amount = max(amount, outcome);
                    }
                    else if (_funcType == TransferFunctionType.NUMERIC_MIN)
                    {
                        if (hasContracCond)
                        {
                            amount = min(amount, outcome);
                        }
                        else
                        {
                            amount = outcome;
                        }
                        
                    }
                    else
                    {
                        assert(false, "error");
                    }
                    hasContracCond = true;
                }
                else
                {
                    assert(false, "condition type error");
                }
            }
            if (hasContracCond)
            {
                assert(amount <= _pay.transferFunc.maxTransfer.receiver.amt, "exceed max transfer amount");
                return amount;
            }
            else
            {
                return _pay.transferFunc.maxTransfer.receiver.amt;
            }
        }

        private static bool _isNumericLogic(byte _funcType)
        {
            TransferFunctionType TransferFunctionType = getTransferFunctionType();
            return _funcType == TransferFunctionType.NUMERIC_ADD || _funcType == TransferFunctionType.NUMERIC_MAX || _funcType == TransferFunctionType.NUMERIC_MIN;
        }

        private static byte[] _calculatePayId(byte[] _payHash, byte[] _setter)
        {
            return SmartContract.Sha256(_payHash.Concat(_setter));
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
                assert(arr[i] < 4, "token type error");
                arrb[i] = arr[i].AsByte();
            }
            return arrb;
        }

        private static TokenType getTokenType()
        {
            TokenType tt = new TokenType();
            tt.INVALID = 0.ToByte();
            tt.NEO = 1.ToByte();
            tt.NEP5 = 2.ToByte();
            tt.GAS = 3.ToByte();
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

        private static BigInteger max(BigInteger a, BigInteger b)
        {
            assert(a >= 0 && b >= 0, "value is less than zero");
            if (a >= b) return a;
            return b;
        }

        private static BigInteger min(BigInteger a, BigInteger b)
        {
            assert(a >= 0 && b >= 0, "value is less than zero");
            if (a <= b) return a;
            return b;
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
