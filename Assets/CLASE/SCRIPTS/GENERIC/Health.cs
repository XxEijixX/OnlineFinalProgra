using Fusion;
using System.Linq;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] int maxHealth = 100;
    [Networked] public int CurrentHealth { get; set; }

    public override void Spawned()
    {
        CurrentHealth = maxHealth;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_TakeDamage(int damage, PlayerRef damagedplayer)
    {
        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Death(damagedplayer);
        }
    }

    private void Death(PlayerRef executioner)
    {
        if (gameObject.CompareTag("Target"))
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();

            if (executioner != PlayerRef.None)
            {
                PlayerRef[] players = Runner.ActivePlayers.ToArray();

                if (players.Length >= 1)
                {
                    if (executioner == players[0])
                    {
                        gameManager.RpcScorePlayerOne();
                    }
                    else if (players.Length >= 2 && executioner == players[1])
                    {
                        gameManager.RpcScorePlayerTwo();
                    }
                }
            }
        }

        if (Object != null && Object.HasStateAuthority) Runner.Despawn(Object);
    }
}
