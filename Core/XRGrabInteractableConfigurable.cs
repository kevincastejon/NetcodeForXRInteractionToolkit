using KevinCastejon.MoreAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace KevinCastejon.NetcodeForXRInteractionToolkit.Examples
{
    public class XRGrabInteractableConfigurable : XRGrabInteractable
    {
        [SerializeField] private bool _useDefaultRigidbodySetupOnGrab;
        [SerializeField] private bool _useDefaultRigidbodySetupOnDrop;
        [SerializeField] private bool _allowHoveredActivate;
        [SerializeField][ShowPropIf("_allowHoveredActivate", false)] private bool _deactivateOnSelectExited;
        [SerializeField][ShowPropIf("_allowHoveredActivate")] private bool _deactivateOnHoverExited;
        private bool _isActivated;

        public bool IsActivated { get => _isActivated; }
        public bool UseDefaultRigidbodySetupOnGrab { get => _useDefaultRigidbodySetupOnGrab; }
        public bool UseDefaultRigidbodySetupOnDrop { get => _useDefaultRigidbodySetupOnDrop; }
        public bool AllowHoveredActivate { get => _allowHoveredActivate; }
        public bool DeactivateOnSelectExited { get => _deactivateOnSelectExited; }
        public bool DeactivateOnHoverExited { get => _deactivateOnHoverExited; }

        protected override void SetupRigidbodyGrab(Rigidbody rigidbody)
        {
            if (_useDefaultRigidbodySetupOnGrab)
            {
                base.SetupRigidbodyGrab(rigidbody);
            }
        }
        protected override void SetupRigidbodyDrop(Rigidbody rigidbody)
        {
            if (_useDefaultRigidbodySetupOnDrop)
            {
                base.SetupRigidbodyDrop(rigidbody);
            }
        }
        protected override void OnActivated(ActivateEventArgs args)
        {
            if (!_isActivated && (args.interactorObject == null || _allowHoveredActivate || isSelected))
            {
                _isActivated = true;
                base.OnActivated(args);
            }
        }
        protected override void OnDeactivated(DeactivateEventArgs args)
        {
            if (_isActivated && (args.interactorObject == null || _allowHoveredActivate || isSelected))
            {
                _isActivated = false;
                base.OnDeactivated(args);
            }
        }
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            if (!_allowHoveredActivate && _deactivateOnSelectExited && _isActivated)
            {
                base.OnDeactivated(new() { interactableObject = (IXRActivateInteractable)args.interactableObject, interactorObject = (IXRActivateInteractor)args.interactorObject });
            }
            base.OnSelectExited(args);
        }
        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            if (_allowHoveredActivate && _deactivateOnHoverExited)
            {
                base.OnDeactivated(new() { interactableObject = (IXRActivateInteractable)args.interactableObject, interactorObject = (IXRActivateInteractor)args.interactorObject });
            }
            base.OnHoverExited(args);
        }
    }
}