using Caskev.NetcodeForXRInteractionToolkit;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NetworkGun : NetworkBehaviour
{
    [SerializeField] private Transform _bulletSpawnParent;
    private XRGrabInteractableNetworkCompliant _interactable;
    private NetworkBulletsPool _magazine;

    private void Awake()
    {
        _interactable = GetComponent<XRGrabInteractableNetworkCompliant>();
        _magazine = GetComponent<NetworkBulletsPool>();
        _bulletSpawnParent = _bulletSpawnParent ? _bulletSpawnParent : transform;
        _interactable.activated.AddListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs arg0)
    {
        if (!IsOwner)
        {
            return;
        }
        Bullet bullet = _magazine.GetBullet().GetComponent<Bullet>();
        bullet.Body.position = _bulletSpawnParent.position;
        bullet.Body.rotation = _bulletSpawnParent.rotation;
        bullet.Shoot(_bulletSpawnParent.forward * 5f);
    }
}
