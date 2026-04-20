using System.Collections.Generic; // Necesario para usar Diccionarios (Custom Properties)
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using System.Threading.Tasks; // Necesario para las tareas asíncronas

public class NetworkManager : MonoBehaviour
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
            SessionCallbacks callbacks = gameObject.AddComponent<SessionCallbacks>();
            Runner.AddCallbacks(callbacks);
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

    // --- NUEVO: Crear o Unirse a una sala específica con Custom Properties ---
    public async void CreateOrJoinRoom(string roomName)
    {
        // 1. Definimos las Custom Properties (Metadatos para el Lobby)
        Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>
        {
            { "map", "arena01" },
            { "mode", "paintball" }
        };

        // 2. Iniciamos la conexión en modo Shared
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared, // Todos somos iguales (sin Host dictador)
            SessionName = roomName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(2), // Nos lleva a la escena Room
            SessionProperties = sessionProperties // Le pegamos las etiquetas a la sala
        });
    }

    // --- NUEVO: Unión Rápida (Quick Join) ---
    public async void QuickJoinRoom()
    {
        // Al no pasarle un 'SessionName', Photon buscará cualquier sala abierta y te meterá
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(2)
        });
    }
}