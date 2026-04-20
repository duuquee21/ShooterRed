using Fusion;
using UnityEngine;

// SimpleSpawner gestiona la creaciÃ³n y recreaciÃ³n de avatares de jugadores.
// Hereda de NetworkBehaviour para que Fusion ejecute Spawned() automÃ¡ticamente
// cuando el objeto aparece en la sesiÃ³n de red.
public class SimpleSpawner : NetworkBehaviour
{
    // Singleton para que GameState pueda llamar a RespawnLocalPlayerAfterDelay desde cualquier sitio
    public static SimpleSpawner Instance { get; private set; }

    [Header("Prefab del Jugador")]
    public NetworkPrefabRef playerPrefab; // debe estar registrado en Fusion Network Project Config

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Spawned() se ejecuta automÃ¡ticamente cuando este objeto de red aparece en la sesiÃ³n.
    // En la prÃ¡ctica, esto ocurre justo despuÃ©s de que el jugador carga la escena del juego.
    public override void Spawned()
    {
        Debug.Log("He entrado a la sala! Creando mi avatar...");

        // Comprueba que el jugador local no tiene ya un avatar (evita duplicados)
        if (Runner.GetPlayerObject(Runner.LocalPlayer) == null)
        {
            SpawnPlayer(Runner.LocalPlayer);
        }
    }

    // Crea el avatar de un jugador en una posiciÃ³n aleatoria del mapa
    public NetworkObject SpawnPlayer(PlayerRef player)
    {
        Vector3 spawnPosition = GetSpawnPosition();

        string playerName = NetworkManager.Instance != null ? NetworkManager.Instance.LocalPlayerName : "Jugador";

        // Runner.Spawn crea el prefab en todos los clientes simultÃ¡neamente
        // El tercer parÃ¡metro (player) le dice a Fusion quiÃ©n tiene InputAuthority sobre este objeto
        // El callback (runner, obj) => se ejecuta antes de que Spawned() del prefab se llame
        NetworkObject playerObject = Runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player,
            (runner, obj) =>
            {
                // Inicializamos la vida en el PlayerState antes de que Spawned() se ejecute
                PlayerState ps = obj.GetComponent<PlayerState>();
                if (ps != null)
                {
                    ps.Health = 100;
                }

                Debug.Log("Jugador spawneado con datos iniciales: " + playerName);
            });

        // Registra la relaciÃ³n jugador â†’ avatar en Fusion para poder buscarlo con GetPlayerObject()
        Runner.SetPlayerObject(player, playerObject);
        return playerObject;
    }

    // Inicia el proceso de respawn con un delay â€” llamado desde GameState.RPC_NotifyDeath
    public void RespawnLocalPlayerAfterDelay(float delaySeconds)
    {
        // Comprobaciones bÃ¡sicas antes de arrancar la corrutina
        if (!Object || !Runner.IsRunning)
            return;

        StartCoroutine(RespawnLocalCoroutine(delaySeconds));
    }

    // Corrutina que espera el tiempo indicado y luego reaparece al jugador local
    private System.Collections.IEnumerator RespawnLocalCoroutine(float delaySeconds)
    {
        // Esperamos los segundos de penalizaciÃ³n (5 por defecto)
        yield return new WaitForSeconds(delaySeconds);

        // Comprobaciones de seguridad despuÃ©s de la espera
        // (el runner podrÃ­a haberse desconectado durante los 5 segundos)
        if (!Object || Runner == null || !Runner.IsRunning)
            yield break;

        // Si durante la espera el jugador ya tiene un avatar (por algÃºn otro motivo), no creamos otro
        if (Runner.GetPlayerObject(Runner.LocalPlayer) != null)
            yield break;

        // Crear nuevo avatar â€” GameState.RPC_RequestRegisterPlayer se llamarÃ¡ desde Spawned() del prefab
        SpawnPlayer(Runner.LocalPlayer);
    }

    // Calcula una posiciÃ³n de spawn vÃ¡lida en el mapa
    private Vector3 GetSpawnPosition()
    {
        // PosiciÃ³n aleatoria en X/Z, arriba en Y para luego bajarla al suelo
        Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 5f, Random.Range(-3f, 3f));

        // Raycast hacia abajo buscando la capa "Ground"
        if (Physics.Raycast(randomPosition, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
        {
            // Colocamos al jugador justo encima del punto de impacto
            randomPosition.y = hit.point.y + 1f;
        }
        else
        {
            // Fallback si no encontrÃ³ suelo: Y = 1 para no spawnar dentro del suelo
            randomPosition.y = 1f;
        }

        return randomPosition;
    }
}

