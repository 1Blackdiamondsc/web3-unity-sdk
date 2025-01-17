/**
 *           Module: MoralisUnity.cs
 *  Descriptiontion: Class that wraps moralis integration points. Provided as an 
 *                   example of how Moralis can be integrated into Unity
 *           Author: Moralis Web3 Technology AB, 559307-5988 - David B. Goodrich
 *  
 *  MIT License
 *  
 *  Copyright (c) 2021 Moralis Web3 Technology AB, 559307-5988
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */
using Cysharp.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using MoralisUnity;
using MoralisUnity.Platform;
using MoralisUnity.Platform.Objects;
using MoralisUnity.SolanaApi.Client;
using MoralisUnity.Web3Api.Client;
using MoralisUnity.Web3Api.Interfaces;
using MoralisUnity.Web3Api.Models;
using UnityEngine;
using UnityEngine.Scripting;
using WalletConnectSharp.Core;
using WalletConnectSharp.Core.Models;
using WalletConnectSharp.NEthereum;
using WalletConnectSharp.Unity;

namespace MoralisUnity
{
    /// <summary>
    /// Class that wraps moralis integration points. Provided as an example of 
    /// how Moralis can be integrated into Unity
    /// </summary>
    public class Moralis : MonoBehaviour
    {
        /// <summary>
        /// Indicates that the MoralisUnity has been initialized.
        /// </summary>
        public static bool Initialized { get; set; }

        public static ChainList ChainList;

#if UNITY_WEBGL
        /// <summary>
        /// Provide Web3 for WebGL client.
        /// </summary>
        public static Web3GL Web3Client { get; set; }
        private static string web3ClientRpcUrl;
#else
        public static Web3 Web3Client { get; set; }
#endif
        private static ClientMeta clientMetaData;

        private static EvmContractManager contractManager;

        // Singleton instance of Moralis so that is it is available application 
        // wide after being initialized.
        private static MoralisClient client;
        private static ServerConnectionData connectionData;
        
        // Since the user object is used so often, once the user is authenticated 
        // keep a local copy to save some cycles.
        private static MoralisUser user;

        public static IWeb3Api Web3Api;
        
        /// <summary>
        /// Initializes the connection to a Moralis server.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="serverUri"></param>
        /// <param name="hostData"></param>
        /// <param name="clientMeta"></param>
        /// <param name="web3ApiKey"></param>
        public static async UniTask Start(string serverUri, string applicationId, HostManifestData hostData = null, ClientMeta clientMeta = null, string web3ApiKey = null)
        {
            // Application Id is requried.
            if (string.IsNullOrEmpty(applicationId))
            {
                Debug.LogError("Application Id is required.");
                throw new ArgumentException("Application Id was not supplied.");
            }
            // Server URI is required.
            if (string.IsNullOrEmpty(serverUri))
            {
                Debug.LogError("Server URI is required.");
                throw new ArgumentException("Server URI was not supplied.");
            }

            // CHeck that requried Host data properties are set.
            if (hostData == null ||
                string.IsNullOrEmpty(hostData.Version) ||
                string.IsNullOrEmpty(hostData.Name) ||
                string.IsNullOrEmpty(hostData.ShortVersion) ||
                string.IsNullOrEmpty(hostData.Identifier))
            {
                hostData = new HostManifestData()
                {
                    Version = "0.0.1",
                    Identifier = "Identifier",
                    Name = "Name",
                    ShortVersion = "0.0.1"
                };
            }

            // Create instance of Evm Contract Manager.
            contractManager = new EvmContractManager();

            // Set Moralis conenction values.
            connectionData = new ServerConnectionData();
            connectionData.ApplicationID = applicationId;
            connectionData.ServerURI = serverUri;
            connectionData.ApiKey = web3ApiKey;

            // For unity apps the local storage value must also be set.
            connectionData.LocalStoragePath = Application.persistentDataPath;

            // TODO Make this optional!
            connectionData.Key = "";

            // Set manifest / host data required so that the Moralis Client does not
            // attempt to infer them from Assembly values not available in Unity.
            MoralisClient.ManifestData = hostData;

            // Define a Unity specific Json Serializer.
            UnityNewtosoftSerializer jsonSerializer = new UnityNewtosoftSerializer();

            // If user passed web3apikey, add it to configuration.
            if (web3ApiKey is { }) MoralisUnity.SolanaApi.Client.Configuration.ApiKey["X-API-Key"] = web3ApiKey;
            if (web3ApiKey is { }) MoralisUnity.Web3Api.Client.Configuration.ApiKey["X-API-Key"] = web3ApiKey;

            // Create an instance of Moralis Server Client
            // NOTE: Web3ApiClient is optional. If you are not using the Moralis 
            // Web3Api REST API you can call the method with just connectionData
            // NOTE: If you are using a custom user object use 
            // new MoralisClient<YourUser>(connectionData, address, Web3ApiClient)
            client = new MoralisClient(connectionData, new Web3ApiClient(), new SolanaApiClient(), jsonSerializer);

            clientMetaData = clientMeta;

            if (client == null)
            {
                Debug.Log("Moralis connection failed!");
            }
            else
            {
                Web3Api = client.Web3Api;
                Initialized = true;
                Debug.Log("Connected to Moralis!");
                user = await client.GetCurrentUserAsync();
            }
        }

