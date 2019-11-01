using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using System.ComponentModel;
using Helper = Neo.SmartContract.Framework.Helper;
using Neo.SmartContract.Framework.Services.System;

namespace CelerWallet
{
    public class CelerWallet : SmartContract
    {
        //private static bool _paused;
        public static byte[] PausedKey = "paused".AsByteArray();

        //public static Map<byte[], bool> _pausers;
        public static byte[] PauserKey = "pausers".AsByteArray();

        public static readonly byte[] AddressZero = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

        public class Wallet
        {
            public byte[][] owners;
            public byte[] theOperator;
            //public Map<byte[], BigInteger> balances;
            public byte[] proposedNewOperator;
            //public Map<byte[], bool> proposalVotes;
        }

        public static byte[] WalletNum = "walletNum".AsByteArray();
        //public static Map<byte[], Wallet> wallets;
        public static byte[] WalletsPrefix = "wallets".AsByteArray();
        public static byte[] WalletsBalancesPrefix = "balances".AsByteArray();
        public static byte[] WalletsProposalVotesPrefix = "proposalVotes".AsByteArray();

        public delegate object NEP5Contract(string method, object[] args);

        [DisplayName("pause")]
        public static event Action<byte[]> Paused;
        [DisplayName("unpause")]
        public static event Action<byte[]> UnPaused;
        [DisplayName("addPauser")]
        public static event Action<byte[]> PauserAdded;
        [DisplayName("removePauser")]
        public static event Action<byte[]> PauserRemoveded;
        [DisplayName("create")]
        public static event Action<byte[], byte[][], byte[]> CreateWallet;
        [DisplayName("deposit")]
        public static event Action<byte[], byte[], BigInteger> DepositToWallet;
        [DisplayName("withdraw")]
        public static event Action<byte[], byte[], byte[], BigInteger> WithdrawFromWallet;
        [DisplayName("transferToWallet")]
        public static event Action<byte[], byte[], byte[], byte[], BigInteger> TransferToWallet;
        [DisplayName("changeOperator")]
        public static event Action<byte[], byte[], byte[]> ChangeOperator;
        [DisplayName("proposeNewOperator")]
        public static event Action<byte[], byte[], byte[]> ProposeNewOperator;
        [DisplayName("drainToken")]
        public static event Action<byte[], byte[], BigInteger> DrainToken;

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)//取钱才会涉及这里
            {
                return false;
            }
            else if (Runtime.Trigger == TriggerType.VerificationR)
            {
                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "unpause")
                {
                    assert(args.Length == 1, "params length error");
                    byte[] invoker = (byte[])args[0];
                    return unpause(invoker);
                }
                if (method == "addPauser")
                {
                    assert(args.Length == 2, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] account = (byte[])args[1];
                    return addPauser(invoker, account);
                }
                if (method == "removePauser")
                {
                    assert(args.Length == 2, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] account = (byte[])args[1];
                    return removePauser(invoker, account);
                }
                if (method == "create")
                {
                    assert(args.Length == 4, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[][] owners = (byte[][])args[1];
                    byte[] theOperator = (byte[])args[2];
                    byte[] nonce = (byte[])args[3];
                    return create(invoker, owners, theOperator, nonce);
                }
                if (method == "depositNEP5")
                {
                    assert(args.Length == 4, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] walletId = (byte[])args[1];
                    byte[] tokenAddress = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return depositNEP5(invoker, walletId, tokenAddress, amount);
                }
                if (method == "withdraw")
                {
                    assert(args.Length == 4, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] tokenAddress = (byte[])args[1];
                    byte[] receiver = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return withdraw(invoker, tokenAddress, receiver, amount);
                }
                if (method == "transferToWallet")
                {
                    assert(args.Length == 5, "params length error");
                    byte[] fromWalletId = (byte[])args[0];
                    byte[] toWalletId = (byte[])args[1];
                    byte[] tokenAddress = (byte[])args[2];
                    byte[] receiver = (byte[])args[3];
                    BigInteger amount = (BigInteger)args[4];
                    return transferToWallet(fromWalletId, toWalletId, tokenAddress, receiver, amount);
                }
                if (method == "transferOperatorship")
                {
                    assert(args.Length == 2, "params length error");
                    byte[] walletId = (byte[])args[0];
                    byte[] newOperator = (byte[])args[1];
                    return transferOperatorship(walletId, newOperator);
                }
                if (method == "proposeNewOperator")
                {
                    assert(args.Length == 3, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] walletId = (byte[])args[1];
                    byte[] newOperator = (byte[])args[2];
                    return proposeNewOperator(invoker, walletId, newOperator);
                }
                if (method == "drainToken")
                {
                    assert(args.Length == 3, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] tokenAddress = (byte[])args[1];
                    byte[] receiver = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return drainToken(invoker, tokenAddress, receiver, amount);
                }



                if (method == "getWalletOwners")
                {
                    assert(args.Length == 1, "params length error");
                    byte[] walletId = (byte[])args[0];
                    return getWalletOwners(walletId);
                }
                if (method == "getOperator")
                {
                    assert(args.Length == 1, "params length error");
                    byte[] walletId = (byte[])args[0];
                    return getOperator(walletId);
                }
                if (method == "getBalance")
                {
                    assert(args.Length == 2, "params length error");
                    byte[] walletId = (byte[])args[0];
                    byte[] tokenAddress = (byte[])args[1];
                    return getBalance(walletId, tokenAddress);
                }
                if (method == "getProposalVote")
                {
                    assert(args.Length == 2, "params length error");
                    byte[] walletId = (byte[])args[0];
                    byte[] owner = (byte[])args[1];
                    return getProposalVote(walletId, owner);
                }


            }
            return false;
        }


        public static void init()
        {

        }
        public static Wallet getWallet(byte[] walletId)
        {
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            if (walletBs.Length == 0)
                return new Wallet();
            return Helper.Deserialize(walletBs) as Wallet;
        }


        public static void _onlyOperator(byte[] _walletId)
        {
            assert(Runtime.CheckWitness(getWallet(_walletId).theOperator), "operator checkwitness failed");
            //byte[] theOperator = getWallet(_walletId).theOperator;
            //assert(_invoker == theOperator, "msg.sender is not operator");
        }

        public static void _onlyWalletOwner(byte[] _walletId, byte[] _addr)
        {
            assert(_isWalletOwner(_walletId, _addr), "Given address is not wallet owner");
        }

        [DisplayName("create")]
        public static byte[] create(byte[] invoker, byte[][] owners, byte[] theOperator, byte[] nonce)
        {
            assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            assert(_isLegalAddresses(owners), "owners addresses are not byte20");
            assert(_isLegalAddress(theOperator), "the operator is not byte20");
            //TODO: no need to check the nonce byte[] length

            _whenNotPaused();

            assert(theOperator != AddressZero, "New operator is address zero");
            byte[] SelfContractHash = ExecutionEngine.ExecutingScriptHash;
            byte[] concatRes = SelfContractHash.Concat(invoker).Concat(nonce);
            byte[] walletId = SmartContract.Sha256(concatRes);
            assert(getWallet(walletId).theOperator == null, "Occupied wallet id");
            Wallet w = new Wallet();
            assert(_isLegalAddresses(owners), "owners contains illegal address");
            w.owners = owners;
            w.theOperator = theOperator;
            BigInteger walletNum = Storage.Get(Storage.CurrentContext, WalletNum).AsBigInteger();
            Storage.Put(Storage.CurrentContext, WalletNum, (walletNum + 1).AsByteArray());
            Storage.Put(Storage.CurrentContext, WalletsPrefix.Concat(walletId), Helper.Serialize(w));
            CreateWallet(walletId, owners, theOperator);
            return walletId;
        }

        [DisplayName("depositNEP5")]
        public static object depositNEP5(byte[] invoker, byte[] walletId, byte[] tokenAddress, BigInteger amount)
        {
            assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            assert(_isByte32(walletId), "walletId is not byte32");
            assert(_isLegalAddress(tokenAddress), "tokenAddress is not byte20");
            assert(amount >= 0, "amount is less than zero");

            _whenNotPaused();

            assert(_updateBalance(walletId, tokenAddress, amount, "add"), "updateBalance failed");
            NEP5Contract dyncall = (NEP5Contract)tokenAddress.ToDelegate();
            Object[] args = new object[] { invoker, ExecutionEngine.ExecutingScriptHash, amount };
            bool res = (bool)dyncall("transfer", args);
            assert(res, "transfer NEP5 tokens failed");

            DepositToWallet(walletId, tokenAddress, amount);
            return true;
        }

        [DisplayName("withdraw")]
        public static object withdraw(byte[] walletId, byte[] tokenAddress, byte[] receiver, BigInteger amount)
        {
            assert(_isByte32(walletId), "walletId illegal, not byte32");
            assert(_isLegalAddress(tokenAddress), "tokenAddress is illegal");
            assert(_isLegalAddress(receiver), "receiver address is illegal");
            assert(amount > 0, "amount is less than zero");
            //assert(Runtime.CheckWitness(receiver), "CheckWitness failed");

            _whenNotPaused();
            _onlyOperator(walletId);
            _onlyWalletOwner(walletId, receiver);

            assert(_updateBalance(walletId, tokenAddress, amount, "sub"), "updateBalance failed");
            assert(_withdrawToken(tokenAddress, receiver, amount), "withdrawToken failed");
            WithdrawFromWallet(walletId, tokenAddress, receiver, amount);
            return true;
        }

        [DisplayName("transferToWallet")]
        public static object transferToWallet(byte[] fromWalletId, byte[] toWalletId, byte[] tokenAddress, byte[] receiver, BigInteger amount)
        {
            assert(_isByte32(fromWalletId), "fromWalletId illegal, not byte32");
            assert(_isByte32(toWalletId), "toWalletId illegal, not byte32");
            assert(_isLegalAddress(tokenAddress), "tokenAddress is illegal");
            assert(_isLegalAddress(receiver), "receiver address is illegal");
            assert(amount > 0, "amount is less than zero");
            //assert(Runtime.CheckWitness(receiver), "CheckWitness failed");

            _whenNotPaused();
            _onlyOperator(fromWalletId);
            _onlyWalletOwner(fromWalletId, receiver);
            _onlyWalletOwner(toWalletId, receiver);

            assert(_updateBalance(fromWalletId, tokenAddress, amount, "sub"), "sub balance in from wallet failed");
            assert(_updateBalance(toWalletId, tokenAddress, amount, "add"), "add balance in to wallet failed");

            TransferToWallet(fromWalletId, toWalletId, tokenAddress, receiver, amount);
            return true;
        }

        [DisplayName("transferOperatorship")]
        public static object transferOperatorship(byte[] walletId, byte[] newOperator)
        {
            assert(_isByte32(walletId), "walletId illegal, not byte32");
            assert(_isLegalAddress(newOperator), "newOperator address is illegal");
            // no need to checkwitness since _onlyOperator has already done it

            _whenNotPaused();
            _onlyOperator(walletId);
            _changeOperator(walletId, newOperator);
            return true;
        }

        [DisplayName("proposeNewOperator")]
        public static object proposeNewOperator(byte[] invoker, byte[] walletId, byte[] newOperator)
        {
            assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            assert(_isByte32(walletId), "walletId illegal");
            assert(_isLegalAddress(newOperator), "new operator is not address");

            _onlyWalletOwner(walletId, invoker);

            Wallet w = getWallet(walletId);
            // wpvBs means Wallet Proposal Votes ByteS
            byte[] wpvBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(WalletsProposalVotesPrefix).Concat(walletId));
            Map<byte[], bool> wpv = new Map<byte[], bool>();
            if (wpvBs.Length > 0)
            {
                wpv = Helper.Deserialize(wpvBs) as Map<byte[], bool>;
            }

            if (newOperator != w.proposedNewOperator)
            {
                wpv = _clearVotes(w, wpv);
                w.proposedNewOperator = newOperator;
            }
            wpv[invoker] = true;

            if (_checkAllVotes(w, wpv))
            {
                _changeOperator(walletId, newOperator);
                wpv = _clearVotes(w, wpv);
            }

            Storage.Put(Storage.CurrentContext, WalletsPrefix.Concat(walletId), Helper.Serialize(w));
            Storage.Put(Storage.CurrentContext, WalletsPrefix.Concat(WalletsProposalVotesPrefix).Concat(walletId), Helper.Serialize(wpv));

            ProposeNewOperator(walletId, newOperator, invoker);
            return true;
        }

        [DisplayName("drainToken")]
        public static object drainToken(byte[] invoker, byte[] tokenAddress, byte[] receiver, BigInteger amount)
        {
            assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            assert(_isLegalAddress(tokenAddress), "tokenAddress is illegal");
            assert(_isLegalAddress(receiver), "receiver address is illegal");
            assert(amount >= 0, "amount is less than zero");

            _whenPaused();
            _onlyPauser(invoker);

            assert(_withdrawToken(tokenAddress, receiver, amount), "withdrawToken failed");

            DrainToken(tokenAddress, receiver, amount);
            return true;
        }

        [DisplayName("getWalletOwners")]
        public static byte[][] getWalletOwners(byte[] walletId)
        {
            assert(_isByte32(walletId), "walletId illegal");
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            Wallet w = new Wallet();
            if (walletBs.Length > 0)
                w = Helper.Deserialize(walletBs) as Wallet;
            return w.owners;
        }

        [DisplayName("getOperator")]
        public static byte[] getOperator(byte[] walletId)
        {
            assert(_isByte32(walletId), "walletId illegal");
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            Wallet w = new Wallet();
            if (walletBs.Length > 0)
                w = Helper.Deserialize(walletBs) as Wallet;
            return w.theOperator;
        }

        [DisplayName("getBalance")]
        public static BigInteger getBalance(byte[] walletId, byte[] tokenAddress)
        {
            assert(_isByte32(walletId), "walletId illegal");
            assert(_isLegalAddress(tokenAddress), "tokenAddress is illegal");
            byte[] wBalanceBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(WalletsBalancesPrefix).Concat(walletId));
            Map<byte[], BigInteger> wBalanceMap = Helper.Deserialize(wBalanceBs) as Map<byte[], BigInteger>;
            if (wBalanceMap.HasKey(tokenAddress))
            {
                return wBalanceMap[tokenAddress];
            }
            return 0;
        }

        [DisplayName("getProposedNewOperator")]
        public static byte[] getProposedNewOperator(byte[] walletId)
        {
            assert(_isByte32(walletId), "walletId illegal");
            Wallet w = new Wallet();
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            if (walletBs.Length > 0)
                w = Helper.Deserialize(walletBs) as Wallet;
            return w.theOperator;
        }

        [DisplayName("getProposalVote")]
        public static bool getProposalVote(byte[] walletId, byte[] owner)
        {
            assert(_isByte32(walletId), "walletId illegal");
            assert(_isLegalAddress(owner), "owner address is not length of 20 bytes");
            _onlyWalletOwner(walletId, owner);

            Wallet w = getWallet(walletId);
            // wpvBs means Wallet Proposal Votes ByteS
            byte[] wpvBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(WalletsProposalVotesPrefix).Concat(walletId));
            Map<byte[], bool> wpv = new Map<byte[], bool>();
            if (wpvBs.Length > 0)
            {
                wpv = Helper.Deserialize(wpvBs) as Map<byte[], bool>;
                return wpv[owner];
            }
            return false;
        }

        private static bool _checkAllVotes(Wallet _w, Map<byte[], bool> _wpv)
        {
            for (var i = 0; i < _w.owners.Length; i++)
            {
                if (_wpv[_w.owners[i]] == false)
                    return false;
            }
            return true;
        }

        private static Map<byte[], bool> _clearVotes(Wallet _w, Map<byte[], bool> _wpv)
        {
            for (var i = 0; i < _w.owners.Length; i++)
            {
                _wpv[_w.owners[i]] = false;
            }

            return _wpv;
        }

        private static void _changeOperator(byte[] _walletId, byte[] _newOperator)
        {
            assert(_isLegalAddress(_newOperator), "new operator is illegal");
            Wallet w = getWallet(_walletId);
            byte[] oldOperator = w.theOperator;
            assert(_isLegalAddress(oldOperator), "old operator is not legal");

            w.theOperator = _newOperator;

            Storage.Put(Storage.CurrentContext, WalletsPrefix.Concat(_walletId), Helper.Serialize(w));
            ChangeOperator(_walletId, oldOperator, _newOperator);
        }

        private static bool _withdrawToken(byte[] _tokenAddress, byte[] _receiver, BigInteger _amount)
        {
            NEP5Contract dyncall = (NEP5Contract)_tokenAddress.ToDelegate();
            Object[] args = new object[] { ExecutionEngine.ExecutingScriptHash, _receiver, _amount };
            bool res = (bool)dyncall("transfer", args);
            return res;
        }

        private static bool _updateBalance(byte[] _walletId, byte[] _tokenAddress, BigInteger _amount, string _mathOperation)
        {

            assert(_amount >= 0, "amount is less than zero");

            Wallet w = getWallet(_walletId);
            assert(_isLegalAddress(w.theOperator), "wallet Object does not exist");

            byte[] wBalanceBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(WalletsBalancesPrefix).Concat(_walletId));
            Map<byte[], BigInteger> wBalanceMap = new Map<byte[], BigInteger>();
            if (wBalanceBs.Length > 0)
            {
                wBalanceMap = Helper.Deserialize(wBalanceBs) as Map<byte[], BigInteger>;
            }
            if (!wBalanceMap.HasKey(_tokenAddress))
            {
                wBalanceMap[_tokenAddress] = 0;
            }
            if (_mathOperation == "add")
            {
                wBalanceMap[_tokenAddress] += _amount;
            }
            else if (_mathOperation == "sub")
            {
                wBalanceMap[_tokenAddress] -= _amount;
                assert(wBalanceMap[_tokenAddress] >= 0, "balance is less than zero");
            }
            else
            {
                assert(false, "math operation illegal");
            }
            Storage.Put(Storage.CurrentContext, WalletsPrefix.Concat(WalletsBalancesPrefix).Concat(_walletId), Helper.Serialize(wBalanceMap));
            return true;
        }

        private static bool _isLegalAddresses(byte[][] addrs)
        {
            for (var i = 0; i < addrs.Length; i++)
            {
                if (_isLegalAddress(addrs[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool _isLegalAddress(byte[] addr)
        {
            return addr.Length == 20 && addr != AddressZero;
        }

        private static bool _isByte32(byte[] byte32)
        {
            return byte32.Length == 32;
        }

        public static bool _isWalletOwner(byte[] _walletId, byte[] _addr)
        {
            Wallet w = getWallet(_walletId);
            for (var i = 0; i < w.owners.Length; i++)
            {
                if (_addr == w.owners[i])
                {
                    return true;
                }
            }
            return false;
        }

        private static void _onlyPauser(byte[] invoker)
        {
            assert(isPauser(invoker), "the invoker is not one effective pauser");
        }

        public static bool isPauser(byte[] account)
        {
            byte[] pauserBs = Storage.Get(Storage.CurrentContext, PauserKey);
            if (pauserBs.Length > 0)
            {
                Map<byte[], bool> pausers = Helper.Deserialize(pauserBs) as Map<byte[], bool>;
                if (pausers.HasKey(account))
                {
                    return pausers[account];
                }
                return false;
            }
            return false;
        }

        [DisplayName("addPauser")]
        public static object addPauser(byte[] invoker, byte[] account)
        {
            assert(Runtime.CheckWitness(invoker), "Checkwitness failed");

            // make sure the invoker is one effective pauser
            byte[] pauserBs = Storage.Get(Storage.CurrentContext, PauserKey);
            assert(pauserBs.Length > 0, "pauser map is empty");
            Map<byte[], bool> pausers = Helper.Deserialize(pauserBs) as Map<byte[], bool>;
            assert(pausers.HasKey(invoker), "invoker is not one pauser");
            assert(pausers[invoker], "invoker is an effective pauser");

            // update the pausers map
            pausers[account] = true;
            Storage.Put(Storage.CurrentContext, PauserKey, Helper.Serialize(pausers));
            PauserAdded(account);
            return true;
        }

        [DisplayName("removePauser")]
        public static object removePauser(byte[] invoker, byte[] account)
        {
            assert(Runtime.CheckWitness(invoker), "Checkwitness failed");

            // make sure the invoker is one effective pauser
            byte[] pauserBs = Storage.Get(Storage.CurrentContext, PauserKey);
            assert(pauserBs.Length > 0, "pauser map is empty");
            Map<byte[], bool> pausers = Helper.Deserialize(pauserBs) as Map<byte[], bool>;
            assert(pausers.HasKey(invoker), "invoker is not one pauser");
            assert(pausers[invoker], "invoker is an effective pauser");

            // update the pausers map
            pausers[account] = false;
            Storage.Put(Storage.CurrentContext, PauserKey, Helper.Serialize(pausers));
            PauserRemoveded(account);
            return true;
        }
        
        public static bool getPausedStatus()
        {
            return Storage.Get(Storage.CurrentContext, PausedKey) == new byte[] { 0x01 };
        }

        public static void _whenNotPaused()
        {
            assert(!getPausedStatus(), "Pausable: paused");
        }

        public static void _whenPaused()
        {
            assert(getPausedStatus(), "Pausable: paused");
        }

        [DisplayName("pause")]
        public static object pause(byte[] invoker)
        {
            assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            _onlyPauser(invoker);
            _whenNotPaused();
            Storage.Put(Storage.CurrentContext, PausedKey, new byte[] { 0x01 });
            Paused(invoker);
            return true;
        }

        [DisplayName("unpause")]
        public static object unpause(byte[] invoker)
        {
            assert(Runtime.CheckWitness(invoker), "CheckWitness failed");

            _onlyPauser(invoker);
            _whenPaused();
            Storage.Put(Storage.CurrentContext, PausedKey, new byte[] { 0x00 });
            UnPaused(invoker);
            return true;
        }

        public static void assert(bool condition, string msg)
        {
            if (!condition)
            {
                throw new Exception((msg.HexToBytes().Concat(" error ".HexToBytes())).AsString());
            }
        }
    }
}
