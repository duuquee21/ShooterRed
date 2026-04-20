using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[Session] Conectado al servidor.");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogWarning("[Session] Fallo de conexión: " + reason);
        // Aquí desbloquea botones de menú si los tienes bloqueados
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning("[Session] Desconectado: " + reason);
        SceneManager.LoadScene("Menu");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("[Session] Runner apagado: " + shutdownReason);
        SceneManager.LoadScene("Menu");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("[Session] Jugador entró: " + player);
        // El registro en GameState se hace desde PlayerCombatIntent.Spawned()
        // No lo llamamos aquí porque GameState puede no estar listo todavía
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("[Session] Jugador salió: " + player);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("[Session] Lista de salas actualizada. Total: " + sessionList.Count);
        // Aquí conectarás el browser de salas cuando lo tengas
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[Session] Cargando escena...");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Session] Escena cargada.");
    }

    // Métodos obligatorios de la interfaz que no usamos aún
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
}
