using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BobboNet.Networking.Unity
{
    public class GameClientComponent : MonoBehaviour
    {
        //
        //  Properties
        //

        public IGameClient Client { get; private set; }

        //
        //  Public Methods
        //

        public void Setup(IGameClient client) 
        {
            Client = client;
            OnSetup();
        }

        //
        //  Unity Methods
        //

        private void Update()
        {
            // If we're connected, then run the update function.
            if (Client.GetState() == NetworkState.Connected) OnUpdate();
        }

        //
        //  Virtual Methods
        //

        protected virtual void OnSetup() { }
        protected virtual void OnUpdate() { }
        public virtual void OnRegisterPacketTypes(NetPacketProcessor packetProcessor) { }
        public virtual void OnNetworkStateChanged(NetworkState newState) { }
    }
}
