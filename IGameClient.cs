using LiteNetLib;

namespace BobboNet.Networking.Unity
{
    public interface IGameClient
    {
        /// <summary>
        /// Connect this client to a server.
        /// </summary>
        /// <param name="address">The network address of the server.</param>
        /// <param name="port">The open network port of the server.</param>
        /// <param name="key">An optional key to provide the server when connecting.</param>
        void Connect(string address, int port, string key = "");

        /// <summary>
        /// Disconnect this client from the server.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send a message to the server.
        /// </summary>
        /// <typeparam name="DataType">The Type of message to send. This MUST have been registered in the packet processor via OnRegisterPacketTypes()</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="channel">The channel to send the message on.</param>
        /// <param name="deliveryMethod">How to send the message.</param>
        void Send<DataType>(DataType message, byte channel, DeliveryMethod deliveryMethod) where DataType : class, new();

        /// <summary>
        /// Subscribe a listener for when the client receives a message of type MessageType.
        /// </summary>
        /// <typeparam name="MessageType">The type of message to listen for.</typeparam>
        /// <param name="action">The action to call once a message of MessageType is received.</param>
        void OnGetMessageSubscribe<MessageType>(System.Action<MessageType> action) where MessageType : class, new();

        /// <summary>
        /// Unsubscribe a listener for when the client receives a message of type MessageType.
        /// </summary>
        /// <typeparam name="MessageType">The type of message to stop listening for.</typeparam>
        /// <param name="action">The action that was previously subscribed.</param>
        /// <returns>true if removed, false if the given action was not found.</returns>
        bool OnGetMessageUnsubscribe<MessageType>(System.Action<MessageType> action);

        /// <returns>The current network state</returns>
        NetworkState GetState();

        /// <summary>
        /// Subscribe a listener for when the client's network state changes.
        /// </summary>
        /// <param name="action">The action to call when the network state changes</param>
        void OnStateChangeSubscribe(OnStateChangedDelegate action);

        /// <summary>
        /// Unsubscribe a listener for when the client's network state changes.
        /// </summary>
        /// <param name="action">The action that was previous subscribed with OnStateChangeSubscribe()</param>
        void OnStateChangeUnsubscribe(OnStateChangedDelegate action);
    }
}
