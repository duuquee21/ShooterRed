using Fusion;
using UnityEngine;

// Este spawner crea un pickup de arma aleatoria y rareza aleatoria.
// Mantiene su estado de respawn incluso si el Master Client cambia.
public class RandomWeaponPickupSpawner : NetworkBehaviour
{
    [Header("Prefabs de pickups de arma")]
    public NetworkPrefabRef riflePickupPrefab;
    public NetworkPrefabRef sniperPickupPrefab;

    [Header("Tiempo de respawn")]
    public float respawnTime = 10f;

    // Usamos el ID de red del objeto para saber si está vivo, no una referencia local
    [Networked] private NetworkId CurrentPickupId { get; set; }
    
    // Timer de red: sobrevive a desconexiones del Master Client
    [Networked] private TickTimer RespawnTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SpawnRandomPickup();
        }
    }

    // Cambiamos a FixedUpdateNetwork para la lógica de red
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // ¿Tenemos un pickup vivo?
        bool hasPickup = Runner.TryFindObject(CurrentPickupId, out NetworkObject pickupObj);

        if (hasPickup)
        {
            // Si el pickup existe en el mundo, nos aseguramos de que el timer esté apagado
            RespawnTimer = TickTimer.None; 
        }
        else
        {
            // El pickup ya no existe. ¿Está el cronómetro apagado?
            if (RespawnTimer.IsRunning == false && RespawnTimer.ExpiredOrNotRunning(Runner) == false)
            {
                // El arma acaba de ser recogida. Iniciamos la cuenta atrás de 10 segundos
                RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
            }
            
            // ¿Se ha acabado el tiempo del cronómetro?
            if (RespawnTimer.Expired(Runner))
            {
                // ¡Tiempo! Spawneamos una nueva arma y apagamos el cronómetro
                SpawnRandomPickup();
                RespawnTimer = TickTimer.None;
            }
        }
    }

    private void SpawnRandomPickup()
    {
        // 1. Elegimos aleatoriamente el prefab
        int weaponType = Random.Range(0, 2); // 0 = rifle, 1 = sniper
        NetworkPrefabRef prefab = weaponType == 0 ? riflePickupPrefab : sniperPickupPrefab;

        // 2. Instanciamos el pickup (Fusion nos devuelve el objeto creado)
        NetworkObject newPickup = Runner.Spawn(prefab, transform.position, Quaternion.identity);

        // 3. Guardamos el ID de red de este nuevo pickup para vigilarlo
        CurrentPickupId = newPickup.Id;

        // 4. Le asignamos su rareza
        int rarity = Random.Range(0, 5); // 0-4
        var pickupScript = newPickup.GetComponent<WeaponPickup>();
        
        if (pickupScript != null)
        {
            // Como acabamos de instanciarlo, nosotros somos su StateAuthority temporalmente
            pickupScript.RarityLevel = rarity;
            pickupScript.WeaponId = weaponType == 0 ? 1 : 2; // Aseguramos que tenga el ID correcto
        }
    }
}