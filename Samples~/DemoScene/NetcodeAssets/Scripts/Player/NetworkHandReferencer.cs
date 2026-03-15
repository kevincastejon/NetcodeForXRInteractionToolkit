using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Caskev.NetcodeForXRInteractionToolkitSamples.DemoScene
{
    public class NetworkHandReferencer : MonoBehaviour
    {
        [SerializeField] private NetworkLineRenderer _nearFarNetworkRay;
        [SerializeField] private NetworkLineRenderer _teleportNetworkRay;
        private NetworkObject _networkObject;

        public NetworkObject NetworkObject { get => _networkObject; }
        public NetworkLineRenderer NearFarNetworkRay { get => _nearFarNetworkRay; }
        public NetworkLineRenderer TeleportNetworkRay { get => _teleportNetworkRay; }

        private void Awake()
        {
            _networkObject = GetComponent<NetworkObject>();
        }
    }
}
