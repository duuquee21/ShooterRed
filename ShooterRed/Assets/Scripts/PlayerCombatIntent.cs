using Fusion;
using UnityEngine;

// Este script gestiona todo lo relacionado con el combate del jugador:
// - Detecta la intenciÃ³n de disparar (input)
// - Spawnea el proyectil en red
// - Gestiona quÃ© cÃ¡mara y AudioListener estÃ¡n activos (solo los del jugador local)
public class PlayerCombatIntent : NetworkBehaviour
{
    [Header("Disparo")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private float range = 100f;
    [SerializeField] private int baseDamage = 25;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool useScreenCenter = true;
    [SerializeField] private NetworkPrefabRef projectilePrefab;
    [SerializeField] private float fireInterval = 0.3f;
    private float _nextFireTime;
    private bool _autoFire;

    [Header("Granada visual")]
    [SerializeField] private GameObject grenadePrefabVisual; // prefab normal (sin NetworkObject)
    [SerializeField] private float grenadeThrowForce = 12f;  // velocidad de lanzamiento
    [SerializeField] private float grenadeArcUp = 5f;        // impulso vertical para el arco

    [Header("Vista local")]
    [SerializeField] private Camera[] ownedCameras;
    [SerializeField] private AudioListener[] ownedAudioListeners;

    private bool warnedMissingAimSource; // evita spam de warnings en consola

    private void Update()
    {
        // Solo procesa input el cliente que controla este avatar
        if (!HasInputAuthority)
            return;

        // Comprueba que GameState existe y está listo
        if (!GameState.TryGetInstance(out GameState gameState))
            return;

        // Si el jugador no está registrado aún, reintenta cada segundo
        if (!gameState.TryGetPlayerData(Object.InputAuthority, out PlayerCombatData localData))
        {
            if (Time.time >= _nextRegisterAttempt)
            {
                _nextRegisterAttempt = Time.time + 1f;
                TryRegisterInGameState();
            }
            return;
        }

        // Un jugador muerto no puede disparar
        if (localData.Health <= 0)
            return;

        TryResolveLocalCamera();

        // E alterna entre modo semi-automático y automático
        if (Input.GetKeyDown(KeyCode.E))
        {
            _autoFire = !_autoFire;
            Debug.Log("Modo disparo: " + (_autoFire ? "Automatico" : "Semiautomatico"));
        }

        // GetMouseButton = mientras mantengas pulsado (auto)
        // GetMouseButtonDown = solo el primer frame que pulsas (semi-auto)
              bool shooting = _autoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (shooting)
        {
            TryShoot();
        }

        // G = granada (racha 3)
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Comprobamos si tenemos la recompensa antes de lanzar la visual
            if (localData.HasGrenade)
                SpawnGrenadeVisual(gameState);
        }

        // F = ataque aÃ©reo (si tienes racha 5)
        if (Input.GetKeyDown(KeyCode.F))
            gameState.RPC_RequestUseAirstrike(Object.InputAuthority);

        // T = desplegar torreta (si tienes racha 10)
        if (Input.GetKeyDown(KeyCode.T))
            gameState.RPC_RequestUseTurret(Object.InputAuthority, transform.position + transform.forward * 2f);
    }

    // Instancia la granada visual localmente — el RPC de daño se llama cuando explota
    private void SpawnGrenadeVisual(GameState gameState)
    {
        if (grenadePrefabVisual == null) return;

        Vector3 throwDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + throwDir * 0.5f;

        GameObject go = Instantiate(grenadePrefabVisual, spawnPos, Random.rotation);
        GrenadeVisual gv = go.GetComponent<GrenadeVisual>();
        if (gv != null)
        {
            Vector3 velocity = throwDir.normalized * grenadeThrowForce + Vector3.up * grenadeArcUp;
            gv.Launch(velocity);

            // Cuando la granada explota, enviamos el RPC con su posición final
            PlayerRef owner = Object.InputAuthority;
            gv.OnExplode = (explosionPos) =>
            {
                if (GameState.TryGetInstance(out GameState gs))
                    gs.RPC_RequestUseGrenade(owner, explosionPos);
            };
        }
    }

