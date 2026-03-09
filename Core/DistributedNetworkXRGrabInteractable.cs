using KevinCastejon.NetcodeForGameObjectsDistributedAuthority;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace KevinCastejon.NetcodeForXRInteractionToolkit.Examples
{
    public class OwnershipRemotelyLockedFilter : IXRSelectFilter
    {
        public bool allowSelect = true;
        public bool canProcess { get => true; }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return allowSelect;
        }
    }
    [RequireComponent(typeof(XRGrabInteractableConfigurable))]
    public class DistributedNetworkXRGrabInteractable : DistributedNetworkRigidbody
    {
        [Tooltip("Will lock ownership on two-hands grabbing")]
        [SerializeField] private bool _lockOwnershipOnTwoHandsGrabbing;
        [Tooltip("Three sets of events fired when activation value changes, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkValueEvents<bool> _activationChangeEvents;
        [Tooltip("Three sets of events fired when activated, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkTriggerEvents _activatedEvents;
        [Tooltip("Three sets of events fired when deactivated, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkTriggerEvents _deactivatedEvents;
        private XRGrabInteractableConfigurable _xrGrabinteractable;
        private OwnershipRemotelyLockedFilter _selectFilter = new();
        /// <summary>
        /// The XRGrabInteractableConfigurable component to synchronise
        /// </summary>
        public XRGrabInteractableConfigurable XRGrabInteractable { get => _xrGrabinteractable; }
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
            _xrGrabinteractable = GetComponent<XRGrabInteractableConfigurable>();
            _xrGrabinteractable.selectEntered.AddListener(OnLocalGrab);
            _xrGrabinteractable.selectExited.AddListener(OnLocalRelease);
            _xrGrabinteractable.activated.AddListener(OnActivated);
            _xrGrabinteractable.deactivated.AddListener(OnDeactivated);
            _xrGrabinteractable.selectFilters.Add(_selectFilter); 
        }

        private void OnActivated(ActivateEventArgs arg0)
        {
            FireEvents(true);
            RelayActivateRpc();
        }
        [Rpc(SendTo.Server)]
        private void RelayActivateRpc(RpcParams rpcParams = default)
        {
            if (_xrGrabinteractable.AllowHoveredActivate || rpcParams.Receive.SenderClientId == OwnerClientId)
            {
                ActivateRpc(new() { Send = new() { Target = RpcTarget.Not(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
            else
            {
                OnActivateCancelRpc(new() { Send = new() { Target = RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void OnActivateCancelRpc(RpcParams rpcParams = default)
        {
            if (_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.deactivated.Invoke(new DeactivateEventArgs() { interactableObject = _xrGrabinteractable, interactorObject = null });
                FireEvents(true);
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void ActivateRpc(RpcParams rpcParams = default)
        {
            if (!_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.activated.RemoveListener(OnActivated);
                ((IXRActivateInteractable)_xrGrabinteractable).OnActivated(new() { interactableObject = _xrGrabinteractable, interactorObject = null });
                _xrGrabinteractable.activated.AddListener(OnActivated);
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
            if (_xrGrabinteractable.AllowHoveredActivate || rpcParams.Receive.SenderClientId == OwnerClientId)
            {
                DeactivateRpc(new() { Send = new() { Target = RpcTarget.Not(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
            else
            {
                OnDeactivateCancelRpc(new() { Send = new() { Target = RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void OnDeactivateCancelRpc(RpcParams rpcParams = default)
        {
            if (!_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.activated.Invoke(new ActivateEventArgs() { interactableObject = _xrGrabinteractable, interactorObject = null });
                FireEvents(false);
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void DeactivateRpc(RpcParams rpcParams = default)
        {
            if (_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.deactivated.RemoveListener(OnDeactivated);
                ((IXRActivateInteractable)_xrGrabinteractable).OnDeactivated(new() { interactableObject = _xrGrabinteractable, interactorObject = null });
                _xrGrabinteractable.deactivated.AddListener(OnDeactivated);
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
        private void OnLocalGrab(SelectEnterEventArgs arg0)
        {
            if (IsServer && IsOwnershipLocked && !IsOwner)
            {
                CancelGrab();
                return;
            }
            bool twoHandsGrab = _xrGrabinteractable.IsSelectedByLeft() && _xrGrabinteractable.IsSelectedByRight();
            SetRigidbodyForGrab();
            RequestOwnership(_lockOwnershipOnTwoHandsGrabbing && twoHandsGrab);
        }
        private void OnLocalRelease(SelectExitEventArgs arg0)
        {

            SetRigidbodyForUnGrab();
            if (_lockOwnershipOnTwoHandsGrabbing && _xrGrabinteractable.isSelected)
            {
                UnlockOwnership();
            }
        }
        public override void OnOwnershipGained(ulong clientId)
        {
            base.OnOwnershipGained(clientId);
            if (clientId != NetworkManager.LocalClientId) { return; }
        }
        public override void OnOwnershipLost(ulong clientId)
        {
            base.OnOwnershipLost(clientId);
            if (clientId != NetworkManager.LocalClientId) { return; }
        }
        public override void OnEnterAuthority()
        {
            //base.OnEnterAuthority(); // Do not call base as it will mess with rigidbody kinematic state
            OwnerClientNetworkTransform.enabled = true;
            if (_xrGrabinteractable.isSelected)
            {
                SetRigidbodyForGrab();
            }
            else
            {
                SetRigidbodyForUnGrab();
            }
        }
        public override void OnEnterRemote()
        {
            base.OnEnterRemote();
            SetRigidbodyForRemote(OwnerClientNetworkTransform.Interpolate);
            if (_xrGrabinteractable.isSelected)
            {
                CancelGrab();
            }
        }
        public override void OnEnterRequesting()
        {
            base.OnEnterRequesting();
        }
        public override void OnEnterDeclining()
        {
            base.OnEnterDeclining();
        }
        private void SetRigidbodyForGrab()
        {
            Rigidbody.isKinematic = _xrGrabinteractable.movementType == XRBaseInteractable.MovementType.Kinematic || _xrGrabinteractable.movementType == XRBaseInteractable.MovementType.Instantaneous;
            Rigidbody.useGravity = false;
            Rigidbody.linearDamping = 0f;
            Rigidbody.angularDamping = 0f;
            Rigidbody.interpolation = OriginalInterpolation;
        }
        private void SetRigidbodyForUnGrab()
        {
            Rigidbody.isKinematic = OriginalKinematicState;
            Rigidbody.useGravity = OriginalUseGravityState;
            Rigidbody.linearDamping = OriginalDrag;
            Rigidbody.angularDamping = OriginalAngularDrag;
            Rigidbody.interpolation = OriginalInterpolation;
        }
        private void SetRigidbodyForRemote(bool useNetworkTransformInterpolation)
        {
            Rigidbody.isKinematic = true;
            Rigidbody.interpolation = useNetworkTransformInterpolation ? RigidbodyInterpolation.None : OriginalInterpolation;
        }
        private void CancelGrab()
        {
            _xrGrabinteractable.selectExited.RemoveListener(OnLocalRelease);
            for (int i = _xrGrabinteractable.interactorsSelecting.Count - 1; i >= 0; i--)
            {
                _xrGrabinteractable.interactionManager.SelectExit(_xrGrabinteractable.interactorsSelecting[i], _xrGrabinteractable);
            }
            _xrGrabinteractable.selectExited.AddListener(OnLocalRelease);
        }
        public override void OnOwnershipLocked(ulong clientId)
        {
            base.OnOwnershipLocked(clientId);
            if (!IsOwner)
            {
                _selectFilter.allowSelect = false;
            }
        }
        public override void OnOwnershipUnlocked(ulong clientId)
        {
            base.OnOwnershipUnlocked(clientId);
            _selectFilter.allowSelect = true;
        }
    }
}