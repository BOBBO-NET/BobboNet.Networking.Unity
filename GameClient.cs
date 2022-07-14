using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using BobboNet.Networking.Util;
using BobboNet.Networking.Messages;

namespace BobboNet.Networking.Unity
{
    public abstract class GameClient<SelfType, PlayerType, PlayerUpdate> : MonoBehaviour, IGameClient, ISubSystem
        where SelfType : GameClient<SelfType, PlayerType, PlayerUpdate>
        where PlayerType : GamePlayer<PlayerUpdate>
        where PlayerUpdate : class, INetSerializable, new()
    {
        //
        //  Properties
        //

        public NetworkState State { get; private set; } = NetworkState.Unknown;

        //
        //  Events
        //

        private event OnStateChangedDelegate OnStateChanged;

        //
        //  Variables
        //

        [Header("Settings")]
        [Tooltip("When true, the client logs when the network state changes.")]
        public bool logStateChange = false;

        private NetManager client;
        private NetPeer server;
        private NetPacketProcessor packetProcessor;
        private GameClientComponent[] submodules;
        private Dictionary<System.Type, List<object>> messageListeners = new Dictionary<System.Type, List<object>>();

        //
        //  Construction & Config
        //

        public void SetupSubSystem()
        {
            client = ConstructClient();
            packetProcessor = ConstructPacketProcessor();
            submodules = GetClientComponents();
            client.Start();

            SetNetworkState(NetworkState.Disconnected);
            SetupSubmodules();
        }

        private NetManager ConstructClient()
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            listener.NetworkReceiveEvent += OnNetworkReceive;
            listener.PeerConnectedEvent += OnConnectedToServer;
            listener.PeerDisconnectedEvent += OnDisconnectedFromServer;

            NetManager newClient = new NetManager(listener);
            newClient.AutoRecycle = true;
            newClient.ChannelsCount = NetworkChannelIDs.ChannelCount;

            return newClient;
        }

        private NetPacketProcessor ConstructPacketProcessor()
        {
            NetPacketProcessor processor = new NetPacketProcessor();

            // Register nested data types
            processor.RegisterBobboNetNestedTypes();
            processor.RegisterNestedType(() => new StandardMessages<PlayerUpdate>.SM_BatchPlayerUpdates());
            processor.RegisterNestedType(() => new StandardMessages<PlayerUpdate>.SM_InitialPlayerUpdates());
            processor.RegisterNestedType(() => new StandardMessages<PlayerUpdate>.SM_PlayerJoin());
            processor.RegisterNestedType(() => new StandardMessages<PlayerUpdate>.SM_PlayerLeave());
            OnRegisterPacketTypes(processor);

            return processor;
        }

        private void SetupSubmodules()
        {
            foreach (GameClientComponent submodule in submodules)
            {
                submodule.Setup(this);
            }
        }

        //
        //  Unity Methods
        //

        public void Update()
        {
            if (client.IsRunning) client.PollEvents();
        }

        public void OnDestroy()
        {
            if (client.IsRunning)
            {
                Disconnect();
                client.Stop();
            }
        }

        //
        //  Public Methods
        //

        public void Connect(string address, int port, string key = "")
        {
            SetNetworkState(NetworkState.Connecting);
            client.Connect(address, port, key);
        }

        public void Disconnect()
        {
            client.DisconnectAll();
        }

        public void Send<DataType>(DataType message, byte channel, DeliveryMethod deliveryMethod) where DataType : class, new()
        {
            server.Send(packetProcessor.Write<DataType>(message), channel, deliveryMethod);
        }

        public NetworkState GetState() => State;

        public void OnGetMessageSubscribe<MessageType>(System.Action<MessageType> action) where MessageType : class, new()
        {
            List<object> foundList;

            // If there are no listeners for this message type yet...
            if (!messageListeners.TryGetValue(typeof(MessageType), out foundList))
            {
                // ...then create the list of listeners for this type!
                foundList = new List<object>();
                messageListeners.Add(typeof(MessageType), foundList);

                // ALSO
                // ...hook into the packet processor
                packetProcessor.SubscribeReusable<MessageType, NetPeer>(OnPacketRecieved);
            }

            // Add the action to our list of listeners
            foundList.Add(action);
        }

