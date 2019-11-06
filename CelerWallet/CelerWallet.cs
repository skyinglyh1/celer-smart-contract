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
        public class Wallet
        {
            public byte[][] owners;
            public byte[] theOperator;
            //public Map<byte[], BigInteger> balances;
            public byte[] proposedNewOperator;
            //public Map<byte[], bool> proposalVotes;
        }

        //private static bool _paused;
        public static byte[] PausedKey = "paused".AsByteArray();
        //public static Map<byte[], bool> _pausers;
        public static byte[] PauserKey = "pausers".AsByteArray();
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
                    BasicMethods.assert(args.Length == 1, "params length error");
                    byte[] invoker = (byte[])args[0];
                    return unpause(invoker);
                }
                if (method == "addPauser")
                {
                    BasicMethods.assert(args.Length == 2, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] account = (byte[])args[1];
                    return addPauser(invoker, account);
                }
                if (method == "removePauser")
                {
                    BasicMethods.assert(args.Length == 2, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] account = (byte[])args[1];
                    return removePauser(invoker, account);
                }
                if (method == "create")
                {
                    BasicMethods.assert(args.Length == 4, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[][] owners = (byte[][])args[1];
                    byte[] theOperator = (byte[])args[2];
                    byte[] nonce = (byte[])args[3];
                    return create(invoker, owners, theOperator, nonce);
                }
                if (method == "depositNEP5")
                {
                    BasicMethods.assert(args.Length == 4, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] walletId = (byte[])args[1];
                    byte[] tokenAddress = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return depositNEP5(invoker, walletId, tokenAddress, amount);
                }
                if (method == "withdraw")
                {
                    BasicMethods.assert(args.Length == 4, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] tokenAddress = (byte[])args[1];
                    byte[] receiver = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return withdraw(invoker, tokenAddress, receiver, amount);
                }
                if (method == "transferToWallet")
                {
                    BasicMethods.assert(args.Length == 5, "params length error");
                    byte[] fromWalletId = (byte[])args[0];
                    byte[] toWalletId = (byte[])args[1];
                    byte[] tokenAddress = (byte[])args[2];
                    byte[] receiver = (byte[])args[3];
                    BigInteger amount = (BigInteger)args[4];
                    return transferToWallet(fromWalletId, toWalletId, tokenAddress, receiver, amount);
                }
                if (method == "transferOperatorship")
                {
                    BasicMethods.assert(args.Length == 2, "params length error");
                    byte[] walletId = (byte[])args[0];
                    byte[] newOperator = (byte[])args[1];
                    return transferOperatorship(walletId, newOperator);
                }
                if (method == "proposeNewOperator")
                {
                    BasicMethods.assert(args.Length == 3, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] walletId = (byte[])args[1];
                    byte[] newOperator = (byte[])args[2];
                    return proposeNewOperator(invoker, walletId, newOperator);
                }
                if (method == "drainToken")
                {
                    BasicMethods.assert(args.Length == 3, "params length error");
                    byte[] invoker = (byte[])args[0];
                    byte[] tokenAddress = (byte[])args[1];
                    byte[] receiver = (byte[])args[2];
                    BigInteger amount = (BigInteger)args[3];
                    return drainToken(invoker, tokenAddress, receiver, amount);
                }
                if (method == "getWalletOwners")
                {
                    BasicMethods.assert(args.Length == 1, "params length error");
                    byte[] walletId = (byte[])args[0];
                    return getWalletOwners(walletId);
                }
                if (method == "getOperator")
                {
                    BasicMethods.assert(args.Length == 1, "params length error");
                    byte[] walletId = (byte[])args[0];
                    return getOperator(walletId);
                }
                if (method == "getBalance")
                {
                    BasicMethods.assert(args.Length == 2, "params length error");
                    byte[] walletId = (byte[])args[0];
                    byte[] tokenAddress = (byte[])args[1];
                    return getBalance(walletId, tokenAddress);
                }
                if (method == "getProposalVote")
                {
                    BasicMethods.assert(args.Length == 2, "params length error");
                    byte[] walletId = (byte[])args[0];
                    byte[] owner = (byte[])args[1];
                    return getProposalVote(walletId, owner);
                }
            }
            return false;
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
            BasicMethods.assert(Runtime.CheckWitness(getWallet(_walletId).theOperator), "operator checkwitness failed");
            //byte[] theOperator = getWallet(_walletId).theOperator;
            //assert(_invoker == theOperator, "msg.sender is not operator");
        }

        public static void _onlyWalletOwner(byte[] _walletId, byte[] _addr)
        {
            BasicMethods.assert(_isWalletOwner(_walletId, _addr), "Given address is not wallet owner");
        }

        [DisplayName("create")]
        public static byte[] create(byte[] invoker, byte[][] owners, byte[] theOperator, byte[] nonce)
        {
            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            BasicMethods.assert(BasicMethods._isLegalAddresses(owners), "owners addresses are not byte20");
            BasicMethods.assert(BasicMethods._isLegalAddress(theOperator), "the operator is not byte20");
            //TODO: no need to check the nonce byte[] length

            _whenNotPaused();

            BasicMethods.assert(BasicMethods._isLegalAddress(theOperator), "New operator is address zero");
            byte[] SelfContractHash = ExecutionEngine.ExecutingScriptHash;
            byte[] concatRes = SelfContractHash.Concat(invoker).Concat(nonce);
            byte[] walletId = SmartContract.Sha256(concatRes);
            BasicMethods.assert(getWallet(walletId).theOperator == null, "Occupied wallet id");
            Wallet w = new Wallet();
            BasicMethods.assert(BasicMethods._isLegalAddresses(owners), "owners contains illegal address");
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
            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId is not byte32");
            BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddress), "tokenAddress is not byte20");
            BasicMethods.assert(amount >= 0, "amount is less than zero");

            _whenNotPaused();

            BasicMethods.assert(_updateBalance(walletId, tokenAddress, amount, "add"), "updateBalance failed");
            NEP5Contract dyncall = (NEP5Contract)tokenAddress.ToDelegate();
            Object[] args = new object[] { invoker, ExecutionEngine.ExecutingScriptHash, amount };
            bool res = (bool)dyncall("transfer", args);
            BasicMethods.assert(res, "transfer NEP5 tokens failed");

            DepositToWallet(walletId, tokenAddress, amount);
            return true;
        }

        [DisplayName("withdraw")]
        public static object withdraw(byte[] walletId, byte[] tokenAddress, byte[] receiver, BigInteger amount)
        {
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal, not byte32");
            BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddress), "tokenAddress is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(receiver), "receiver address is illegal");
            BasicMethods.assert(amount > 0, "amount is less than zero");
            //assert(Runtime.CheckWitness(receiver), "CheckWitness failed");

            _whenNotPaused();
            _onlyOperator(walletId);
            _onlyWalletOwner(walletId, receiver);

            BasicMethods.assert(_updateBalance(walletId, tokenAddress, amount, "sub"), "updateBalance failed");
            BasicMethods.assert(_withdrawToken(tokenAddress, receiver, amount), "withdrawToken failed");
            WithdrawFromWallet(walletId, tokenAddress, receiver, amount);
            return true;
        }

        [DisplayName("transferToWallet")]
        public static object transferToWallet(byte[] fromWalletId, byte[] toWalletId, byte[] tokenAddress, byte[] receiver, BigInteger amount)
        {
            BasicMethods.assert(BasicMethods._isByte32(fromWalletId), "fromWalletId illegal, not byte32");
            BasicMethods.assert(BasicMethods._isByte32(toWalletId), "toWalletId illegal, not byte32");
            BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddress), "tokenAddress is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(receiver), "receiver address is illegal");
            BasicMethods.assert(amount > 0, "amount is less than zero");
            //assert(Runtime.CheckWitness(receiver), "CheckWitness failed");

            _whenNotPaused();
            _onlyOperator(fromWalletId);
            _onlyWalletOwner(fromWalletId, receiver);
            _onlyWalletOwner(toWalletId, receiver);

            BasicMethods.assert(_updateBalance(fromWalletId, tokenAddress, amount, "sub"), "sub balance in from wallet failed");
            BasicMethods.assert(_updateBalance(toWalletId, tokenAddress, amount, "add"), "add balance in to wallet failed");

            TransferToWallet(fromWalletId, toWalletId, tokenAddress, receiver, amount);
            return true;
        }

        [DisplayName("transferOperatorship")]
        public static object transferOperatorship(byte[] walletId, byte[] newOperator)
        {
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal, not byte32");
            BasicMethods.assert(BasicMethods._isLegalAddress(newOperator), "newOperator address is illegal");
            // no need to checkwitness since _onlyOperator has already done it

            _whenNotPaused();
            _onlyOperator(walletId);
            _changeOperator(walletId, newOperator);
            return true;
        }

        [DisplayName("proposeNewOperator")]
        public static object proposeNewOperator(byte[] invoker, byte[] walletId, byte[] newOperator)
        {
            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(newOperator), "new operator is not address");

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
            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddress), "tokenAddress is illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(receiver), "receiver address is illegal");
            BasicMethods.assert(amount >= 0, "amount is less than zero");

            _whenPaused();
            _onlyPauser(invoker);

            BasicMethods.assert(_withdrawToken(tokenAddress, receiver, amount), "withdrawToken failed");

            DrainToken(tokenAddress, receiver, amount);
            return true;
        }

        [DisplayName("getWalletOwners")]
        public static byte[][] getWalletOwners(byte[] walletId)
        {
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal");
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            Wallet w = new Wallet();
            if (walletBs.Length > 0)
                w = Helper.Deserialize(walletBs) as Wallet;
            return w.owners;
        }

        [DisplayName("getOperator")]
        public static byte[] getOperator(byte[] walletId)
        {
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal");
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            Wallet w = new Wallet();
            if (walletBs.Length > 0)
                w = Helper.Deserialize(walletBs) as Wallet;
            return w.theOperator;
        }

        [DisplayName("getBalance")]
        public static BigInteger getBalance(byte[] walletId, byte[] tokenAddress)
        {
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(tokenAddress), "tokenAddress is illegal");
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
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal");
            Wallet w = new Wallet();
            byte[] walletBs = Storage.Get(Storage.CurrentContext, WalletsPrefix.Concat(walletId));
            if (walletBs.Length > 0)
                w = Helper.Deserialize(walletBs) as Wallet;
            return w.theOperator;
        }

        [DisplayName("getProposalVote")]
        public static bool getProposalVote(byte[] walletId, byte[] owner)
        {
            BasicMethods.assert(BasicMethods._isByte32(walletId), "walletId illegal");
            BasicMethods.assert(BasicMethods._isLegalAddress(owner), "owner address is not length of 20 bytes");
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
            BasicMethods.assert(BasicMethods._isLegalAddress(_newOperator), "new operator is illegal");
            Wallet w = getWallet(_walletId);
            byte[] oldOperator = w.theOperator;
            BasicMethods.assert(BasicMethods._isLegalAddress(oldOperator), "old operator is not legal");

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

            BasicMethods.assert(_amount >= 0, "amount is less than zero");

            Wallet w = getWallet(_walletId);
            BasicMethods.assert(BasicMethods._isLegalAddress(w.theOperator), "wallet Object does not exist");

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
                BasicMethods.assert(wBalanceMap[_tokenAddress] >= 0, "balance is less than zero");
            }
            else
            {
                BasicMethods.assert(false, "math operation illegal");
            }
            Storage.Put(Storage.CurrentContext, WalletsPrefix.Concat(WalletsBalancesPrefix).Concat(_walletId), Helper.Serialize(wBalanceMap));
            return true;
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
            BasicMethods.assert(isPauser(invoker), "the invoker is not one effective pauser");
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
            BasicMethods.assert(Runtime.CheckWitness(invoker), "Checkwitness failed");

            // make sure the invoker is one effective pauser
            byte[] pauserBs = Storage.Get(Storage.CurrentContext, PauserKey);
            BasicMethods.assert(pauserBs.Length > 0, "pauser map is empty");
            Map<byte[], bool> pausers = Helper.Deserialize(pauserBs) as Map<byte[], bool>;
            BasicMethods.assert(pausers.HasKey(invoker), "invoker is not one pauser");
            BasicMethods.assert(pausers[invoker], "invoker is an effective pauser");

            // update the pausers map
            pausers[account] = true;
            Storage.Put(Storage.CurrentContext, PauserKey, Helper.Serialize(pausers));
            PauserAdded(account);
            return true;
        }

        [DisplayName("removePauser")]
        public static object removePauser(byte[] invoker, byte[] account)
        {
            BasicMethods.assert(Runtime.CheckWitness(invoker), "Checkwitness failed");

            // make sure the invoker is one effective pauser
            byte[] pauserBs = Storage.Get(Storage.CurrentContext, PauserKey);
            BasicMethods.assert(pauserBs.Length > 0, "pauser map is empty");
            Map<byte[], bool> pausers = Helper.Deserialize(pauserBs) as Map<byte[], bool>;
            BasicMethods.assert(pausers.HasKey(invoker), "invoker is not one pauser");
            BasicMethods.assert(pausers[invoker], "invoker is an effective pauser");

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
            BasicMethods.assert(!getPausedStatus(), "Pausable: paused");
        }

        public static void _whenPaused()
        {
            BasicMethods.assert(getPausedStatus(), "Pausable: paused");
        }

        [DisplayName("pause")]
        public static object pause(byte[] invoker)
        {
            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");
            _onlyPauser(invoker);
            _whenNotPaused();
            Storage.Put(Storage.CurrentContext, PausedKey, new byte[] { 0x01 });
            Paused(invoker);
            return true;
        }

        [DisplayName("unpause")]
        public static object unpause(byte[] invoker)
        {
            BasicMethods.assert(Runtime.CheckWitness(invoker), "CheckWitness failed");

            _onlyPauser(invoker);
            _whenPaused();
            Storage.Put(Storage.CurrentContext, PausedKey, new byte[] { 0x00 });
            UnPaused(invoker);
            return true;
        }
    }
}