        /// <summary>
        /// Properly dispose Moralis Client, shuts down any subscriptions, etc.
        /// </summary>
        public static void Dispose()
        {
            client.Dispose();
        }

        /// <summary>
        /// Get the Moralis Server Client.
        /// </summary>
        /// <returns></returns>
        public static MoralisClient GetClient()
        {
            return client;
        }

        /// <summary>
        /// Provides the current authenticated user if Moralis 
        /// authentication has been completed.
        /// </summary>
        /// <returns>MoralisUser</returns>
        public static async UniTask<MoralisUser> GetUser()
        {
            if (user == null)
            {
                user = await client.GetCurrentUser();
            }

            return user;
        }

        /// <summary>
        /// Provides the current authenticated user if Moralis 
        /// authentication has been completed.
        /// </summary>
        /// <returns>MoralisUser</returns>
        public static async UniTask<MoralisUser> GetUserAsync()
        {
            if (user == null)
            {
                user = await client.GetCurrentUserAsync();
            }

            return user;
        }

        /// <summary>
        /// Idicates if user is already logged in.
        /// </summary>
        /// <returns></returns>
        public static bool IsLoggedIn() { return user != null; }

        /// <summary>
        /// Authenicate the user by logging into Moralis using message signed by 
        /// Crypto Wallat. If this is a new user, the user's record is automatically 
        /// created.
        /// EXAMPLE: { { "id", address }, { "signature", response }, { "data", "Moralis Authentication" } }
        /// </summary>
        /// <param name="authData"></param>
        /// <returns></returns>
        public static async UniTask<MoralisUser> LogInAsync(IDictionary<string, object> authData)
        {
            return await client.LogInAsync(authData, CancellationToken.None);
        }

        /// <summary>
        /// Login using username and password.
        /// </summary>
        /// <param name="username">username / Email</param>
        /// <param name="password">user password</param>
        /// <returns>MoralisUser</returns>
        public static UniTask<MoralisUser> LogInAsync(string username, string password)
        {
            return client.UserService.LogInAsync(username, password, client.ServiceHub);
        }

        /// <summary>
        /// Logout the user session.
        /// </summary>
        /// <returns></returns>
        public static UniTask LogOutAsync()
        {
            return client.LogOutAsync();
        }

        public static List<ChainEntry> SupportedChains => SupportedEvmChains.SupportedChains;
        
        /// <summary>
        /// Initializes the Web3 connection to the supplied RPC Url. Call this to change the target chain.
        /// </summary>
        /// <returns></returns>
        public static async UniTask SetupWeb3()
        {
            if (clientMetaData == null)
            {
                Debug.LogError("Metadata not provided.");
            }
#if UNITY_WEBGL
            await Web3GL.Connect(clientMetaData);
#else
            await UniTask.Run(() =>
            {
                WalletConnectSession client = WalletConnect.Instance.Session;

                // Create a web3 client using Wallet Connect as write client and a dummy client as read client.
                Web3Client = new Web3(client.CreateProvider(new DeadRpcReadClient(Debug.LogError)));
            });
#endif
        }
        
        /// <summary>
        /// Performs a transfer of value to receipient.
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="value"></param>
        /// <param name="gas"></param>
        /// <param name="gasPrice"></param>
        /// <returns></returns>
        public async static UniTask<string> SendTransactionAsync(string recipientAddress, HexBigInteger value, HexBigInteger gas = null, HexBigInteger gasPrice = null)
        {
            string txnHash = null;
            
            // Retrieve from address, the address used to authenticate the user.
            MoralisUser user = await Moralis.GetUserAsync();
            string fromAddress = user.authData["moralisEth"]["id"].ToString();
            
            try
            {
#if UNITY_WEBGL
                string g = "";
                string gp = "";
                
                if (gas != null) g = gas.Value.ToString();
                if (gasPrice != null) gp = gasPrice.Value.ToString();
                
                txnHash = await Web3GL.SendTransaction(recipientAddress, value.Value.ToString(), g, gp);
#else
                // Create transaction request.
                TransactionInput txnRequest = new TransactionInput()
                {
                    Data = String.Empty,
                    From = fromAddress,
                    To = recipientAddress,
                    Value = value
                };
                
                // Execute the transaction.
                txnHash = await Moralis.Web3Client.Eth.TransactionManager.SendTransactionAsync(txnRequest);
#endif              
            }
            catch (Exception exp)
            {
                Debug.Log($"Transfer of {value.Value} WEI from {fromAddress} to {recipientAddress} failed: {exp.Message}");
            }

            return txnHash;
        }
        
