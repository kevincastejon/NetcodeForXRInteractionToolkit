using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Caskev.NetcodeForXRInteractionToolkitSamples.DemoScene
{
    public class VRPlayer : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _headPrefab;
        [SerializeField] private NetworkHandReferencer _handLeftPrefab;
        [SerializeField] private NetworkHandReferencer _handRightPrefab;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                transform.SetParent(XRRigReferencer.Instance.Root);
                transform.localPosition = Vector3.zero;
                RequestBodyPartsInstantiateRpc();
            }
        }
        [Rpc(SendTo.Server)]
        private void RequestBodyPartsInstantiateRpc(RpcParams rpcParams = default)
        {
            NetworkObject head = Instantiate(_headPrefab);
            head.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
            NetworkObject handLeft = Instantiate(_handLeftPrefab).NetworkObject;
            handLeft.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
            NetworkObject handRight = Instantiate(_handRightPrefab).NetworkObject;
            handRight.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
            OnBodyPartsInstantiatedRpc(new(head), new(handLeft), new(handRight));
        }
        [Rpc(SendTo.Everyone)]
        private void OnBodyPartsInstantiatedRpc(NetworkObjectReference headId, NetworkObjectReference handLeftId, NetworkObjectReference handRightId, RpcParams rpcParams = default)
        {
            headId.TryGet(out NetworkObject head);
            handLeftId.TryGet(out NetworkObject handLeft);
            handRightId.TryGet(out NetworkObject handRight);
            if (head.OwnerClientId == NetworkManager.LocalClientId)
            {
                head.transform.SetParent(XRRigReferencer.Instance.Head);
                head.transform.localPosition = Vector3.zero;
                head.transform.localRotation = Quaternion.identity;
                TurnOffRenderers(head.gameObject);
                handLeft.transform.SetParent(XRRigReferencer.Instance.HandLeft);
                handLeft.transform.localPosition = Vector3.zero;
                handLeft.transform.localRotation = Quaternion.identity;
                TurnOffRenderers(handLeft.gameObject);
                NetworkHandReferencer handLRef = handLeft.GetComponent<NetworkHandReferencer>();
                handLRef.NearFarNetworkRay.SourceLineRenderer = XRRigReferencer.Instance.HandLeftNearFarRay;
                handLRef.TeleportNetworkRay.SourceLineRenderer = XRRigReferencer.Instance.HandLeftTeleportRay;
                handRight.transform.SetParent(XRRigReferencer.Instance.HandRight);
                handRight.transform.localPosition = Vector3.zero;
                handRight.transform.localRotation = Quaternion.identity;
                TurnOffRenderers(handRight.gameObject);
                NetworkHandReferencer handRRef = handRight.GetComponent<NetworkHandReferencer>();
                handRRef.NearFarNetworkRay.SourceLineRenderer = XRRigReferencer.Instance.HandRightNearFarRay;
                handRRef.TeleportNetworkRay.SourceLineRenderer = XRRigReferencer.Instance.HandRightTeleportRay;
            }
        }
        private void TurnOffRenderers(GameObject hierarchy)
        {
            Renderer[] renderers = hierarchy.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }
        }
    }
}
