using Fusion;
using UnityEngine;

// La torreta es una entidad de red que busca enemigos y les dispara automáticamente.
// Hereda de NetworkBehaviour para existir en todos los clientes y ser controlada por la autoridad.
public class TurretController : NetworkBehaviour
{
    [Header("Torreta")]
    [SerializeField] private float damage = 15f;       // daño por disparo
    [SerializeField] private float fireRate = 1f;      // disparos por segundo
    [SerializeField] private float detectionRange = 20f; // radio de detección de enemigos
    [SerializeField] private float lifetime = 20f;     // segundos hasta que desaparece

    // Propietario de la torreta (el jugador que la desplegó)
    [Networked] public PlayerRef Owner { get; set; }

    // Timer de red determinista — igual en todos los clientes
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer FireTimer { get; set; }

    public override void Spawned()
    {
        // Solo la autoridad inicializa los timers
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
            FireTimer = TickTimer.CreateFromSeconds(Runner, 1f / fireRate);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Solo la autoridad controla la lógica de la torreta
        if (!HasStateAuthority) return;

        // Si expiró su tiempo de vida, desaparece
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // Solo dispara cuando el timer de cadencia lo permite
        if (!FireTimer.Expired(Runner)) return;

        // Reinicia el timer para el próximo disparo
        FireTimer = TickTimer.CreateFromSeconds(Runner, 1f / fireRate);

        TryFindAndShootTarget();
    }

    private void TryFindAndShootTarget()
    {
        // Busca el jugador enemigo más cercano dentro del rango
        PlayerRef closestEnemy = PlayerRef.None;
        float closestDist = detectionRange;

        foreach (var kvp in GameState.Instance.Players)
        {
            // No dispara a su propietario
            if (kvp.Key == Owner) continue;
            // No dispara a jugadores muertos
            if (kvp.Value.Health <= 0) continue;

            // Obtiene la posición del avatar del enemigo
            NetworkObject enemyObj = Runner.GetPlayerObject(kvp.Key);
            if (enemyObj == null) continue;

            float dist = Vector3.Distance(transform.position, enemyObj.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = kvp.Key;
            }
        }

        // Si encontró un objetivo, solicita daño al GameState
        if (closestEnemy != PlayerRef.None)
        {
            if (GameState.TryGetInstance(out GameState gs))
            {
                gs.RPC_RequestDamage(Owner, closestEnemy, (int)damage);
            }
        }
    }
}