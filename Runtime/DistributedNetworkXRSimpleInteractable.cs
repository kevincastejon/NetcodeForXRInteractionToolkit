using Caskev.NetcodeForGameObjects.DistributedAuthority;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
namespace Caskev.NetcodeForXRInteractionToolkit
{
    [RequireComponent(typeof(XRSimpleInteractableNetworkCompliant))]
    public class DistributedNetworkXRSimpleInteractable : DistributedNetworkObject<bool>
    {
        [Tooltip("Three sets of events fired when activation value changes, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkValueEvents<bool> _activationChangeEvents;
        [Tooltip("Three sets of events fired when activated, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkTriggerEvents _activatedEvents;
        [Tooltip("Three sets of events fired when deactivated, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkTriggerEvents _deactivatedEvents;

        private XRSimpleInteractableNetworkCompliant _xrSimpleinteractable;
        /// <summary>
        /// The XRSimpleInteractableConfigurable component to synchronise
        /// </summary>
        public XRSimpleInteractableNetworkCompliant XRSimpleInteractable { get => _xrSimpleinteractable; }
        /// <summary>
        /// Three sets of events fired when activation value changes, organized into a server/client and local/remote oriented way.
        /// </summary>
        public NetworkValueEvents<bool> ActivationChangeEvents { get => _activationChangeEvents; }
        /// <summary>
        /// Three sets of events fired when activated, organized into a server/client and local/remote oriented way.
        /// </summary>
        public NetworkTriggerEvents ActivatedEvents { get => _activatedEvents; }
        /// <summary>
        /// Three sets of events fired when deactivated, organized into a server/client and local/remote oriented way.
        /// </summary>
        public NetworkTriggerEvents DeactivatedEvents { get => _deactivatedEvents; }

        protected override void Awake()
        {
            base.Awake();
            _xrSimpleinteractable = GetComponent<XRSimpleInteractableNetworkCompliant>();
            _xrSimpleinteractable.selectEntered.AddListener(OnLocalSelectEnter);
            _xrSimpleinteractable.selectExited.AddListener(OnLocalSelectExit);
            _xrSimpleinteractable.activated.AddListener(OnActivated);
            _xrSimpleinteractable.deactivated.AddListener(OnDeactivated);
        }

        private void OnActivated(ActivateEventArgs arg0)
        {
            FireEvents(true);
            RelayActivateRpc();
        }
        [Rpc(SendTo.Server)]
        private void RelayActivateRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
            {
                ActivateRpc(new() { Send = new() { Target = RpcTarget.Not(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
            else
            {
                OnActivateRpcCancelRpc(new() { Send = new() { Target = RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void OnActivateRpcCancelRpc(RpcParams rpcParams = default)
        {
            if (_xrSimpleinteractable.IsActivated)
            {
                _xrSimpleinteractable.deactivated.Invoke(new DeactivateEventArgs() { interactableObject = _xrSimpleinteractable, interactorObject = null });
                FireEvents(true);
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void ActivateRpc(RpcParams rpcParams = default)
        {
            if (!_xrSimpleinteractable.IsActivated)
            {
                _xrSimpleinteractable.activated.Invoke(new ActivateEventArgs() { interactableObject = _xrSimpleinteractable, interactorObject = null });
                FireEvents(true);
            }
        }
        private void OnDeactivated(DeactivateEventArgs arg0)
        {
            FireEvents(false);
            RelayDeactivateRpc();
        }
        [Rpc(SendTo.Server)]
        private void RelayDeactivateRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
            {
                DeactivateRpc(new() { Send = new() { Target = RpcTarget.Not(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
            else
            {
                OnDeactivateRpcCancelRpc(new() { Send = new() { Target = RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void OnDeactivateRpcCancelRpc(RpcParams rpcParams = default)
        {
            if (!_xrSimpleinteractable.IsActivated)
            {
                _xrSimpleinteractable.activated.Invoke(new ActivateEventArgs() { interactableObject = _xrSimpleinteractable, interactorObject = null });
                FireEvents(false);
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void DeactivateRpc(RpcParams rpcParams = default)
        {
            if (_xrSimpleinteractable.IsActivated)
            {
                _xrSimpleinteractable.deactivated.Invoke(new DeactivateEventArgs() { interactableObject = _xrSimpleinteractable, interactorObject = null });
                FireEvents(false);
            }
        }
        private void FireEvents(bool activated)
        {
            _activationChangeEvents.CrossSideEvents.OnValueChanged.Invoke(activated);
            if (activated)
            {
                _activatedEvents.CrossSideEvents.OnTrigger.Invoke();
                if (IsServer)
                {
                    _activationChangeEvents.ServerSideEvents.OnValueChanged.Invoke(activated);
                    _activatedEvents.ServerSideEvents.OnTrigger.Invoke();
                    if (IsOwner)
                    {
                        _activationChangeEvents.ServerSideEvents.OnLocalValueChanged.Invoke(activated);
                        _activatedEvents.ServerSideEvents.OnLocalTrigger.Invoke();
                    }
                    else
                    {
                        _activationChangeEvents.ServerSideEvents.OnRemoteValueChanged.Invoke(activated);
                        _activatedEvents.ServerSideEvents.OnRemoteTrigger.Invoke();
                    }
                }
                else
                {
                    _activationChangeEvents.ClientSideEvents.OnValueChanged.Invoke(activated);
                    _activatedEvents.ClientSideEvents.OnTrigger.Invoke();
                    if (IsOwner)
                    {
                        _activationChangeEvents.ClientSideEvents.OnLocalValueChanged.Invoke(activated);
                        _activatedEvents.ClientSideEvents.OnLocalTrigger.Invoke();
                    }
                    else
                    {
                        _activationChangeEvents.ClientSideEvents.OnRemoteValueChanged.Invoke(activated);
                        _activatedEvents.ClientSideEvents.OnRemoteTrigger.Invoke();
                    }
                }
            }
            else
            {
                _deactivatedEvents.CrossSideEvents.OnTrigger.Invoke();
                if (IsServer)
                {
                    _activationChangeEvents.ServerSideEvents.OnValueChanged.Invoke(activated);
                    _deactivatedEvents.ServerSideEvents.OnTrigger.Invoke();
                    if (IsOwner)
                    {
                        _activationChangeEvents.ServerSideEvents.OnLocalValueChanged.Invoke(activated);
                        _deactivatedEvents.ServerSideEvents.OnLocalTrigger.Invoke();
                    }
                    else
                    {
                        _activationChangeEvents.ServerSideEvents.OnRemoteValueChanged.Invoke(activated);
                        _deactivatedEvents.ServerSideEvents.OnRemoteTrigger.Invoke();
                    }
                }
                else
                {
                    _activationChangeEvents.ClientSideEvents.OnValueChanged.Invoke(activated);
                    _deactivatedEvents.ClientSideEvents.OnTrigger.Invoke();
                    if (IsOwner)
                    {
                        _activationChangeEvents.ClientSideEvents.OnLocalValueChanged.Invoke(activated);
                        _deactivatedEvents.ClientSideEvents.OnLocalTrigger.Invoke();
                    }
                    else
                    {
                        _activationChangeEvents.ClientSideEvents.OnRemoteValueChanged.Invoke(activated);
                        _deactivatedEvents.ClientSideEvents.OnRemoteTrigger.Invoke();
                    }
                }
            }
        }
        private void OnLocalSelectEnter(SelectEnterEventArgs arg0)
        {
            switch (CurrentState)
            {
                case DistributedAuthorityState.AUTHORITY:
                    break;
                case DistributedAuthorityState.REMOTE:
                    break;
                case DistributedAuthorityState.REQUESTING:
                    break;
                case DistributedAuthorityState.DECLINING:
                    break;
                default:
                    break;
            }
            RequestOwnership();
        }
        private void OnLocalSelectExit(SelectExitEventArgs arg0)
        {
            switch (CurrentState)
            {
                case DistributedAuthorityState.AUTHORITY:
                    break;
                case DistributedAuthorityState.REMOTE:
                    break;
                case DistributedAuthorityState.REQUESTING:
                    break;
                case DistributedAuthorityState.DECLINING:
                    break;
                default:
                    break;
            }
            DeclineOwnership(_xrSimpleinteractable.IsActivated);
        }
        public override void OnClientDecliningServerSide(DecliningReason decliningReason, bool isDecliningDataProvided, bool decliningData)
        {
            if (isDecliningDataProvided)
            {
                if (decliningData)
                {
                    _xrSimpleinteractable.Activate();
                }
                else
                {
                    _xrSimpleinteractable.Deactivate();
                }
            }
            else if (decliningReason == DecliningReason.SERVER_FORCE_ON_CLIENT_DISCONNECT)
            {
                
            }
        }
    }
}