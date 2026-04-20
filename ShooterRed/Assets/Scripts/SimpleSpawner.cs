using Fusion;
using UnityEngine;

// Heredamos de NetworkBehaviour para que Fusion reconozca este script
public class SimpleSpawner : NetworkBehaviour
{
    public static SimpleSpawner Instance { get; private set; }

    [Header("Pon aqu� tu Prefab del Jugador")]
    public NetworkPrefabRef playerPrefab;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Spawned() es un m�todo m�gico de Fusion.
    // Se ejecuta autom�ticamente en cuanto el jugador termina de cargar esta escena y se conecta a la sala.
    public override void Spawned()
    {
        Debug.Log("�He entrado a la sala! Creando mi avatar...");

        if (Runner.GetPlayerObject(Runner.LocalPlayer) == null)
        {
            SpawnPlayer(Runner.LocalPlayer);
        }
    }

    public NetworkObject SpawnPlayer(PlayerRef player)
    {
        Vector3 spawnPosition = GetSpawnPosition();

        string playerName = NetworkManager.Instance != null ? NetworkManager.Instance.LocalPlayerName : "Jugador";

        NetworkObject playerObject = Runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player,
            (runner, obj) =>
            {
                PlayerState ps = obj.GetComponent<PlayerState>();
                if (ps != null)
                {
                    ps.Health = 100;
                }

                Debug.Log("Jugador spawneado con datos iniciales: " + playerName);
            });

        Runner.SetPlayerObject(player, playerObject);
        return playerObject;
    }

    public void RespawnLocalPlayerAfterDelay(float delaySeconds)
    {
        if (!Object || !Runner.IsRunning)
            return;

        StartCoroutine(RespawnLocalCoroutine(delaySeconds));
    }

    private System.Collections.IEnumerator RespawnLocalCoroutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (!Object || Runner == null || !Runner.IsRunning)
            yield break;

        if (Runner.GetPlayerObject(Runner.LocalPlayer) != null)
            yield break;

        SpawnPlayer(Runner.LocalPlayer);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 5f, Random.Range(-3f, 3f));
        if (Physics.Raycast(randomPosition, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
        {
            randomPosition.y = hit.point.y + 1f;
        }
        else
        {
            randomPosition.y = 1f;
        }

        return randomPosition;
    }
}