        /// <summary>
        /// Executes a contract function.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="abi"></param>
        /// <param name="functionName"></param>
        /// <param name="args"></param>
        /// <param name="value"></param>
        /// <param name="gas"></param>
        /// <param name="gasPrice"></param>
        /// <returns></returns>
        public async static UniTask<string> ExecuteContractFunction(string contractAddress,
            string abi,
            string functionName,
            object[] args,
            HexBigInteger value,
            HexBigInteger gas,
            HexBigInteger gasPrice)
        {
            string result = null;
            string gasValue = gas.Value.ToString();
            string gasPriceValue = gasPrice.ToString();
            
            if (gasValue.Equals("0") || gasValue.Equals("0x0")) gasValue = "";
            if (gasPriceValue.Equals("0") || gasPriceValue.Equals("0x0")) gasPriceValue = "";
            
            try
            {
#if UNITY_WEBGL
                string functionArgs = JsonConvert.SerializeObject(args);
                result = await Web3GL.SendContract(functionName, abi, contractAddress, functionArgs, value.Value.ToString(), gasValue, gasPriceValue);
#else
                // Retrieve from address, the address used to authenticate the user.
                MoralisUser user = await Moralis.GetUserAsync();
                string fromAddress = user.authData["moralisEth"]["id"].ToString();

                Contract contractInstance = Web3Client.Eth.GetContract(abi, contractAddress);
                Function function = contractInstance.GetFunction(functionName);

                if (function != null)
                {
                    result = await function.SendTransactionAsync(fromAddress, gas, value, args);
                }
#endif
            }
            catch (Exception exp)
            {
                Debug.Log($"Call to {functionName} failed due to: {exp.Message}");
            }
            
            return result;
        }
        
#if !UNITY_WEBGL
        /// <summary>
        /// Creates and adds a contract instance based on ABI and associates it to specified chain and address.
        /// </summary>
        /// <param name="key">How you identify the contract instance.</param>
        /// <param name="abi">ABI of the contract in standard ABI json format</param>
        /// <param name="baseChainId">The initial chain Id used to interact with this contract</param>
        /// <param name="baseContractAddress">The initial contract address of the contract on specified chain</param>
        public static void InsertContractInstance(string key, string abi, string baseChainId, string baseContractAddress)
        {
            if (Web3Client == null)
            {
                Debug.LogError("Web3 has not been setup yet.");
            }
            else
            {
                EvmContractItem eci = new EvmContractItem(Web3Client, abi, baseChainId, baseContractAddress);

                contractManager.InsertContractInstance(key, eci);
            }
        }

        /// <summary>
        /// Adds a contract address for a chain to a specific contract. Contract for key must exist.
        /// </summary>
        /// <param name="key">How you identify the contract instance.</param>
        /// <param name="chainId">The The chain the contract is deployed on.</param>
        /// <param name="contractAddress">Address the contract is deployed at</param>
        public static void AddContractChainAddress(string key, string chainId, string contractAddress)
        {
            if (Web3Client == null)
            {
                Debug.LogError("Web3 has not been setup yet.");
            }
            else
            {
                contractManager.AddChainInstanceToContract(key, Web3Client, chainId, contractAddress);
            }
        }

        /// <summary>
        /// Retrieves the specified contract instance if it exists.
        /// </summary>
        /// <param name="key">How you identify the contract instance.</param>
        /// <param name="chainId">The The chain the contract is deployed on.</param>
        /// <returns>Nethereum.Contracts.Contract</returns>
        public static Contract EvmContractInstance(string key, string chainId)
        {
            Contract contract = null;

            if (Web3Client == null)
            {
                Debug.LogError("Web3 has not been setup yet.");
            }
            else
            {
                if (contractManager.Contracts.ContainsKey(key) &&
                    contractManager.Contracts[key].ChainContractMap.ContainsKey(chainId))
                {
                    contract = contractManager.Contracts[key].ChainContractMap[chainId].ContractInstance;
                }
            }

            return contract;
        }

