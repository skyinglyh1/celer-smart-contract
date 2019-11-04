using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace CelerLedger
{
    public class CelerLedger : SmartContract
    {
        public struct ChannelStatus
        {
            public UInt16 Uninitialized;
            public UInt16 Operable;
            public UInt16 Settling;
            public UInt16 Closed;
            public UInt16 Migrated;
        }
        public ChannelStatus getChannelStatus()
        {
            ChannelStatus cs = new ChannelStatus();
            cs.Uninitialized = 0;
            cs.Operable = 1;
            cs.Settling = 2;
            cs.Closed = 3;
            cs.Migrated = 4;
            return cs;
        }

        public struct PeerState
        {
            public BigInteger seqNum;
            public BigInteger transferOut;
            public byte[] nextPayIdListHash;
            public BigInteger lastPayresolveDeadline;
            public BigInteger pendingPayout;
        }

        public struct PeerProfile
        {
            public byte[] peerAddr;
            public BigInteger deposit;
            public BigInteger withdrawal;
            public PeerState state;
        }

        public struct WithdrawIntent
        {
            public byte[] receiver;
            public BigInteger amount;
            public BigInteger requestTime;
            public byte[] recipientChannelId;
        }

        public struct TokenInfo
        {
            public UInt16 tokenType;
            public byte[] address;
        }

        public struct Channel
        {
            public BigInteger settleFinalizedTime;
            public BigInteger disputeTimeout;
            public TokenInfo token;
            public ChannelStatus status;
            public byte[] migratedTo;
            public PeerProfile[] peerProfiles;
            public BigInteger cooperativeWithdrawSeqNum;
            public WithdrawIntent withdrawIntent;
        }
        public struct Ledger
        {
            public Map<BigInteger, BigInteger> channelStatusNums;
            public byte[] ethPool;
            public byte[] payRegistry;
            public byte[] celerWallet;
            public Map<byte[], BigInteger> balanceLimits;
            public bool balanceLimitsEnabled;
            public Map<byte[], Channel> channelMap;
        }
    }
}
