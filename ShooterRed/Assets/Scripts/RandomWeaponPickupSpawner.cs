using Fusion;
using UnityEngine;

// Este spawner crea un pickup de arma aleatoria (rifle de asalto o francotirador) y rareza aleatoria
public class RandomWeaponPickupSpawner : NetworkBehaviour
{
    [Header("Prefabs de pickups de arma")]
    public NetworkPrefabRef riflePickupPrefab;
    public NetworkPrefabRef sniperPickupPrefab;

    [Header("Tiempo de respawn")]
    public float respawnTime = 10f;

    private NetworkObject _currentPickup;
    private float _nextRespawnTime = 0f;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SpawnRandomPickup();
        }
    }

    private void Update()
    {
        if (!HasStateAuthority) return;

        // Si no hay pickup y el tiempo de respawn ha pasado, spawnea uno nuevo
        if (_currentPickup == null && Time.time >= _nextRespawnTime)
        {
            SpawnRandomPickup();
        }

        // Si el pickup existía y ha sido recogido (despawns), inicia el temporizador
        if (_currentPickup != null && _currentPickup.IsValid == false)
        {
            _currentPickup = null;
            _nextRespawnTime = Time.time + respawnTime;
        }
    }

    private void SpawnRandomPickup()
    {
        // Elige aleatoriamente el prefab
        int weaponType = Random.Range(0, 2); // 0 = rifle, 1 = sniper
        NetworkPrefabRef prefab = weaponType == 0 ? riflePickupPrefab : sniperPickupPrefab;

        // Instancia el pickup
        _currentPickup = Runner.Spawn(prefab, transform.position, Quaternion.identity);

        // Asigna rareza aleatoria
        int rarity = Random.Range(0, 5); // 0-4 (WeaponRarity)
        var pickup = _currentPickup.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.RarityLevel = rarity;
        }

        // Ya no es necesario suscribirse a eventos, el control es por Update
    }

}
