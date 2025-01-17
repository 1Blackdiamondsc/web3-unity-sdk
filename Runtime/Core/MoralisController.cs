/**
 *           Module: MoralisSetup.cs
 *  Descriptiontion: Example class that demonstrates a game menu that incorporates
 *                   Wallet Connect and Moralis Authentication.
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
using MoralisUnity.Platform;
using UnityEngine;
using WalletConnectSharp.Unity;
using WalletConnectSharp.Core.Models;

namespace MoralisUnity
{
    public class MoralisController : MonoBehaviour
    {
        public WalletConnect walletConnect;

        public async UniTask Initialize()
        {
            if (!Moralis.Initialized)
            {
                HostManifestData hostManifestData = new HostManifestData()
                {
                    Version = MoralisSettings.MoralisData.ApplicationVersion,
                    Identifier = MoralisSettings.MoralisData.ApplicationName,
                    Name = MoralisSettings.MoralisData.ApplicationName,
                    ShortVersion = MoralisSettings.MoralisData.ApplicationVersion
                };

                ClientMeta clientMeta = new ClientMeta()
                {
                    Name = MoralisSettings.MoralisData.ApplicationName,
                    Description = MoralisSettings.MoralisData.ApplicationDescription,
                    Icons = new[] { MoralisSettings.MoralisData.ApplicationIconUri },
                    URL = MoralisSettings.MoralisData.ApplicationUrl
                };

                walletConnect.AppData = clientMeta;

                // Initialize and register the Moralis, Moralis Web3Api and NEthereum Web3 clients
                await Moralis.Start(MoralisSettings.MoralisData.ServerUri, MoralisSettings.MoralisData.ApplicationId, hostManifestData, clientMeta);
            }
        }
    }
}
