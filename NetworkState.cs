using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BobboNet.Networking.Unity
{
    /// <summary>
    /// The current state of a GameClient's network connection.
    /// </summary>
    public enum NetworkState
    {
        Unknown,
        Disconnected,
        Connecting,
        Connected
    }

    public delegate void OnStateChangedDelegate(NetworkState newState);
}
