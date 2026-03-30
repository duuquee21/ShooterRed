using UnityEngine;
using Fusion;

public class NetworkManager : MonoBehaviour
{
   
    //Patron Singleton : 
    public static NetworkManager Instance { get; private set; }

    public NetworkRunner Runner { get; private set; }
    public string LocalPlayerName { get; set; }

    private void Awake()
    {
        //gestion de singleton y ddol

        if (Instance != null && Instance != this)
        {
            Destroy ( gameObject );
            return;
        }

        Instance = this;
        DontDestroyOnLoad (gameObject);

        //creacion runner

        if (Runner != null)
        {
            Runner = gameObject.AddComponent<NetworkRunner> ();

            //fusion necesita un scena manager para cambiar de escena

            gameObject.AddComponent<NetworkSceneManagerDefault>();

            //decir al runer que este cinete manda inputs

            Runner.ProvideInput = true;

        }

    }

    // crear sala como host
    public async void StartHost(string roomName)
    {
        
        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host, // Tú creas y mandas en la partida
            SessionName = roomName,
            SceneManager = GetComponent<NetworkSceneManagerDefault>()
        });
    }

    // unirse a sala
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
