using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace Caskev.NetcodeForXRInteractionToolkit
{
    public class XRSimpleInteractableNetworkCompliant : XRSimpleInteractable
    {
        private bool _isActivated;

        public bool IsActivated { get => _isActivated; }
        public void Activate(IXRActivateInteractor interactor = null)
        {
            OnActivated(new() { interactableObject = this, interactorObject = interactor });
        }
        public void Deactivate(IXRActivateInteractor interactor = null)
        {
            OnDeactivated(new() { interactableObject = this, interactorObject = interactor });
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
    }
}