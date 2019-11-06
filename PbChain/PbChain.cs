using Neo.SmartContract.Framework;
using System;

public class PbChain
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

    public struct ResolvePayByConditionsRequest
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

    public struct Request
    {
        public byte[] channelInitializer;
        public byte[] sigs;
    }

    public static Request decodeRequest(byte[] raw)
    {
        Request request = new Request();
        int seek = 0;
        int len = 0;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        request.channelInitializer = raw.Range(seek, len);
        seek += len;

        len = (int)raw.Range(seek, 2).AsBigInteger();
        seek += 2;
        request.sigs = raw.Range(seek, len);
        seek += len;
        BasicMethods.assert(raw.Length == seek, "decodeRequest raw data illegal");
        return request;
    }
}