        /// <summary>
        /// Get an Nethereum Function instance from a specific contract.
        /// </summary>
        /// <param name="key">How you identify the contract instance.</param>
        /// <param name="chainId">The The chain the contract is deployed on.</param>
        /// <param name="functionName">Name of the function to return</param>
        /// <returns>Function</returns>
        public static Function EvmContractFunctionInstance(string key, string chainId, string functionName)
        {
            Contract contract = EvmContractInstance(key, chainId);
            Function function = null;

            if (contract != null)
            {
                function = contract.GetFunction(functionName);
            }

            return function;
        }

        /// <summary>
        /// Executes a NEthereum SendTransactionAsync which executes a function 
        /// on a EVM contract (can change state) and returns response as a 
        /// string.
        /// </summary>
        /// <param name="contractKey">How you identify the contract instance.</param>
        /// <param name="chainId">he The chain the contract is deployed on.</param>
        /// <param name="functionName">name of function to call</param>
        /// <param name="transactionInput">NEthereum TransactionInput object</param>
        /// <param name="functionInput">Function params</param>
        /// <returns>string</returns>
        [Obsolete("This method is deprecated. Please use SendTransactionAsync or ExecuteContractFunction as appropriate.")]
        public static async UniTask<string> SendEvmTransactionAsync(string contractKey, string chainId, string functionName, TransactionInput transactionInput, object[] functionInput)
        {
            string result = null;

            if (Web3Client == null)
            {
                Debug.LogError("Web3 has not been setup yet.");
            }
            else
            {
                if (contractManager.Contracts.ContainsKey(contractKey) &&
                    contractManager.Contracts[contractKey].ChainContractMap.ContainsKey(chainId))
                {
                    Tuple<bool, string, string> resp = await contractManager.SendTransactionAsync(contractKey, chainId, functionName, transactionInput, functionInput);

                    if (resp.Item1) result = resp.Item2;
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractKey"></param>
        /// <param name="chainId"></param>
        /// <param name="functionName"></param>
        /// <param name="fromaddress"></param>
        /// <param name="gas"></param>
        /// <param name="value"></param>
        /// <param name="functionInput"></param>
        /// <returns></returns>
        [Obsolete("This method is deprecated. Please use SendTransactionAsync or ExecuteContractFunction as appropriate.")]
        public static async UniTask<string> SendEvmTransactionAsync(string contractKey, string chainId, string functionName, string fromaddress, HexBigInteger gas, HexBigInteger value, object[] functionInput)
        {
            string result = null;

            if (Web3Client == null)
            {
                Debug.LogError("Web3 has not been setup yet.");
            }
            else
            {
                if (contractManager.Contracts.ContainsKey(contractKey) &&
                    contractManager.Contracts[contractKey].ChainContractMap.ContainsKey(chainId))
                {
                    Tuple<bool, string, string> resp = await contractManager.SendTransactionAsync(contractKey, chainId, functionName, fromaddress, gas, value, functionInput);

                    if (resp.Item1)
                    {
                        result = resp.Item2;
                    }
                    else
                    {
                        Debug.LogError($"Evm Transaction failed: {resp.Item3}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractKey"></param>
        /// <param name="chainId"></param>
        /// <param name="functionName"></param>
        /// <param name="fromaddress"></param>
        /// <param name="gas"></param>
        /// <param name="value"></param>
        /// <param name="functionInput"></param>
        /// <returns></returns>
        [Obsolete("This method is deprecated. Please use SendTransactionAsync or ExecuteContractFunction as appropriate.")]
        public static async UniTask<string> SendTransactionAndWaitForReceiptAsync(string contractKey, string chainId, string functionName, string fromaddress, HexBigInteger gas, HexBigInteger value, object[] functionInput)
        {
            string result = null;

            if (Web3Client == null)
            {
                Debug.LogError("Web3 has not been setup yet.");
            }
            else
            {
                if (contractManager.Contracts.ContainsKey(contractKey) &&
                    contractManager.Contracts[contractKey].ChainContractMap.ContainsKey(chainId))
                {
                    Tuple<bool, string, string> resp = await contractManager.SendTransactionAndWaitForReceiptAsync(contractKey, chainId, functionName, fromaddress, gas, value, functionInput);

                    if (resp.Item1)
                    {
                        result = resp.Item2;
                    }
                    else
                    {
                        Debug.LogError($"Evm Transaction failed: {resp.Item3}");
                    }
                }
            }

            return result;
        }
#endif
    }
}