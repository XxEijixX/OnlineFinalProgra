using Fusion;
using UnityEngine;

public class Handgun : Weapon
{
    private float nextTimeToFire = 0f;

    [Networked] public PlayerRef WeaponOwner { get; set; }

    public override void Spawned()
    {
        WeaponOwner = Object.InputAuthority;
    }

    public override void RigidBodyShoot()
    {
        if (!Object.HasInputAuthority) return;
        if (Time.time >= nextTimeToFire)
        {
            RpcRequestRigidBodyShoot(shootPoint.position, shootPoint.rotation);
            nextTimeToFire = Time.time + (1f / fireRate);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcRequestRigidBodyShoot(Vector3 pos, Quaternion rot)
    {
        NetworkObject bulletInstance = Runner.Spawn(bulletPrefab, pos, rot);

        if (bulletInstance.TryGetComponent(out Projetile projectile))
        {
            projectile.damage = damage;
            projectile.Shooter = WeaponOwner;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public override void RpcRaycastShoot(RpcInfo info = default)
    {
        if (Time.time >= nextTimeToFire)
        {
            if (Physics.Raycast(cameraPos.position, cameraPos.forward, out RaycastHit hitInfo, range, hitLayers))
            {
                Debug.Log("You hit: " + hitInfo.collider.name);
            }

            if (hitInfo.collider.TryGetComponent<Health>(out Health health))
            {
              //  health.RpcTakeDamage(damage, WeaponOwner);
            }

            nextTimeToFire = Time.time + (1f / fireRate);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public override void RpcReload()
    {
        // Reload logic
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(cameraPos.position, cameraPos.forward * range);
    }
}
