using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;
using System.ComponentModel;

namespace PbEntity
{
    public class PbEntity : SmartContract
    {
        public static bool assert(bool condition, string msg)
        {
            if (condition)
            {
                return true;
            }
            else
            {
                throw new Exception((msg.HexToBytes().Concat(" not support".HexToBytes())).AsString());
            }
        }
        public struct TokenType
        {
            public UInt16 INVALID;
            public UInt16 NEO;
            public UInt16 NEP5;
            public UInt16 GAS;
        }
        public static TokenType getTokenType()
        {
            TokenType tt = new TokenType();
            tt.INVALID = 0;
            tt.NEO = 1;
            tt.NEP5 = 2;
            tt.GAS = 3;
            return tt;
        }

        public struct TransferFunctionType
        {
            public UInt16 BOOLEAN_AND;
            public UInt16 BOOLEAN_OR;
            public UInt16 BOOLEAN_CIRCUIT;
            public UInt16 NUMERIC_ADD;
            public UInt16 NUMERIC_MAX;
            public UInt16 NUMERIC_MIN;
        }
        public static TransferFunctionType getTransferFunctionType()
        {
            TransferFunctionType tft = new TransferFunctionType();
            tft.BOOLEAN_AND = 0;
            tft.BOOLEAN_OR = 1;
            tft.BOOLEAN_CIRCUIT = 2;
            tft.NUMERIC_ADD = 3;
            tft.NUMERIC_MAX = 4;
            tft.NUMERIC_MIN = 5;
            return tft;
        }

        public struct ConditionTypes
        {
            public UInt16 HASH_LOCK;
            public UInt16 DEPLOYED_CONTRACT;
            public UInt16 VIRTUAL_CONTRACT;
        }
        public static ConditionTypes getConditionTypes()
        {
            ConditionTypes ct = new ConditionTypes();
            ct.HASH_LOCK = 0;
            ct.DEPLOYED_CONTRACT = 1;
            ct.VIRTUAL_CONTRACT = 2;
            return ct;
        }



        // Length is 2 byte means the maximum length of each type raw data is 2^16 = 1 MB
        public struct Type2Mark
        {
            public byte[] IntegeR;
            public byte[] BooleaN;
            public byte[] StrinG;
            public byte[] BytearraY;
            public byte[] ArraY;
            public byte[] MaP;
            public byte[] InterfacE;
        }
        public struct Type2Len
        {
            public int IntegeR;
            public int BooleaN;
            public int StrinG;
            public int BytearraY;
            public int ArraY;
            public int MaP;
            public int InterfacE;
        }
        public static Type2Mark getTypeMark()
        {
            Type2Mark t2m = new Type2Mark();
            t2m.IntegeR = new byte[] { 0x01 };
            t2m.BooleaN = new byte[] { 0x02 };
            t2m.StrinG = new byte[] { 0x03 };
            t2m.BytearraY = new byte[] { 0x04 };
            t2m.ArraY = new byte[] { 0x04 };
            t2m.MaP = new byte[] { 0x04 };
            t2m.InterfacE = new byte[] { 0x04 };
            return t2m;
        }
        public static Type2Len getTypeLen()
        {
            Type2Len t2l = new Type2Len();
            t2l.IntegeR = (int)1;
            t2l.BooleaN = (int)1;
            t2l.StrinG = (int)2;
            t2l.BytearraY = (int)2;
            t2l.ArraY = (int)2;
            t2l.MaP = (int)2;
            t2l.InterfacE = (int)1;
            return t2l;
        }

        //public static UInt16[] TokenTypes(UInt16[] arr)
        //{
        //    var t = new UInt16[arr.Length];
        //    for (var i = 0; i < t.Length; i++)
        //    {
        //        //t[i] = TokenType[arr[i]];
        //    }
        //    return t;
        //}

        //[DisplayName("init")]
        //public static bool init ()
        //{
        //    TTS tokenTypeStruct = new TTS();
        //    tokenTypeStruct.INVALID = 0;
        //    tokenTypeStruct.NEO = 1;
        //    tokenTypeStruct.NEP5 = 2;
        //    tokenTypeStruct.GAS = 3;
        //    Storage.Put(Storage.CurrentContext, "TokenType", Helper.Serialize(tokenTypeStruct));

