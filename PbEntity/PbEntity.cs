﻿using Neo.SmartContract.Framework;
using System;
using System.Numerics;

public class PbEntity
{
    public struct TokenType
    {
        public byte INVALID;
        public byte NEO;
        public byte NEP5;
        public byte GAS;
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
        public byte BOOLEAN_AND;
        public byte BOOLEAN_OR;
        public byte BOOLEAN_CIRCUIT;
        public byte NUMERIC_ADD;
        public byte NUMERIC_MAX;
        public byte NUMERIC_MIN;
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

    public struct ConditionType
    {
        public byte HASH_LOCK;
        public byte DEPLOYED_CONTRACT;
        public byte VIRTUAL_CONTRACT;
    }
    public static ConditionType getConditionType()
    {
        ConditionType ct = new ConditionType();
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

    public struct Type2Len
    {
        public byte IntegeR;
        public byte BooleaN;
        public byte StrinG;
        public byte BytearraY;
        public byte ArraY;
        public byte MaP;
        public byte InterfacE;
    }
    public static Type2Len getTypeLen()
    {
        Type2Len t2l = new Type2Len();
        t2l.IntegeR = 1;
        t2l.BooleaN = 1;
        t2l.StrinG = 2;
        t2l.BytearraY = 2;
        t2l.ArraY = 2;
        t2l.MaP = 2;
        t2l.InterfacE = 1;
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
        BasicMethods.assert(BasicMethods._isLegalLength(len), "account length error");
        seek += 2;
        aap.account = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        aap.amt = raw.Range(seek, len).AsBigInteger();
        BasicMethods.assert(aap.amt >= 0, "amt error");
        seek += len;
        BasicMethods.assert(raw.Length == seek, "decAccountAmtPair raw data illegal");
        return aap;
    }

    public struct TokenInfo
    {
        public byte tokenType;
        public byte[] address;
    }
    public static TokenInfo decTokenInfo(byte[] raw)
    {
        TokenInfo ti = new TokenInfo();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(len == 1, "tokenType length illegal");
        ti.tokenType = raw.Range(seek, 1)[0];
        BasicMethods.assert(ti.tokenType <= getTokenType().GAS, "tokenType illegal");
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "address illegal");
        ti.address = raw.Range(seek, len);
        seek += len;
        BasicMethods.assert(raw.Length == seek, "decTokenInfo raw data illegal");
        return ti;
    }

    public struct TokenDistribution
    {
        public TokenInfo token;
        public AccountAmtPair[] distribution;
    }
    public static TokenDistribution decTokenDistribution(byte[] raw)
    {
        TokenDistribution td = new TokenDistribution();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        td.token = decTokenInfo(raw.Range(seek, len));
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        BasicMethods.assert(len >= 0, "distribution illegal");
        seek += 2;
        td.distribution = new AccountAmtPair[len];

        for (int i = 0; i < len; i++)
        {
            int k = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            td.distribution[i] = decAccountAmtPair(raw.Range(seek, k));
            seek += k;
        }
        BasicMethods.assert(raw.Length == seek, "decTokenDistribution raw data illegal");
        return td;
    }

    public struct TokenTransfer
    {
        public TokenInfo token;
        public AccountAmtPair receiver;
    }
    public static TokenTransfer decTokenTransfer(byte[] raw)
    {
        TokenTransfer tt = new TokenTransfer();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        tt.token = decTokenInfo(raw.Range(seek, len));
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        tt.receiver = decAccountAmtPair(raw.Range(seek, len));
        seek += len;
        BasicMethods.assert(raw.Length == seek, "decTokenInfo raw data illegal");
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
        BasicMethods.assert(len >= 0, "payIds illegal");
        seek += 2;
        pil.payIds = new byte[len][];
        for (int i = 0; i < len; i++)
        {
            int k = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            BasicMethods.assert(k == 32, "payIds " + i + " illegal");
            pil.payIds[i] = raw.Range(seek, k);
            seek += k;
        }

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(len == 32, "nextListHash illegal");
        pil.nextListHash = raw.Range(seek, len);
        seek += len;
        BasicMethods.assert(raw.Length == seek, "decPayIdList raw data illegal");
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
        BasicMethods.assert(len == 32, "channelId should be 32 bytes");
        spc.channelId = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "peerFrom illegal");
        spc.peerFrom = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        BasicMethods.assert(len >= 0, "seqNum illegal");
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
        BasicMethods.assert(len >= 0, "lastPayResolveDeadline illegal");
        seek += 2;
        spc.lastPayResolveDeadline = raw.Range(seek, len).AsBigInteger();
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        BasicMethods.assert(len >= 0, "totalPendingAmount illegal");
        seek += 2;
        spc.totalPendingAmount = raw.Range(seek, len).AsBigInteger();
        seek += len;

