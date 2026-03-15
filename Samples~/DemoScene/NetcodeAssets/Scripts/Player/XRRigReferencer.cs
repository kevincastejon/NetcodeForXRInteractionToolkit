using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Caskev.NetcodeForXRInteractionToolkitSamples.DemoScene
{
    public class XRRigReferencer : MonoBehaviour
    {
        private static XRRigReferencer _instance;

        [SerializeField] private Transform _root;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _handLeft;
        [SerializeField] private LineRenderer _handLeftNearFarRay;
        [SerializeField] private LineRenderer _handLeftTeleportRay;
        [SerializeField] private Transform _handRight;
        [SerializeField] private LineRenderer _handRightNearFarRay;
        [SerializeField] private LineRenderer _handRightTeleportRay;

        public static XRRigReferencer Instance { get => _instance; }
        public Transform Root { get => _root; }
        public Transform Head { get => _head; }
        public Transform HandLeft { get => _handLeft; }
        public LineRenderer HandLeftNearFarRay { get => _handLeftNearFarRay; }
        public LineRenderer HandLeftTeleportRay { get => _handLeftTeleportRay; }
        public Transform HandRight { get => _handRight; }
        public LineRenderer HandRightNearFarRay { get => _handRightNearFarRay; }
        public LineRenderer HandRightTeleportRay { get => _handRightTeleportRay; }

        private void Awake()
        {
            _instance = this;
        }
    }
}