        //    return true;
        //}



        public struct AccountAmtPair
        {
            public byte[] account;
            public BigInteger amt;
        }
        public static AccountAmtPair decAccountAmtPair(byte[] raw)
        {
            AccountAmtPair aap = new AccountAmtPair();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            assert(len == 20, "account length error");
            seek += 2;
            aap.account = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            aap.amt = raw.Range(seek, len).AsBigInteger();
            seek += len;
            assert(raw.Length == seek, "decAccountAmtPair raw data illegal");
            return aap;
        }

        public struct TokenInfo
        {
            public UInt16 tokenType;
            public byte[] address;
        }
        public static TokenInfo decTokenInfo(byte[] raw)
        {
            TokenInfo ti = new TokenInfo();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            ti.tokenType = (UInt16)(raw.Range(seek, len).AsBigInteger());
            assert(ti.tokenType <= getTokenType().GAS, "tokenType illegal");
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            assert(len == 20, "address illegal");
            ti.address = raw.Range(seek, len);
            seek += len;
            assert(raw.Length == seek, "decTokenInfo raw data illegal");
            return ti;
        }

        public struct TokenDistribution
        {
            public UInt16 token;
            public AccountAmtPair[] distribution;
        }
        public static TokenDistribution decTokenDistribution(byte[] raw)
        {
            TokenDistribution td = new TokenDistribution();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            td.token = (UInt16)(raw.Range(seek, len).AsBigInteger());
            assert(td.token <= getTokenType().GAS, "tokenType illegal");
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            while (seek < raw.Length)
            {
                int disIndex = 0;
                AccountAmtPair aap = new AccountAmtPair();
                len = (int)raw.Range(seek, 2).AsBigInteger();
                assert(len == 20, "account length error");
                seek += 2;
                aap.account = raw.Range(seek, len);
                seek += len;

                len = (int)raw.Range(seek, 2).AsBigInteger();
                seek += 2;
                aap.amt = raw.Range(seek, len).AsBigInteger();
                seek += len;

                td.distribution[disIndex] = aap;
                disIndex += 1;
            }
            assert(raw.Length == seek, "decTokenDistribution raw data illegal");
            return td;
        }


        public struct TokenTransfer
        {
            public UInt16 token;
            public AccountAmtPair receiver;
        }
        public static TokenTransfer decTokenTransfer(byte[] raw)
        {
            TokenTransfer tt = new TokenTransfer();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            tt.token = (UInt16)(raw.Range(seek, len).AsBigInteger());
            assert(tt.token <= getTokenType().GAS, "tokenType illegal");
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            tt.receiver = decAccountAmtPair(raw.Range(seek, len));
            seek += len;
            assert(raw.Length == seek, "decTokenInfo raw data illegal");
            return tt;
        }

