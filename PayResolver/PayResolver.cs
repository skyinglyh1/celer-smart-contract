using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;

namespace PayResolver
{
    public class PayResolver : SmartContract
    {
        public static readonly byte[] PayRegistryHashKey = "payRegistry".AsByteArray();
        public static readonly byte[] VirtResolverHashKey = "virtResolver".AsByteArray();
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
                    BasicMethods.assert(args.Length == 2, "PayResolver parameter error");
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
                    BasicMethods.assert(args.Length == 1, "PayResolver parameter error");
                    byte[] resolvePayRequestBs = (byte[])args[0];
                    return resolvePaymentByConditions(resolvePayRequestBs);
                }
                if (operation == "resolvePaymentByVouchedResult")
                {
                    BasicMethods.assert(args.Length == 1, "PayResolver parameter error");
                    byte[] vouchedPayResultBs = (byte[])args[0];
                    return resolvePaymentByVouchedResult(vouchedPayResultBs);
                }
            }
            return false;
        }

        [DisplayName("init")]
        public static object init(byte[] revPayRegistryAddr, byte[] revVirtResolverAddr)
        {
            BasicMethods.assert(BasicMethods._isLegalAddress(revPayRegistryAddr), "invalid contract address");
            BasicMethods.assert(BasicMethods._isLegalAddress(revVirtResolverAddr), "invalid contract address");
            Storage.Put(Storage.CurrentContext, PayRegistryHashKey, revPayRegistryAddr);
            Storage.Put(Storage.CurrentContext, VirtResolverHashKey, revVirtResolverAddr);
            return true;
        }
        
        [DisplayName("getVirtResolverHash")]
        public static byte[] getVirtResolverHash()
        {
            byte[] payRegistryHash = Storage.Get(Storage.CurrentContext, VirtResolverHashKey);
            BasicMethods.assert(BasicMethods._isLegalAddress(payRegistryHash), "empty pay registry contract hash");
            return payRegistryHash;
        }

        [DisplayName("getPayRegistryHash")]
        public static byte[] getPayRegistryHash()
        {
            byte[] payRegistryHash = Storage.Get(Storage.CurrentContext, PayRegistryHashKey);
            BasicMethods.assert(BasicMethods._isLegalAddress(payRegistryHash), "empty pay registry contract hash");
            return payRegistryHash;
        }

        [DisplayName("resolvePaymentByConditions")]
        public static object resolvePaymentByConditions(byte[] resolvePayRequestBs)
        {
            PbChain.ResolvePayByConditionsRequest resolvePayRequest = new PbChain.ResolvePayByConditionsRequest();
            resolvePayRequest = (PbChain.ResolvePayByConditionsRequest)Helper.Deserialize(resolvePayRequestBs);
            PbEntity.ConditionalPay pay = Helper.Deserialize(resolvePayRequest.condPay) as PbEntity.ConditionalPay;
            PbEntity.TransferFunction function = pay.transferFunc;
            byte funcType = function.logicType;
            BigInteger amount = 0;
            PbEntity.TransferFunctionType TransferFunctionType = PbEntity.getTransferFunctionType();
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
                BasicMethods.assert(false, "error");
            }
            byte[] payHash = SmartContract.Sha256(resolvePayRequest.condPay);
            _resolvePayment(pay, payHash, amount);
            return true;
        }

        [DisplayName("resolvePaymentByVouchedResult")]
        public static object resolvePaymentByVouchedResult(byte[] vouchedPayResultBs)
        {
            PbEntity.VouchedCondPayResult vouchedPayResult = new PbEntity.VouchedCondPayResult();
            vouchedPayResult = (PbEntity.VouchedCondPayResult)Helper.Deserialize(vouchedPayResultBs);
            PbEntity.CondPayResult payResult = (PbEntity.CondPayResult)Helper.Deserialize(vouchedPayResult.condPayResult);
            PbEntity.ConditionalPay pay = (PbEntity.ConditionalPay)Helper.Deserialize(payResult.condPay);

            PbEntity.TransferFunction transaferFunction = pay.transferFunc;
            PbEntity.TokenTransfer tokenTransfer = transaferFunction.maxTransfer;
            PbEntity.AccountAmtPair accountAmtPair = tokenTransfer.receiver;
            BasicMethods.assert(payResult.amount <= accountAmtPair.amt, "exceed max transfer amount");
            byte[] hash = SmartContract.Sha256(vouchedPayResult.condPayResult);
            bool srcVerifiedRes = SmartContract.VerifySignature(hash, vouchedPayResult.sigOfSrc, pay.src);
            bool destVerifiedRes = SmartContract.VerifySignature(hash, vouchedPayResult.sigOfDest, pay.dest);
            BasicMethods.assert(srcVerifiedRes && destVerifiedRes, "verify signature failed");

            byte[] payHash = SmartContract.Sha256(payResult.condPay);

            _resolvePayment(pay, payHash, payResult.amount);

            return true;
        }

        public static void _resolvePayment(PbEntity.ConditionalPay _pay, byte[] _payHash, BigInteger _amount)
        {
            BasicMethods.assert(_amount >= 0, "amount is less than zero");

            BigInteger now = Blockchain.GetHeight();
            BasicMethods.assert(now <= _pay.resolveDeadline, "passed pay resolve deadline in condPay msg");

            byte[] payId = _calculatePayId(_payHash, ExecutionEngine.ExecutingScriptHash);

            byte[] payRegistryHash = getPayRegistryHash();
            DynamicCallContract dyncall = (DynamicCallContract)payRegistryHash.ToDelegate();
            BigInteger[] res = (BigInteger[]) dyncall("getPayInfo", new object[] { payId });
            BigInteger currentAmt = res[0];
            BigInteger currentDeadline = res[1];

            BasicMethods.assert(
                currentDeadline == 0 || now <= currentDeadline,
                "Passed onchain resolve pay deadline"
                );
            PbEntity.TransferFunction transferFunction = _pay.transferFunc;
            PbEntity.TokenTransfer tokenTransfer = transferFunction.maxTransfer;
            PbEntity.AccountAmtPair accountAmtPair = tokenTransfer.receiver;
            if (currentDeadline > 0)
            {
                BasicMethods.assert(_amount > currentAmt, "New amount is not larger");
                
                if (_amount == accountAmtPair.amt)
                {
                    BasicMethods.assert((bool)dyncall("setPayInfo", new object[] { _payHash, _amount, now}), "setPayInfo error");
                    ResolvePayment(payId, _amount, now);
                }
                else
                {
                    BasicMethods.assert((bool)dyncall("setPayAmount", new object[] { _payHash, _amount }), "setPayAmount error");
                    ResolvePayment(payId, _amount, currentDeadline);
                }
            }
            else
            {
                BigInteger newDeadline = 0;
                if (_amount == accountAmtPair.amt)
                {
                    newDeadline = now;
                }
                else
                {
                    newDeadline = min(now + _pay.resolveTimeout, _pay.resolveDeadline);
                    BasicMethods.assert(newDeadline > 0, "new resolve deadline is not greater than 0");
                }

                BasicMethods.assert((bool)dyncall("setPayInfo", new object[] { _payHash, _amount, newDeadline }), "setPayInfo error");
                ResolvePayment(payId, _amount, currentDeadline);
            }
        }

        private static BigInteger _calculateBooleanAndPayment(PbEntity.ConditionalPay _pay, byte[][] _preimages)
        {
            int j = 0;
            bool hasFalseContractCond = false;
            PbEntity.ConditionType ConditionType = PbEntity.getConditionType();
            PbEntity.Condition[] conditions = _pay.conditions;
            for (var i = 0; i < conditions.Length; i++)
            {
                PbEntity.Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    BasicMethods.assert(SmartContract.Sha256(_preimages[j]) == cond.hashLock, "wrong preimage");
                    j++;
                }
                else if (
                   cond.conditionType == ConditionType.DEPLOYED_CONTRACT ||
                   cond.conditionType == ConditionType.VIRTUAL_CONTRACT
                   )
                {
                    byte[] booleanCondHash = _getCondAddress(cond);
                    DynamicCallContract dyncall = (DynamicCallContract)booleanCondHash.ToDelegate();
                    BasicMethods.assert((bool)dyncall("isFinalized", new object[] { cond.argsQueryFinalization }), "Condition is not finalized");
                    bool outcome = (bool)dyncall("getOutcome", new object[] { cond.argsQueryOutcome });
                    if (!outcome){
                        hasFalseContractCond = true;
                    }
                }
                else
                {
                    BasicMethods.assert(false, "condition type error");
                }
            }
            if (hasFalseContractCond)
            {
                return 0;
            }
            else
            {
                PbEntity.TransferFunction transferFunction = _pay.transferFunc;
                PbEntity.TokenTransfer tokenTransfer = transferFunction.maxTransfer;
                PbEntity.AccountAmtPair accountAmtPair = tokenTransfer.receiver;
                return accountAmtPair.amt;
            }
        }

        private static BigInteger _calculateBooleanOrPayment(PbEntity.ConditionalPay _pay, byte[][] _preimages)
        {
            int j = 0;
            bool hasContractCond = false;
            bool hasTrueContractCond = false;
            PbEntity.ConditionType ConditionType = PbEntity.getConditionType();
            PbEntity.Condition[] conditions = _pay.conditions;
            for (var i = 0; i < conditions.Length; i++)
            {
                PbEntity.Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    BasicMethods.assert(SmartContract.Sha256(_preimages[j]) == cond.hashLock, "wrong preimage");
                    j++;
                }
                else if (
                   cond.conditionType == ConditionType.DEPLOYED_CONTRACT ||
                   cond.conditionType == ConditionType.VIRTUAL_CONTRACT
                   )
                {
                    byte[] booleanCondHash = _getCondAddress(cond);
                    DynamicCallContract dyncall = (DynamicCallContract)booleanCondHash.ToDelegate();
                    BasicMethods.assert((bool)dyncall("isFinalized", new object[] { cond.argsQueryFinalization }), "Condition is not finalized");
                    hasContractCond = true;

                    bool outcome = (bool)dyncall("getOutcome", new object[] { cond.argsQueryOutcome });
                    if (outcome)
                    {
                        hasTrueContractCond = true;
                    }
                }
                else
                {
                    BasicMethods.assert(false, "condition type error");
                }
            }
            if (!hasContractCond || hasTrueContractCond)
            {
                PbEntity.TransferFunction transferFunction = _pay.transferFunc;
                PbEntity.TokenTransfer tokenTransfer = transferFunction.maxTransfer;
                PbEntity.AccountAmtPair accountAmtPair = tokenTransfer.receiver;
                return accountAmtPair.amt;
            }
            else
            {
                return 0;
            }
        }

        private static BigInteger _calculateNumericLogicPayment(PbEntity.ConditionalPay _pay, byte[][] _preimages, byte _funcType)
        {
            int j = 0;
            BigInteger amount = 0;
            bool hasContracCond = false;
            PbEntity.ConditionType ConditionType = PbEntity.getConditionType();
            PbEntity.TransferFunctionType TransferFunctionType = PbEntity.getTransferFunctionType();
            PbEntity.Condition[] conditions = _pay.conditions;
            for (var i = 0; i < conditions.Length; i++)
            {
                PbEntity.Condition cond = _pay.conditions[i];
                if (cond.conditionType == ConditionType.HASH_LOCK)
                {
                    BasicMethods.assert(SmartContract.Sha256(_preimages[j]) == cond.hashLock, "wrong preimage");
                    j++;
                }
                else if (
                   cond.conditionType == ConditionType.DEPLOYED_CONTRACT ||
                   cond.conditionType == ConditionType.VIRTUAL_CONTRACT
                   )
                {
                    byte[] numericCondHash = _getCondAddress(cond);
                    DynamicCallContract dyncall = (DynamicCallContract)numericCondHash.ToDelegate();
                    BasicMethods.assert((bool)dyncall("isFinalized", new object[] { cond.argsQueryFinalization }), "Condition is not finalized");

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
                        BasicMethods.assert(false, "error");
                    }
                    hasContracCond = true;
                }
                else
                {
                    BasicMethods.assert(false, "condition type error");
                }
            }
            PbEntity.TransferFunction transferFunction = _pay.transferFunc;
            PbEntity.TokenTransfer tokenTransfer = transferFunction.maxTransfer;
            PbEntity.AccountAmtPair accountAmtPair = tokenTransfer.receiver;
            if (hasContracCond)
            {
                BasicMethods.assert(amount <= accountAmtPair.amt, "exceed max transfer amount");
                return amount;
            }
            else
            {
                return accountAmtPair.amt;
            }
        }

        private static bool _isNumericLogic(byte _funcType)
        {
            PbEntity.TransferFunctionType TransferFunctionType = PbEntity.getTransferFunctionType();
            return _funcType == TransferFunctionType.NUMERIC_ADD || _funcType == TransferFunctionType.NUMERIC_MAX || _funcType == TransferFunctionType.NUMERIC_MIN;
        }

        private static byte[] _calculatePayId(byte[] _payHash, byte[] _setter)
        {
            return SmartContract.Sha256(_payHash.Concat(_setter));
        }

        private static byte[] _getCondAddress(PbEntity.Condition _cond)
        {
            PbEntity.ConditionType ConditionType = PbEntity.getConditionType();
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
            BasicMethods.assert(false, "conditiontype error");
            return new byte[] { 0x00 };
        }

        /*private static byte[] TokenTypes(BigInteger[] arr)
        {
            byte[] arrb = new byte[arr.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                BasicMethods.assert(arr[i] < 4, "token type error");
                arrb[i] = arr[i].AsByte();
            }
            return arrb;
        }*/

        private static BigInteger max(BigInteger a, BigInteger b)
        {
            BasicMethods.assert(a >= 0 && b >= 0, "value is less than zero");
            if (a >= b) return a;
            return b;
        }

        private static BigInteger min(BigInteger a, BigInteger b)
        {
            BasicMethods.assert(a >= 0 && b >= 0, "value is less than zero");
            if (a <= b) return a;
            return b;
        }
    }
}
