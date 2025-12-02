using Fusion;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    [SerializeField] protected ShootType shootType;
    [SerializeField] protected Transform cameraPos;
    [SerializeField] protected LayerMask hitLayers;

    [SerializeField] protected Transform shootPoint;
    [SerializeField] protected NetworkPrefabRef bulletPrefab;

    [SerializeField] protected int damage;
    [SerializeField] protected float range;
    [SerializeField] protected int currentAmmo;
    [SerializeField] protected float fireRate;

    public abstract void RigidBodyShoot();
    public abstract void RpcRaycastShoot(RpcInfo info = default);
    public abstract void RpcReload();
}

public enum ShootType
{
    RigidBody,
    Raycast
}