        public struct PayIdList
        {
            public byte[][] payIds;
            public byte[] nextListHash;
        }
        public static PayIdList decPayIdList(byte[] raw)
        {
            PayIdList pil = new PayIdList();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            pil.payIds = new byte[][] { };
            //TODO payId物理意义是什么？
            int s = 0;
            int i = 0;
            while (s < len)
            {
                pil.payIds[i] = raw.Range(seek + s, 32);
                s += 32;
                i = i + 1;
            }
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            assert(len == 20, "nextListHash illegal");
            pil.nextListHash = raw.Range(seek + s, 32);
            seek += len;
            assert(raw.Length == seek, "decPayIdList raw data illegal");
            return pil;
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
        public static SimplexPaymentChannel decSimplexPaymentChannel(byte[] raw)
        {
            SimplexPaymentChannel spc = new SimplexPaymentChannel();
            int seek = 0;
            int len = 0;

            len = (int)(raw.Range(seek, 2).AsBigInteger());
            seek += 2;
            assert(len == 32, "channelId should be 32 bytes");
            spc.channelId = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            assert(len == 20, "peerFrom illegal");
            spc.peerFrom = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            spc.seqNum = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            spc.transferToPeer = decTokenTransfer(raw.Range(seek, len));
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            spc.pendingPayIds = decPayIdList(raw.Range(seek, len));
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            spc.lastPayResolveDeadline = (BigInteger)raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            spc.totalPendingAmount = (BigInteger)raw.Range(seek, len).AsBigInteger();
            seek += len;

            assert(raw.Length == seek, "decSimplexPaymentChannel raw data illegal");
            return spc;
        }

        public struct TransferFunction
        {
            public UInt16 logicType;
            public TokenTransfer maxTransfer;
        }
        public static TransferFunction decTransferFunction(byte[] raw)
        {
            TransferFunction tf = new TransferFunction();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            tf.logicType = (UInt16)(raw.Range(seek, len).AsBigInteger());
            assert(tf.logicType <= getTransferFunctionType().NUMERIC_MIN, "logicType illegal");
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            tf.maxTransfer = decTokenTransfer(raw.Range(seek, len));
            seek += len;
            assert(raw.Length == seek, "decTransferFunction raw data illegal");
            return tf;
        }

        public struct Condition
        {
            public UInt16 conditionType;
            public byte[] hashLock;
            public byte[] deployedContractAddress;
            public byte[] virtualContractAddress;
            public byte[] argsQueryFinalization;
            public byte[] argsQueryOutcome;
        }
        public static Condition decCondition(byte[] raw)
        {
            Condition c = new Condition();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            c.conditionType = (UInt16)(raw.Range(seek, len).AsBigInteger());
            assert(c.conditionType <= getConditionTypes().VIRTUAL_CONTRACT, "conditionType illegal");
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            c.hashLock = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            c.deployedContractAddress = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            c.virtualContractAddress = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            c.argsQueryFinalization = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            c.argsQueryOutcome = raw.Range(seek, len);
            seek += len;

            assert(raw.Length == seek, "decCondition raw data illegal");
            return c;
        }




        public struct ConditionalPay
        {
            public BigInteger payTimestamp;
            public byte[] src;
            public byte[] dest;
            public ConditionalPay[] conditions;
            public TransferFunction transferFunc;
            public BigInteger resolveDeadline;
            public BigInteger resolveTimeout;
            public byte[] payResolver;
        }
        public static ConditionalPay decConditionalPay(byte[] raw)
        {
            ConditionalPay cp = new ConditionalPay();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.payTimestamp = raw.Range(seek, len).AsBigInteger();
            assert(cp.payTimestamp > 0, "payTimestamp illegal");
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.src = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.dest = raw.Range(seek, len);
            seek += len;

            int s = seek;
            int i = 0;
            int l = 0;
            while (s < len)
            {
                l = (int)raw.Range(seek, 2).AsBigInteger();
                s += 2;
                cp.conditions[i] = decConditionalPay(raw.Range(s, l));
                s += l;
                i = i + 1;
            }
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.transferFunc = decTransferFunction(raw.Range(seek, len));
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.resolveDeadline = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.resolveTimeout = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.payResolver = raw.Range(seek, len);
            seek += len;

            assert(raw.Length == seek, "decConditionalPay raw data illegal");
            return cp;
        }

        public struct CondPayResult
        {
            public byte[] condPay;
            public BigInteger amount;
        }
        public static CondPayResult decCondPayResult(byte[] raw)
        {
            CondPayResult cpr = new CondPayResult();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cpr.condPay = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cpr.amount = raw.Range(seek, len).AsBigInteger();
            seek += len;

            assert(raw.Length == seek, "decConditionalPay raw data illegal");
            return cpr;
        }

        public struct VouchedCondPayResult
        {
            public byte[] condPayResult;
            public byte[] sigOfSrc;
            public byte[] sigOfDes;
        }
        public static VouchedCondPayResult decVouchedCondPayResult(byte[] raw)
        {
            VouchedCondPayResult vcpr = new VouchedCondPayResult();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            vcpr.condPayResult = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            vcpr.sigOfSrc = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            vcpr.sigOfDes = raw.Range(seek, len);
            seek += len;

            assert(raw.Length == seek, "decConditionalPay raw data illegal");
            return vcpr;
        }

        public struct CooperativeWithdrawInfo
        {
            public byte[] channelId;
            public BigInteger seqNum;
            public AccountAmtPair withdraw;
            public BigInteger withdrawDeadline;
            public byte[] recipientChannelId;
        }
        public static CooperativeWithdrawInfo decCooperativeWithdrawInfo(byte[] raw)
        {
            CooperativeWithdrawInfo cwi = new CooperativeWithdrawInfo();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cwi.channelId = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cwi.seqNum = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cwi.withdraw = decAccountAmtPair(raw.Range(seek, len));
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cwi.withdrawDeadline = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cwi.recipientChannelId = raw.Range(seek, len);
            seek += len;

            assert(raw.Length == seek, "decConditionalPay raw data illegal");
            return cwi;
        }

        public struct PaymentChannelInitializer
        {
            public TokenDistribution initDistribution;
            public BigInteger openDeadline;
            public BigInteger disputeTimeout;
            public BigInteger msgValueReceiver;
        }
        public static PaymentChannelInitializer decPaymentChannelInitializer(byte[] raw)
        {
            PaymentChannelInitializer pci = new PaymentChannelInitializer();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            pci.initDistribution = decTokenDistribution(raw.Range(seek, len));
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            pci.openDeadline = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            pci.disputeTimeout = raw.Range(seek, len).AsBigInteger();
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            pci.msgValueReceiver = raw.Range(seek, len).AsBigInteger();
            seek += len;

            assert(raw.Length == seek, "decPaymentChannelInitializer raw data illegal");
            return pci;
        }

        public struct CooperativeSettleInfo
        {
            public byte[] channelId;
            public BigInteger seqNum;
            public AccountAmtPair[] settleBalance;
            public BigInteger settleDeadline;
        }
        public static CooperativeSettleInfo decCooperativeSettleInfo(byte[] raw)
        {
            CooperativeSettleInfo csi = new CooperativeSettleInfo();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            csi.channelId = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            csi.seqNum = raw.Range(seek, len).AsBigInteger();
            seek += len;

            int s = seek;
            int i = 0;
            int l = 0;
            while (s < len)
            {
                l = (int)raw.Range(seek, 2).AsBigInteger();
                s += 2;
                csi.settleBalance[i] = decAccountAmtPair(raw.Range(s, l));
                s += l;
                i = i + 1;
            }
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            csi.settleDeadline = raw.Range(seek, len).AsBigInteger();
            seek += len;

            assert(raw.Length == seek, "decCooperativeSettleInfo raw data illegal");
            return csi;
        }

        public struct ChannelMigrationInfo
        {
            public byte[] channelId;
            public byte[] fromLedgerAddress;
            public byte[] toLegerAddress;
            public BigInteger migrationDeadline;
        }

        public static ChannelMigrationInfo decChannelMigrationInfo(byte[] raw)
        {
            ChannelMigrationInfo cmi = new ChannelMigrationInfo();
            int seek = 0;
            int len = 0;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cmi.channelId = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cmi.fromLedgerAddress = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cmi.toLegerAddress = raw.Range(seek, len);
            seek += len;

            len = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cmi.migrationDeadline = raw.Range(seek, len).AsBigInteger();
            seek += len;

            assert(raw.Length == seek, "decPaymentChannelInitializer raw data illegal");
            return cmi;
        }







        //public static object Main(string operation, object[] args)
        //{
        //    if (Runtime.Trigger == TriggerType.Verification)//取钱才会涉及这里
        //    {
        //        return false;
        //    }
        //    else if (Runtime.Trigger == TriggerType.VerificationR)
        //    {
        //        return false;
        //    }
        //    else if (Runtime.Trigger == TriggerType.Application)
        //    {
        //        var raw = (byte[])args[0];
        //        ////随便调用
        //        //if (operation == "TokenTypes")

        //        //    return TokenTypes(raw);
        //        ////请求者调用
        //        //if (operation == "setResolverData")
        //        //    return setResolverData((byte[])args[0], (byte[])args[1], (string)args[2], (string)args[3], (byte[])args[4]);
        //    }
        //    return false;
        //}
    }
}
