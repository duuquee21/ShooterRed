using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    // Patrón Singleton
    public static NetworkManager Instance { get; private set; }

    public NetworkRunner Runner { get; private set; }
    public string LocalPlayerName { get; set; }

    private void Awake()
    {
        // 1. Gestión de Singleton y DDOL
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 2. Creación del Runner
        // (Corregido: Ahora dice == null)
        if (Runner == null)
        {
            Runner = gameObject.AddComponent<NetworkRunner>();

            // Fusion necesita un scene manager para cambiar de escena
            gameObject.AddComponent<NetworkSceneManagerDefault>();

            // Decir al runner que este cliente manda inputs
            Runner.ProvideInput = true;
        }
    }

    // 3. NUEVO: Al arrancar, nos vamos automáticamente al Menú
    private void Start()
    {
        if (Instance == this)
        {
            SceneManager.LoadScene("Menu"); // Asegúrate de que tu escena 1 se llama exactamente "Menu"
        }
    }

    // Crear sala como host
    public async void StartHost(string roomName)
    {
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host, // Tú creas y mandas en la partida
            SessionName = roomName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>(),
            // NUEVO: Le decimos a Fusion que la sala es la escena 2 (Room)
            Scene = SceneRef.FromIndex(2) 
        });
    }

    // Unirse a sala como cliente
    public async void StartClient(string roomName)
    {
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client, // Te unes a una partida que ya existe
            SessionName = roomName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>()
        });
    }
}