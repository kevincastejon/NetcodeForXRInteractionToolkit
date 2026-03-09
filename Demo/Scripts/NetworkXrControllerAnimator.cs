using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
namespace KevinCastejon.NetcodeForXRInteractionToolkit.Examples
{
    public class NetworkXrControllerAnimator : NetworkBehaviour
    {
        [Header("Thumbstick")]
        [SerializeField]
        Transform m_ThumbstickTransform;

        [SerializeField]
        Vector2 m_StickRotationRange = new Vector2(30f, 30f);

        [SerializeField]
        XRInputValueReader<Vector2> m_StickInput = new XRInputValueReader<Vector2>("Thumbstick");

        [Header("Trigger")]
        [SerializeField]
        Transform m_TriggerTransform;

        [SerializeField]
        Vector2 m_TriggerXAxisRotationRange = new Vector2(0f, -15f);

        [SerializeField]
        XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

        [Header("Grip")]
        [SerializeField]
        Transform m_GripTransform;

        [SerializeField]
        Vector2 m_GripRightRange = new Vector2(-0.0125f, -0.011f);

        [SerializeField]
        XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");
        private NetworkVariable<Vector4> _controllerInput = new NetworkVariable<Vector4>(Vector4.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            if (!IsOwner)
            {
                _controllerInput.OnValueChanged += OnRemoteValueChanged;
            }
        }

        private void OnRemoteValueChanged(Vector4 previousValue, Vector4 newValue)
        {
            Vector2 stickVal = new(newValue.x, newValue.y);
            float triggerVal = newValue.z;
            float gripVal = newValue.w;
            m_ThumbstickTransform.localRotation = Quaternion.Euler(-stickVal.y * m_StickRotationRange.x, 0f, -stickVal.x * m_StickRotationRange.y);
            m_TriggerTransform.localRotation = Quaternion.Euler(Mathf.Lerp(m_TriggerXAxisRotationRange.x, m_TriggerXAxisRotationRange.y, triggerVal), 0f, 0f);
            var currentPos = m_GripTransform.localPosition;
            m_GripTransform.localPosition = new Vector3(Mathf.Lerp(m_GripRightRange.x, m_GripRightRange.y, gripVal), currentPos.y, currentPos.z);
        }

        private void Update()
        {
            if (IsSpawned && IsOwner)
            {
                Vector2 stickVal = m_StickInput.ReadValue();
                float triggerVal = m_TriggerInput.ReadValue();
                float gripVal = m_GripInput.ReadValue();
                _controllerInput.Value = new Vector4(stickVal.x, stickVal.y, triggerVal, gripVal);
            }
        }
    }
}
