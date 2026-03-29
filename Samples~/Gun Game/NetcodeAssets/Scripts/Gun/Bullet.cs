using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private GameObject _visuals;
    private Rigidbody _body;

    public Rigidbody Body { get => _body; set => _body = value; }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }
    public void Shoot(Vector3 force)
    {
        ActivateRpc();
        _body.isKinematic = false;
        _body.AddForce(force, ForceMode.Impulse);
    }
    [Rpc(SendTo.Everyone)]
    private void ActivateRpc()
    {
        _visuals.SetActive(true);
    }
}
