using System.Collections;
using System.Collections.Generic;
using UnityEditor;
namespace Caskev.NetcodeForXRInteractionToolkit.Editor
{
    [CustomEditor(typeof(DistributedNetworkXRGrabInteractable))]
    public class DistributedNetworkXRGrabInteractableEditor : UnityEditor.Editor
    {
        private SerializedProperty _currentState;
        private SerializedProperty _ownershipLockingOnGainPolicy;
        private SerializedProperty _statesEvents;
        private SerializedProperty _ownershipEvents;
        private SerializedProperty _debugParameters;
        private SerializedProperty _lockOwnershipOnTwoHandsGrabbing;
        private SerializedProperty _activationChangeEvents;
        private SerializedProperty _activatedEvents;
        private SerializedProperty _deactivatedEvents;
        private SerializedProperty _automaticallyDeclineOnAsleep;
        private SerializedProperty _onAsleep;
        private SerializedProperty _onAwake;

        private DistributedNetworkXRGrabInteractable _script;

        private void OnEnable()
        {
            _currentState = serializedObject.FindProperty("_currentState");
            _ownershipLockingOnGainPolicy = serializedObject.FindProperty("_ownershipLockingOnGainPolicy");
            _statesEvents = serializedObject.FindProperty("_statesEvents");
            _ownershipEvents = serializedObject.FindProperty("_ownershipEvents");
            _debugParameters = serializedObject.FindProperty("_debugParameters");
            _lockOwnershipOnTwoHandsGrabbing = serializedObject.FindProperty("_lockOwnershipOnTwoHandsGrabbing");
            _activationChangeEvents = serializedObject.FindProperty("_activationChangeEvents");
            _activatedEvents = serializedObject.FindProperty("_activatedEvents");
            _deactivatedEvents = serializedObject.FindProperty("_deactivatedEvents");
            _automaticallyDeclineOnAsleep = serializedObject.FindProperty("_automaticallyDeclineOnAsleep");
            _onAsleep = serializedObject.FindProperty("_onAsleep");
            _onAwake = serializedObject.FindProperty("_onAwake");

            _script = (DistributedNetworkXRGrabInteractable)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_currentState);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(_ownershipLockingOnGainPolicy);
            EditorGUILayout.PropertyField(_statesEvents);
            EditorGUILayout.PropertyField(_ownershipEvents);
            EditorGUILayout.PropertyField(_debugParameters);
            EditorGUILayout.PropertyField(_lockOwnershipOnTwoHandsGrabbing);
            EditorGUILayout.PropertyField(_activationChangeEvents);
            EditorGUILayout.PropertyField(_activatedEvents);
            EditorGUILayout.PropertyField(_deactivatedEvents);
            EditorGUILayout.PropertyField(_automaticallyDeclineOnAsleep);
            EditorGUILayout.PropertyField(_onAsleep);
            EditorGUILayout.PropertyField(_onAwake);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
