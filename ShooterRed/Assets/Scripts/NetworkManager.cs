using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    public NetworkRunner Runner { get; private set; }
    public string LocalPlayerName { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Runner == null)
        {
            Runner = gameObject.AddComponent<NetworkRunner>();
            Runner.AddCallbacks(this); // ¡Perfecto, escuchándonos a nosotros mismos!
            gameObject.AddComponent<NetworkSceneManagerDefault>();
            Runner.ProvideInput = true;
        }
    }

    private void Start()
    {
        if (Instance == this)
        {
            SceneManager.LoadScene("Menu");
        }
    }

    // --- Crear o Unirse a una sala ESPECÍFICA (por nombre) ---
    public async void CreateOrJoinRoom(string roomName)
    {
        Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>
        {
            { "map", "arena01" },
            { "mode", "paintball" }
        };

        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared, 
            SessionName = roomName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(2),
            SessionProperties = sessionProperties 
        });
    }

    // --- UNIÓN RÁPIDA (Optimizada) ---
    public async void QuickJoinRoom()
    {
        Debug.Log("QuickJoin: Buscando partida rápida...");
        
        // Al dejar SessionName vacío, Fusion automáticamente busca una sala libre o crea una.
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "", 
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(2)
        });
    }

    // --- MOSTRAR PANEL DE SALAS ---
    public async void JoinRoom()
    {
        Debug.Log("Conectando al Lobby para buscar salas...");
        
        // 1. Mostramos el panel vacío para que el jugador vea que está cargando
        // Búsqueda dinámica por si Instance no se inicializó aún
        RoomListPanel panel = RoomListPanel.Instance ?? FindFirstObjectByType<RoomListPanel>();
        if (panel != null)
        {
            RoomListPanel.Instance = panel; // aseguramos que Instance esté asignado
            panel.Show(new List<SessionInfo>());
        }
        else
        {
            Debug.LogWarning("[NetworkManager] No se encontró RoomListPanel en la escena.");
        }

        // 2. Nos unimos al pasillo (Lobby Shared)
        var result = await Runner.JoinSessionLobby(SessionLobby.Shared);

        if (result.Ok)
        {
            Debug.Log("¡Conectados al Lobby! Esperando salas...");
        }
        else
        {
            Debug.LogWarning($"Fallo al conectar al Lobby: {result.ShutdownReason}");
        }
    }

    // --- CALLBACK: RECIBIR LISTA DE SALAS ---
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[Fusion] Actualizando Lobby... {sessionList.Count} salas encontradas.");
        
        // Si el panel está activo en la pantalla, actualizamos los botones directamente
        RoomListPanel roomPanel = RoomListPanel.Instance ?? FindFirstObjectByType<RoomListPanel>();
        if (roomPanel != null && roomPanel.panel.activeInHierarchy)
        {
            roomPanel.Show(sessionList);
        }
    }

    // ========================================================================
    // Métodos vacíos requeridos por INetworkRunnerCallbacks
    // ========================================================================
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason, string message) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}