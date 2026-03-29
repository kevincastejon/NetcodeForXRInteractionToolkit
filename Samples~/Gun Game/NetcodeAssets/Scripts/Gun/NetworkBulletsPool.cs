using Caskev.NetcodeForXRInteractionToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
public class Magazine : INetworkSerializable, IEquatable<Magazine>
{
    private List<NetworkObjectReference> _bullets = new();

    public List<NetworkObjectReference> Bullets { get => _bullets; set => _bullets = value; }

    public bool Equals(Magazine other)
    {
        if (other._bullets.Count != _bullets.Count)
        {
            return false;
        }
        bool same = true;
        for (int i = 0; i < _bullets.Count; i++)
        {
            if (!_bullets[i].Equals(other._bullets[i]))
            {
                same = false;
                break;
            }
        }
        return same;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(_bullets.Count);
            for (int i = 0; i < _bullets.Count; i++)
            {
                writer.WriteValueSafe(_bullets[i]);
            }
        }
        if (serializer.IsReader)
        {
            FastBufferReader reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out int count);
            _bullets.Clear();
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out NetworkObjectReference bullet);
                _bullets.Add(bullet);
            }
        }
    }
}
public class NetworkBulletsPool : NetworkBehaviour
{
    [SerializeField] private NetworkObject _bulletNetworkPrefab;
    [SerializeField] private uint _bulletsCount = 10;
    private NetworkVariable<Magazine> _magazine = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            RequestBulletSpawnRpc(10);
        }
    }
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        base.OnOwnershipChanged(previous, current);
        if (IsServer && current == OwnerClientId)
        {
            foreach (var item in _magazine.Value.Bullets)
            {
                if(item.TryGet(out NetworkObject bullet))
                {
                    bullet.ChangeOwnership(OwnerClientId);
                }
            }
        }
    }
    public NetworkObject GetBullet()
    {
        if (!IsOwner)
        {
            throw new Exception("This method can called by the owner only!");
        }
        _magazine.Value.Bullets[0].TryGet(out NetworkObject bullet);
        _magazine.Value.Bullets.RemoveAt(0);
        _magazine.Value = _magazine.Value;
        RequestBulletSpawnRpc(1);
        return bullet;
    }
    [Rpc(SendTo.Server)]
    private void RequestBulletSpawnRpc(int bulletsCount, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == OwnerClientId)
        {
            NetworkObject[] bullets = new NetworkObject[bulletsCount];
            for (int i = 0; i < bulletsCount; i++)
            {
                bullets[i] = Instantiate(_bulletNetworkPrefab);
                bullets[i].SpawnWithOwnership(rpcParams.Receive.SenderClientId, true);
            }
            GiveBulletToClientRpc(bullets.Select(x => new NetworkObjectReference(x)).ToArray());
        }
    }
    [Rpc(SendTo.Owner)]
    private void GiveBulletToClientRpc(NetworkObjectReference[] networkObjectReferences)
    {
        foreach (var item in networkObjectReferences)
        {
            if (item.TryGet(out NetworkObject bullet))
            {
                _magazine.Value.Bullets.Add(bullet);
                _magazine.Value = _magazine.Value;
            }
        }
    }
}
