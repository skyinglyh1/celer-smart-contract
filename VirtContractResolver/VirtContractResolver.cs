using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;
using System.ComponentModel;
using Neo.SmartContract.Framework.Services.System;



namespace VirtContractResolver
{
    public class VirtContractResolver : SmartContract
    {

        // byte23 -> address
        //public Map<byte[], byte[]> virtToRealMap;
        public static readonly byte[] Virt2RealPrefix = "virtToRealMap".AsByteArray();
        private static readonly byte[] AddressZero = Helper.ToScriptHash("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

        [DisplayName("deploy")]
        public static event Action<byte[]> Deploy;


        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy")
                {
                    assert(args.Length == 2, "VirtContractResolver parameter error");
                    byte[] avmCode = (byte[])args[0];
                    BigInteger nonce = (BigInteger)args[1];
                    return deploy(avmCode, nonce);
                }
                if (operation == "resolve")
                {
                    assert(args.Length == 1, "VirtContractResolver parameter error");
                    byte[] virtHashId = (byte[])args[0];
                    return resolve(virtHashId);
                }
            }
            return false;
        }



        [DisplayName("deploy")]
        public static bool deploy(byte[] avmCode, BigInteger nonce)
        {
            byte[] virtHashId = SmartContract.Sha256(avmCode.Concat(nonce.AsByteArray()));
            byte[] storedVirtAddr = Storage.Get(Storage.CurrentContext, Virt2RealPrefix.Concat(virtHashId));
            assert(storedVirtAddr.Length == 0, "currently stored real address is not empty");
            // now deploy the contract 
            byte[] parameter_list = new byte[] { 0x07, 0x10 };
            byte return_type = 0x05;
            string name = "new contract";
            string version = "1";
            string author = "celer virtual contract resolver";
            string email = "empty";
            string description = "relay contract to deploy new contract to on-chain based on the avm/script code";
            Contract newDeployedContract = Contract.Create(avmCode, parameter_list, return_type, ContractPropertyState.HasStorage, name, version, author, email, description);

            //byte[] seemsToBeNewlyDeployedContractAddr = SmartContract.Hash160(avmCode);
            byte[] seemsToBeNewlyDeployedContractAddr = newDeployedContract.Script;

            assert(seemsToBeNewlyDeployedContractAddr != AddressZero, "Create contract failed");
            Storage.Put(Storage.CurrentContext, Virt2RealPrefix.Concat(virtHashId), seemsToBeNewlyDeployedContractAddr);

            Deploy(virtHashId);

            return true;
        }

        [DisplayName("resolve")]
        public static byte[] resolve(byte[] virtHashId)
        {
            byte[] storedVirtAddr = Storage.Get(Storage.CurrentContext, Virt2RealPrefix.Concat(virtHashId));
            assert(_isLegalAddress(storedVirtAddr), "nonexistent virtual address");
            return storedVirtAddr;

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