    // Intenta resolver qué cámara usar para apuntar.
    // Primero activa/desactiva cámaras según authority, luego asigna la referencia.
    private void TryResolveLocalCamera()
    {
        ConfigureViewForAuthority();

        if (!HasInputAuthority)
            return;

        // Si ya tenemos cÃ¡mara asignada, no hace falta buscar
        if (playerCamera != null)
            return;

        // Intenta usar las ya cacheadas
        if (ownedCameras != null && ownedCameras.Length > 0)
        {
            playerCamera = ownedCameras[0];
            return;
        }

        // BÃºsqueda dinÃ¡mica en los hijos del prefab
        ownedCameras = GetComponentsInChildren<Camera>(true);
        ownedAudioListeners = GetComponentsInChildren<AudioListener>(true);

        if (ownedCameras.Length > 0)
        {
            playerCamera = ownedCameras[0];
            return;
        }

        // Ãšltimo recurso: la cÃ¡mara principal de la escena
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerCamera = mainCam;
        }
    }

    // Activa la cÃ¡mara y el AudioListener SOLO en el cliente que controla este avatar.
    // Sin esto, si hay 4 jugadores en sala, habrÃ­a 4 cÃ¡maras activas a la vez.
    private void ConfigureViewForAuthority()
    {
        if (ownedCameras == null || ownedCameras.Length == 0)
        {
            ownedCameras = GetComponentsInChildren<Camera>(true);
        }

        if (ownedAudioListeners == null || ownedAudioListeners.Length == 0)
        {
            ownedAudioListeners = GetComponentsInChildren<AudioListener>(true);
        }

        // true solo para el jugador local, false para los avatares remotos
        bool enableLocalView = HasInputAuthority;

        for (int i = 0; i < ownedCameras.Length; i++)
        {
            if (ownedCameras[i] != null)
                ownedCameras[i].enabled = enableLocalView;
        }

        for (int i = 0; i < ownedAudioListeners.Length; i++)
        {
            if (ownedAudioListeners[i] != null)
                ownedAudioListeners[i].enabled = enableLocalView;
        }
    }

    private void TryShoot()
    {
        // Control de cadencia: si no ha pasado suficiente tiempo desde el Ãºltimo disparo, ignorar
        if (Time.time < _nextFireTime)
            return;

        _nextFireTime = Time.time + fireInterval;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (playerCamera != null)
        {
            // Convertimos el centro de la pantalla (o el cursor) en un rayo 3D
            Vector3 screenPoint = useScreenCenter
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;

            Ray ray = playerCamera.ScreenPointToRay(screenPoint);
            spawnPos = ray.origin;                              // el proyectil nace en la cÃ¡mara
            spawnRot = Quaternion.LookRotation(ray.direction);  // apunta en la direcciÃ³n del rayo
        }
        else
        {
            // Fallback si no hay cÃ¡mara: dispara hacia adelante desde la posiciÃ³n del jugador
            Transform origin = shootOrigin != null ? shootOrigin : transform;
            spawnPos = origin.position + Vector3.up * 1.4f;
            spawnRot = origin.rotation;
        }

        // Fusion crea el proyectil en todos los clientes simultÃ¡neamente
        // Object.InputAuthority le dice a Fusion quiÃ©n es el dueÃ±o del proyectil
        Runner.Spawn(projectilePrefab, spawnPos, spawnRot, Object.InputAuthority);
    }

    // Tiempo hasta el próximo intento de registro (evita spam)
    private float _nextRegisterAttempt;

    // Spawned() se ejecuta cuando este NetworkObject aparece en la sesión
    public override void Spawned()
    {
        ConfigureViewForAuthority();
        TryResolveLocalCamera();
        TryRegisterInGameState();
    }

    // Intenta registrar al jugador en GameState. Se llama en Spawned y como reintento desde Update.
    private void TryRegisterInGameState()
    {
        if (!HasInputAuthority) return;
        if (!GameState.TryGetInstance(out GameState gameState)) return;
        gameState.RPC_RequestRegisterPlayer(Object.InputAuthority);
    }

    // Destruye este avatar en la red â€” llamado por GameState cuando el jugador muere
    public void DespawnOwnedAvatar()
    {
        // Solo puede destruirlo quien tiene StateAuthority sobre Ã©l
        if (!HasStateAuthority)
            return;

        Runner.Despawn(Object);
    }
}