        BasicMethods.assert(raw.Length == seek, "decSimplexPaymentChannel raw data illegal");
        return spc;
    }

    public struct TransferFunction
    {
        public byte logicType;
        public TokenTransfer maxTransfer;
    }
    public static TransferFunction decTransferFunction(byte[] raw)
    {
        TransferFunction tf = new TransferFunction();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(len == 1, "tokenType length illegal");
        tf.logicType = raw.Range(seek, 1)[0];
        BasicMethods.assert(tf.logicType <= getTransferFunctionType().NUMERIC_MIN, "logicType illegal");
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        tf.maxTransfer = decTokenTransfer(raw.Range(seek, len));
        seek += len;
        BasicMethods.assert(raw.Length == seek, "decTransferFunction raw data illegal");
        return tf;
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
    public static Condition decCondition(byte[] raw)
    {
        Condition c = new Condition();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(len == 1, "tokenType length illegal");
        c.conditionType = raw.Range(seek, 1)[0];
        BasicMethods.assert(c.conditionType <= getConditionType().VIRTUAL_CONTRACT, "conditionType illegal");
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(len == 32, "hashLock illegal");
        c.hashLock = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "deployedContractAddress illegal");
        c.deployedContractAddress = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(len == 32, "virtualContractAddress illegal");
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

        BasicMethods.assert(raw.Length == seek, "decCondition raw data illegal");
        return c;
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
    public static ConditionalPay decConditionalPay(byte[] raw)
    {
        ConditionalPay cp = new ConditionalPay();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        cp.payTimestamp = raw.Range(seek, len).AsBigInteger();
        BasicMethods.assert(cp.payTimestamp > 0, "payTimestamp illegal");
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "src illegal");
        cp.src = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "dest illegal");
        cp.dest = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        BasicMethods.assert(len >= 0, "conditions illegal");
        seek += 2;
        cp.conditions = new Condition[len];

        for (int i = 0; i < len; i++)
        {
            int k = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            cp.conditions[i] = decCondition(raw.Range(seek, k));
            seek += k;
        }

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
        BasicMethods.assert(BasicMethods._isLegalLength(len), "payResolver illegal");
        cp.payResolver = raw.Range(seek, len);
        seek += len;

        BasicMethods.assert(raw.Length == seek, "decConditionalPay raw data illegal");
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

        BasicMethods.assert(raw.Length == seek, "decConditionalPay raw data illegal");
        return cpr;
    }

    public struct VouchedCondPayResult
    {
        public byte[] condPayResult;
        public byte[] sigOfSrc;
        public byte[] sigOfDest;
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
        vcpr.sigOfDest = raw.Range(seek, len);
        seek += len;

        BasicMethods.assert(raw.Length == seek, "decConditionalPay raw data illegal");
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
        BasicMethods.assert(len == 32, "channelId illegal");
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
        BasicMethods.assert(len == 32, "recipientChannelId illegal");
        cwi.recipientChannelId = raw.Range(seek, len);
        seek += len;

        BasicMethods.assert(raw.Length == seek, "decConditionalPay raw data illegal");
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

        BasicMethods.assert(raw.Length == seek, "decPaymentChannelInitializer raw data illegal");
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
        BasicMethods.assert(len == 32, "channelId illegal");
        csi.channelId = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        csi.seqNum = raw.Range(seek, len).AsBigInteger();
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        BasicMethods.assert(len >= 0, "settleBalance illegal");
        seek += 2;
        csi.settleBalance = new AccountAmtPair[len];

        for (int i = 0; i < len; i++)
        {
            int k = (int)raw.Range(seek, 2).AsBigInteger();
            seek += 2;
            csi.settleBalance[i] = decAccountAmtPair(raw.Range(seek, k));
            seek += k;
        }

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        csi.settleDeadline = raw.Range(seek, len).AsBigInteger();
        seek += len;

        BasicMethods.assert(raw.Length == seek, "decCooperativeSettleInfo raw data illegal");
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
        BasicMethods.assert(len == 32, "channelId illegal");
        cmi.channelId = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "fromLedgerAddress illegal");
        cmi.fromLedgerAddress = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        BasicMethods.assert(BasicMethods._isLegalLength(len), "toLegerAddress illegal");
        cmi.toLegerAddress = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        cmi.migrationDeadline = raw.Range(seek, len).AsBigInteger();
        seek += len;

        BasicMethods.assert(raw.Length == seek, "decPaymentChannelInitializer raw data illegal");
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
