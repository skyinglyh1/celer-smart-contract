using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace PbChain
{
    public class PbChain : SmartContract
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
            assert(raw.Length == seek, "decodeRequest raw data illegal");
            return request;

        }



    }
}