        public bool OnGetMessageUnsubscribe<MessageType>(System.Action<MessageType> action)
        {
            List<object> foundList;

            // If there's no listeners for this type... EXIT EARLY
            if (!messageListeners.TryGetValue(typeof(MessageType), out foundList)) return false;

            // OTHERWISE...
            // ...we have a list. Remove this action from the list.
            return foundList.Remove(action);
        }

        public void OnStateChangeSubscribe(OnStateChangedDelegate action)
        {
            OnStateChanged += action;
        }

        public void OnStateChangeUnsubscribe(OnStateChangedDelegate action)
        {
            OnStateChanged -= action;
        }

        //
        //  Private Methods
        //

        /// <summary>
        /// Sets the value of State and if it's a new value, raises OnStateChanged.
        /// </summary>
        /// <param name="newState">The value to set State to.</param>
        private void SetNetworkState(NetworkState newState)
        {
            if (State == newState) return;  // If we're trying to set to a state we're already at, then EXIT EARLY
            State = newState;               // ...OTHERWISE, update the state internally.

            // If we should log this, then DO IT nerd.
            if(logStateChange) Debug.Log($"Network State: {newState}");

            // Tell all submodules that the network state has changed
            foreach (GameClientComponent submodule in submodules)
            {
                submodule.OnNetworkStateChanged(newState);
            }

            // Temporarily copy the event handler to avoid race condition
            OnStateChangedDelegate raiseEvent = OnStateChanged;

            // If there are any subscribers to the OnStateChanged event...
            if(raiseEvent != null)
            {
                // ...then raise the event!
                raiseEvent(newState);
            }
        }

        //
        //  LiteNetLib Events
        //

        /// <summary>
        /// Called by LiteNetLib when the client has connected to the server.
        /// </summary>
        /// <param name="peer">The server peer object.</param>
        private void OnConnectedToServer(NetPeer peer)
        {
            server = peer;
            SetNetworkState(NetworkState.Connected);
        }

        /// <summary>
        /// Called by LiteNetLib when the client has been disconnected from the server.
        /// </summary>
        /// <param name="peer">The old server peer object.</param>
        /// <param name="disconnectInfo">The reason for disconnection.</param>
        private void OnDisconnectedFromServer(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            server = null;
            SetNetworkState(NetworkState.Disconnected);
        }

        /// <summary>
        /// Called by LiteNetLib when the client receives data from the server.
        /// </summary>
        /// <param name="peer">The peer that the data was received from.</param>
        /// <param name="reader">The object to use to read the data.</param>
        /// <param name="deliveryMethod">How the data was delivered.</param>
        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Debug.Log($"Reading {reader.AvailableBytes} bytes from server...");
            packetProcessor.ReadAllPackets(reader, peer);
        }

        /// <summary>
        /// Called by the client's NetPacketProcessor when a valid message has been parsed.
        /// </summary>
        /// <typeparam name="MessageType">The Type of the message that was parsed.</typeparam>
        /// <param name="packet">The message that was parsed.</param>
        /// <param name="peer">The peer that the message was received from.</param>
        private void OnPacketRecieved<MessageType>(MessageType packet, NetPeer peer) where MessageType : class, new()
        {
            List<object> foundList;

            // If there are no listeners for this type... EXIT EARLY.
            if (!messageListeners.TryGetValue(typeof(MessageType), out foundList)) return;

            // OTHERWISE...
            // ...call all listeners of this type!
            foreach (object genericListener in foundList)
            {
                ((System.Action<MessageType>)genericListener).Invoke(packet);
            }
        }

        //
        //  Virtual Methods
        //

        /// <summary>
        /// Override this to define extra server functionality.
        /// Called by the parent GameClient during construction.
        /// </summary>
        /// <returns></returns>
        protected virtual GameClientComponent[] GetClientComponents() => GetComponents<GameClientComponent>();

        //
        //  Abstract Methods
        //

        /// <summary>
        /// Override this to defined extra packet / data types in this client's NetPacketProcessor. 
        /// Called by the parent GameClient during construction.
        /// </summary>
        /// <param name="packetProcessor"></param>
        protected abstract void OnRegisterPacketTypes(NetPacketProcessor packetProcessor);
    }
}