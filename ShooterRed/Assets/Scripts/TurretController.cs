using Fusion;
using UnityEngine;

public class TurretController : NetworkBehaviour
{
    [Header("Atributos de Combate")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float detectionRange = 50f; // ¡Aumentado a 50 para que no sea ciega!
    [SerializeField] private float lifetime = 20f;

    [Header("Visuales y Animación")]
    [SerializeField] private Transform turretHead; 
    [SerializeField] private Transform firePoint;  
    [SerializeField] private LineRenderer laserRenderer; 
    [SerializeField] private float rotationSpeed = 10f;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public PlayerRef CurrentTarget { get; set; } 

    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer FireTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
            FireTimer = TickTimer.CreateFromSeconds(Runner, 1f / fireRate);
            CurrentTarget = PlayerRef.None;
            Debug.Log($"[Torreta] Desplegada. Dueño: {Owner.PlayerId}");
        }

        if (laserRenderer != null) laserRenderer.enabled = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // 1. Buscar objetivo
        UpdateTarget();

        // 2. Disparar si hay objetivo y el timer lo permite
        if (CurrentTarget != PlayerRef.None)
        {
            if (FireTimer.Expired(Runner))
            {
                FireTimer = TickTimer.CreateFromSeconds(Runner, 1f / fireRate);
                ShootTarget();
            }
        }
    }

    private void UpdateTarget()
    {
        PlayerRef closestEnemy = PlayerRef.None;
        float closestDist = detectionRange;

        foreach (var kvp in GameState.Instance.Players)
        {
            // Ignorar al dueño
            if (kvp.Key == Owner) continue;
            
            // Ignorar muertos
            if (kvp.Value.Health <= 0) continue;

            NetworkObject enemyObj = Runner.GetPlayerObject(kvp.Key);
            if (enemyObj == null) continue;

            // Calcular distancia al enemigo
            float dist = Vector3.Distance(transform.position, enemyObj.transform.position);
            
            // Dibuja una línea en el editor de Unity (Scene view) para ver a quién está escaneando
            Debug.DrawLine(transform.position, enemyObj.transform.position, Color.blue);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = kvp.Key;
            }
        }

        // Si cambiamos de objetivo, avisamos en consola
        if (CurrentTarget != closestEnemy)
        {
            if (closestEnemy != PlayerRef.None)
                Debug.Log($"[Torreta] ¡Enemigo fijado! Jugador {closestEnemy.PlayerId} a {closestDist} metros.");
            else
                Debug.Log("[Torreta] Objetivo perdido. Escaneando...");
                
            CurrentTarget = closestEnemy;
        }
    }

    private void ShootTarget()
    {
        NetworkObject enemyObj = Runner.GetPlayerObject(CurrentTarget);
        if (enemyObj != null)
        {
            Debug.Log($"[Torreta] Disparando al jugador {CurrentTarget.PlayerId}!");

            if (GameState.TryGetInstance(out GameState gs))
            {
                gs.RPC_RequestDamage(Owner, CurrentTarget, (int)damage);
            }

            // Apuntamos al pecho del enemigo (y+1) para el láser visual
            Vector3 targetCenter = enemyObj.transform.position + Vector3.up * 1f;
            RPC_FireVisuals(targetCenter);
        }
    }

    public override void Render()
    {
        // Movimiento de la cabeza localmente en todos los clientes
        if (CurrentTarget != PlayerRef.None && turretHead != null)
        {
            NetworkObject enemyObj = Runner.GetPlayerObject(CurrentTarget);
            if (enemyObj != null)
            {
                // Miramos al pecho del enemigo, no a los pies
                Vector3 targetPos = enemyObj.transform.position + Vector3.up * 1f;
                Vector3 direction = targetPos - turretHead.position;
                direction.y = 0; // Bloqueamos la rotación hacia arriba/abajo para que no parezca que se cae
                
                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FireVisuals(Vector3 targetPos)
    {
        if (laserRenderer != null && firePoint != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowLaser(targetPos));
        }
    }

    private System.Collections.IEnumerator ShowLaser(Vector3 targetPos)
    {
        laserRenderer.enabled = true;
        laserRenderer.SetPosition(0, firePoint.position);
        laserRenderer.SetPosition(1, targetPos);

        yield return new WaitForSeconds(0.05f); // Flash rápido como un disparo
        
        laserRenderer.enabled = false;
    }
}