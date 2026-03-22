using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace Caskev.NetcodeForXRInteractionToolkit
{
    public class XRGrabInteractableNetworkCompliant : XRGrabInteractable
    {
        private bool _isActivated;
        private Transform _parent;
        public bool IsActivated { get => _isActivated; }
        protected override void Awake()
        {
            base.Awake();
            retainTransformParent = false;
            _parent = transform.parent;
        }
        public void Activate(IXRActivateInteractor interactor = null)
        {
            OnActivated(new() { interactableObject = this, interactorObject = interactor });
        }
        public void Deactivate(IXRActivateInteractor interactor = null)
        {
            OnDeactivated(new() { interactableObject = this, interactorObject = interactor });
        }
        protected override void Grab()
        {
            //base.Grab();
            //transform.SetParent(_parent);
        }
        protected override void Drop()
        {
            retainTransformParent = false;
            base.Drop();
        }
        protected override void SetupRigidbodyGrab(Rigidbody rigidbody)
        {
        }
        protected override void SetupRigidbodyDrop(Rigidbody rigidbody)
        {
        }
        protected override void OnActivated(ActivateEventArgs args)
        {
            _isActivated = true;
            base.OnActivated(args);
        }
        protected override void OnDeactivated(DeactivateEventArgs args)
        {
            _isActivated = false;
            base.OnDeactivated(args);
        }
        protected override void Reset()
        {
            base.Reset();
            retainTransformParent = false;
        }
    }
}