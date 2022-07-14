using BobboNet.Player;
using BobboNet.Networking;

namespace BobboNet.Networking.Unity
{
    public static class Extensions
    {
        public static GroundedState ConvertToUnityType(this GenericGroundAnimationType netVersion)
        {
            switch (netVersion)
            {
                case GenericGroundAnimationType.IsGrounded:
                    return GroundedState.Grounded;
                case GenericGroundAnimationType.InAir:
                    return GroundedState.InAir;
                case GenericGroundAnimationType.IsSliding:
                    return GroundedState.Sliding;


                default:
                    return GroundedState.Grounded;
            }
        }

        public static GenericGroundAnimationType ConvertToNetworkType(this GroundedState unityType)
        {
            switch (unityType)
            {
                case GroundedState.Grounded:
                    return GenericGroundAnimationType.IsGrounded;
                case GroundedState.Sliding:
                    return GenericGroundAnimationType.IsSliding;
                case GroundedState.InAir:
                    return GenericGroundAnimationType.InAir;

                default:
                    return GenericGroundAnimationType.IsGrounded;
            }
        }

        public static UnityEngine.Vector3 ConvertToUnityType(this NetVec3 networkType) => new UnityEngine.Vector3(networkType.X, networkType.Y, networkType.Z);

        public static NetVec3 ConvertToNetworkType(this UnityEngine.Vector3 unityType) => new NetVec3() { X = unityType.x, Y = unityType.y, Z = unityType.z };
    }
}

