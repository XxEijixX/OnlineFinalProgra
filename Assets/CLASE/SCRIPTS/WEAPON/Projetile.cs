using Fusion;
using System.Threading.Tasks;
using UnityEngine;


public class Projetile : NetworkBehaviour
{

    [SerializeField] private float speed = 100f;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] public int damage;

    private Rigidbody rb;

    [Networked] public PlayerRef Shooter { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (Object.HasStateAuthority) rb.linearVelocity = speed * transform.forward;
        DespawnAfterTime();
    }

    private async void DespawnAfterTime()
    {
        await Task.Delay((int)(lifetime * 1000));
        if (Object != null && Object.HasStateAuthority) Runner.Despawn(Object);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!ColisionValida()) return;

        ContactPoint impacto = collision.GetContact(0);

        if (collision.gameObject.CompareTag("Player") || (collision.gameObject.CompareTag("Target")))
        {
            if (collision.gameObject.TryGetComponent<Health>(out Health health))
            {
                RpcDañoEnemigo(Shooter, health.Object, damage);
                Runner.Despawn(Object);
            }
        }
    }

    private bool ColisionValida()
    {
        return Object != null && Object.HasStateAuthority;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private void RpcDañoEnemigo(PlayerRef jugador, NetworkObject enemigo, int daño)
    {
        if (enemigo != null && enemigo.TryGetComponent<Health>(out Health health))
        {
            health.Rpc_TakeDamage(daño, jugador);
        }
    }

}