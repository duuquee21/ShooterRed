using Fusion;
using UnityEngine;

// El proyectil es un NetworkObject: existe en todos los clientes visualmente,
// pero solo el State Authority (quien lo spawneÃ³) controla su movimiento y colisiones.
public class Projectile : NetworkBehaviour
{
    [Header("Proyectil")]
    [SerializeField] private float speed = 30f;      // velocidad en m/s
    [SerializeField] private float lifetime = 3f;    // segundos antes de destruirse si no impacta
    [SerializeField] private int damage = 25;        // daño por defecto (se sobreescribe al spawnear)
    [SerializeField] private LayerMask hitMask = ~0; // capas que puede impactar (~0 = todas)

    // Daño replicado: la authority lo fija en OnBeforeSpawned y todos los clientes lo leen
    [Networked] private int NetworkedDamage { get; set; }

    // Llamado por PlayerCombatIntent antes de que el objeto se sincronice
    public void SetDamage(int dmg) { NetworkedDamage = dmg; }

    // TickTimer es un temporizador sincronizado con los ticks de Fusion
    // MÃ¡s fiable que Time.time en objetos de red porque es determinista
    [Networked] private TickTimer LifeTimer { get; set; }

    private bool _hasHit; // evita procesar mÃºltiples impactos en el mismo tick

    // Se ejecuta cuando el proyectil aparece en la sesiÃ³n
    public override void Spawned()
    {
        // Si nadie llamó SetDamage, usar el valor del Inspector como fallback
        if (HasStateAuthority && NetworkedDamage == 0)
            NetworkedDamage = damage;

        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }
    }

    // FixedUpdateNetwork se ejecuta en cada tick de red
    public override void FixedUpdateNetwork()
    {
        // Solo la autoridad mueve el proyectil y procesa colisiones
        // Los otros clientes lo ven moverse gracias a la replicaciÃ³n del transform
        if (!HasStateAuthority || _hasHit)
            return;

        // Si expirÃ³ el tiempo de vida, se destruye sin impactar
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // Distancia que recorre en este tick
        float step = speed * Runner.DeltaTime;

        // Raycast corto hacia adelante: step + 0.15f de margen para evitar que
        // atraviese objetos delgados si va muy rÃ¡pido
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step + 0.15f, hitMask))
        {
            _hasHit = true; // marcamos que ya impactÃ³ para no procesar mÃ¡s

            // Buscamos si el objeto golpeado tiene un jugador
            PlayerCombatIntent target = hit.collider.GetComponentInParent<PlayerCombatIntent>();

            // Solo aplica daÃ±o si el objetivo es un jugador distinto al que disparÃ³
            if (target != null && target.Object.InputAuthority != Object.InputAuthority)
            {
                if (GameState.TryGetInstance(out GameState gameState))
                {
                    int finalDamage = NetworkedDamage > 0 ? NetworkedDamage : damage;
                    gameState.RPC_RequestDamage(Object.InputAuthority, target.Object.InputAuthority, finalDamage);
                }
            }

            // Se destruye al impactar, haya o no jugador
            Runner.Despawn(Object);
            return;
        }

        // Si no impactÃ³ nada, avanza en lÃ­nea recta
        transform.position += transform.forward * step;
    }
}
