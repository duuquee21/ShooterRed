using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Header("Proyectil")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 25;
    [SerializeField] private LayerMask hitMask = ~0;

    [Networked] private TickTimer LifeTimer { get; set; }

    private bool _hasHit;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || _hasHit)
            return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        float step = speed * Runner.DeltaTime;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step + 0.15f, hitMask))
        {
            _hasHit = true;

            PlayerCombatIntent target = hit.collider.GetComponentInParent<PlayerCombatIntent>();
            if (target != null && target.Object.InputAuthority != Object.InputAuthority)
            {
                if (GameState.TryGetInstance(out GameState gameState))
                {
                    gameState.RPC_RequestDamage(Object.InputAuthority, target.Object.InputAuthority, damage);
                }
            }

            Runner.Despawn(Object);
            return;
        }

        transform.position += transform.forward * step;
    }
}