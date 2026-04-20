using Fusion;
using UnityEngine;

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
    
    [Header("Vista local")]
    [SerializeField] private Camera[] ownedCameras;
    [SerializeField] private AudioListener[] ownedAudioListeners;

    private bool warnedMissingAimSource;

    private void Update()
{
    if (!HasInputAuthority)
        return;

    if (!GameState.TryGetInstance(out GameState gameState))
        return;

    if (!gameState.TryGetPlayerData(Object.InputAuthority, out PlayerCombatData localData))
        return;

    if (localData.Health <= 0)
        return;

    TryResolveLocalCamera();

    if (Input.GetKeyDown(KeyCode.E))
    {
        _autoFire = !_autoFire;
        Debug.Log("Modo disparo: " + (_autoFire ? "Automatico" : "Semiautomatico"));
    }

    bool shooting = _autoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
    if (shooting)
    {
        TryShoot();
    }
}

    private void TryResolveLocalCamera()
    {
        ConfigureViewForAuthority();

        if (!HasInputAuthority)
            return;

        if (playerCamera != null)
            return;

        if (ownedCameras != null && ownedCameras.Length > 0)
        {
            playerCamera = ownedCameras[0];
            return;
        }

        ownedCameras = GetComponentsInChildren<Camera>(true);
        ownedAudioListeners = GetComponentsInChildren<AudioListener>(true);

        if (ownedCameras.Length > 0)
        {
            playerCamera = ownedCameras[0];
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerCamera = mainCam;
        }
    }

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

        bool enableLocalView = HasInputAuthority;

        for (int i = 0; i < ownedCameras.Length; i++)
        {
            if (ownedCameras[i] != null)
            {
                ownedCameras[i].enabled = enableLocalView;
            }
        }

        for (int i = 0; i < ownedAudioListeners.Length; i++)
        {
            if (ownedAudioListeners[i] != null)
            {
                ownedAudioListeners[i].enabled = enableLocalView;
            }
        }
    }

   private void TryShoot()
{
    if (Time.time < _nextFireTime)
        return;

    _nextFireTime = Time.time + fireInterval;

    Vector3 spawnPos;
    Quaternion spawnRot;

    if (playerCamera != null)
    {
        Vector3 screenPoint = useScreenCenter
            ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
            : Input.mousePosition;

        Ray ray = playerCamera.ScreenPointToRay(screenPoint);
        spawnPos = ray.origin;
        spawnRot = Quaternion.LookRotation(ray.direction);
    }
    else
    {
        Transform origin = shootOrigin != null ? shootOrigin : transform;
        spawnPos = origin.position + Vector3.up * 1.4f;
        spawnRot = origin.rotation;
    }

    Runner.Spawn(projectilePrefab, spawnPos, spawnRot, Object.InputAuthority);
}

    public override void Spawned()
    {
        ConfigureViewForAuthority();
        TryResolveLocalCamera();

        if (HasInputAuthority && GameState.TryGetInstance(out GameState gameState))
        {
            gameState.RPC_RequestRegisterPlayer(Object.InputAuthority);
        }
    }

    public void DespawnOwnedAvatar()
    {
        if (!HasStateAuthority)
            return;

        Runner.Despawn(Object);
    }
}