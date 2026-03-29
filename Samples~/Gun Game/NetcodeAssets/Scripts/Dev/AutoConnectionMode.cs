using UnityEngine;
public enum AutoConnectionModes
{
    HOST,
    CLIENT,
}
[CreateAssetMenu]
public class AutoConnectionMode : ScriptableObject
{
    [SerializeField] private AutoConnectionModes _autoConnectionMode;

    public AutoConnectionModes Value => _autoConnectionMode;
}
