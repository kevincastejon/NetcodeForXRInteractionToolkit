using Caskev.NetcodeForGameObjects.DistributedAuthority;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace Caskev.NetcodeForXRInteractionToolkit
{
    public struct XRGrabInteractableData : INetworkSerializable
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public bool activated;

        public static XRGrabInteractableData FromRigidbody(Rigidbody rigidbody)
        {
            return new XRGrabInteractableData() { position = rigidbody.position, rotation = rigidbody.rotation, scale = rigidbody.transform.localScale, linearVelocity = rigidbody.linearVelocity, angularVelocity = rigidbody.angularVelocity };
        }
        public void FeedXRGrabInteractable(XRGrabInteractableNetworkCompliant interactable)
        {
            if (!interactable.TryGetComponent(out Rigidbody rb))
            {
                return;
            }
            rb.position = position;
            rb.rotation = rotation;
            rb.transform.localScale = scale;
            rb.linearVelocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
            if (activated)
            {
                interactable.Activate();
            }
            else
            {
                interactable.Deactivate();
            }
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out position);
                reader.ReadValueSafe(out rotation);
                reader.ReadValueSafe(out scale);
                reader.ReadValueSafe(out linearVelocity);
                reader.ReadValueSafe(out angularVelocity);
            }
            if (serializer.IsWriter)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(position);
                writer.WriteValueSafe(rotation);
                writer.WriteValueSafe(scale);
                writer.WriteValueSafe(linearVelocity);
                writer.WriteValueSafe(angularVelocity);
            }
        }
    }
    public class OwnershipRemotelyLockedFilter : IXRSelectFilter
    {
        public bool allowSelect = true;
        public bool canProcess { get => true; }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return allowSelect;
        }
    }
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(XRGrabInteractableNetworkCompliant))]
    [GenerateSerializationForType(typeof(XRGrabInteractableData))]
    public class DistributedNetworkXRGrabInteractable : DistributedNetworkObject<XRGrabInteractableData>
    {
        [Tooltip("Will lock ownership on two-hands grabbing")]
        [SerializeField] private bool _lockOwnershipOnTwoHandsGrabbing;
        [Tooltip("Three sets of events fired when activation value changes, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkValueEvents<bool> _activationChangeEvents;
        [Tooltip("Three sets of events fired when activated, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkTriggerEvents _activatedEvents;
        [Tooltip("Three sets of events fired when deactivated, organized into a server/client and local/remote oriented way.")]
        [SerializeField] private NetworkTriggerEvents _deactivatedEvents;
        [SerializeField] private bool _automaticallyDeclineOnAsleep;
        [SerializeField] private UnityEvent _onAsleep;
        [SerializeField] private UnityEvent _onAwake;
        private NetworkTransform _networkTransform;
        private Rigidbody _rigidbody;
        private bool _originalKinematicState;
        private bool _originalUseGravityState;
        private float _originalDrag;
        private float _originalAngularDrag;
        private RigidbodyInterpolation _originalInterpolation;
        private bool _lastSleepState;
        private XRGrabInteractableNetworkCompliant _xrGrabinteractable;
        private OwnershipRemotelyLockedFilter _selectFilter = new();
        public Rigidbody Rigidbody { get => _rigidbody; }
        public bool AutomaticallyDeclineOnAsleep { get => _automaticallyDeclineOnAsleep; set => _automaticallyDeclineOnAsleep = value; }
        public bool OriginalKinematicState { get => _originalKinematicState; }
        public NetworkTransform NetworkTransform { get => _networkTransform; }
        public bool OriginalUseGravityState { get => _originalUseGravityState; }
        public float OriginalDrag { get => _originalDrag; }
        public float OriginalAngularDrag { get => _originalAngularDrag; }
        public RigidbodyInterpolation OriginalInterpolation { get => _originalInterpolation; }
        /// <summary>
        /// The XRGrabInteractableConfigurable component to synchronise
        /// </summary>
        public XRGrabInteractableNetworkCompliant XRGrabInteractable { get => _xrGrabinteractable; }
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
            _networkTransform = GetComponent<NetworkTransform>();
            _rigidbody = GetComponent<Rigidbody>();
            _originalKinematicState = _rigidbody.isKinematic;
            _originalUseGravityState = _rigidbody.useGravity;
            _originalDrag = _rigidbody.linearDamping;
            _originalAngularDrag = _rigidbody.angularDamping;
            _originalInterpolation = _rigidbody.interpolation;
            _xrGrabinteractable = GetComponent<XRGrabInteractableNetworkCompliant>();
            _xrGrabinteractable.selectEntered.AddListener(OnLocalGrab);
            _xrGrabinteractable.selectExited.AddListener(OnLocalRelease);
            _xrGrabinteractable.activated.AddListener(OnActivated);
            _xrGrabinteractable.deactivated.AddListener(OnDeactivated);
            _xrGrabinteractable.selectFilters.Add(_selectFilter);

        }
        private void FixedUpdate()
        {
            if (_rigidbody.isKinematic)
            {
                return;
            }
            bool newSleepState = _rigidbody.IsSleeping();
            if (_lastSleepState != newSleepState)
            {
                _lastSleepState = newSleepState;
                if (newSleepState)
                {
                    if (_automaticallyDeclineOnAsleep && CurrentState != DistributedAuthorityState.REMOTE)
                    {
                        DeclineOwnership(XRGrabInteractableData.FromRigidbody(_rigidbody));
                    }
                    _onAsleep.Invoke();
                }
                else
                {
                    _onAwake.Invoke();
                }
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkTransform.enabled = true;
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
                OnActivateCancelRpc(new() { Send = new() { Target = RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) } });
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void OnActivateCancelRpc(RpcParams rpcParams = default)
        {
            if (_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.Deactivate();
                FireEvents(false);
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void ActivateRpc(RpcParams rpcParams = default)
        {
            if (!_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.activated.RemoveListener(OnActivated);
                _xrGrabinteractable.Activate();
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
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
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
                _xrGrabinteractable.Activate();
                FireEvents(true);
            }
        }
        [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
        private void DeactivateRpc(RpcParams rpcParams = default)
        {
            if (_xrGrabinteractable.IsActivated)
            {
                _xrGrabinteractable.deactivated.RemoveListener(OnDeactivated);
                _xrGrabinteractable.Deactivate();
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
            if (clientId == NetworkManager.LocalClientId)
            {
                _networkTransform.enabled = true;
            }
        }
        public override void OnOwnershipLost(ulong clientId)
        {
            base.OnOwnershipLost(clientId);
            if (clientId == NetworkManager.LocalClientId)
            {
                _networkTransform.enabled = IsServer || CurrentState == DistributedAuthorityState.REMOTE;
            }
        }
        public override void OnEnterNone()
        {
            _networkTransform.enabled = false;
            _rigidbody.isKinematic = _originalKinematicState;
        }
        public override void OnEnterAuthority()
        {
            NetworkTransform.enabled = true;
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
            _networkTransform.enabled = true;
            _rigidbody.isKinematic = true;
            SetRigidbodyForRemote(NetworkTransform.Interpolate);
            if (_xrGrabinteractable.isSelected)
            {
                CancelGrab();
            }
        }
        public override void OnEnterRequesting()
        {
            base.OnEnterRequesting();
            _networkTransform.enabled = IsOwner;
        }
        public override void OnEnterDeclining()
        {
            base.OnEnterDeclining();
            _networkTransform.enabled = IsOwner;
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
        public override void OnClientDecliningServerSide(DecliningReason decliningReason, bool isDecliningDataProvided, XRGrabInteractableData decliningData)
        {
            if (isDecliningDataProvided)
            {
                decliningData.FeedXRGrabInteractable(_xrGrabinteractable);
            }
            else if (decliningReason == DecliningReason.SERVER_FORCE_ON_CLIENT_DISCONNECT)
            {
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }
}