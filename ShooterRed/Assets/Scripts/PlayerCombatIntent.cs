using Fusion;
using UnityEngine;

public class PlayerCombatIntent : NetworkBehaviour
{
    [Header("Referencias de Red")]
    private PlayerState _myState; 

    [Header("Configuración de Cámara")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private bool useScreenCenter = true;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef projectilePrefab;
    [SerializeField] private GameObject grenadePrefabVisual; 

    [Header("Ajustes de Lanzamiento")]
    [SerializeField] private float grenadeThrowForce = 12f;
    [SerializeField] private float grenadeArcUp = 5f;

    [Header("Vista local")]
    [SerializeField] private Camera[] ownedCameras;
    [SerializeField] private AudioListener[] ownedAudioListeners;

    private float _nextFireTime;
    private bool _autoFireMode; // El estado interno del modo de disparo
    private float _nextRegisterAttempt;
    private int _lastWeaponIdCheck = -1; // Para detectar cambios de arma

    public override void Spawned()
    {
        _myState = GetComponent<PlayerState>();
        ConfigureViewForAuthority();
        TryResolveLocalCamera();
        TryRegisterInGameState();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        if (!GameState.TryGetInstance(out GameState gameState)) return;

        if (!gameState.TryGetPlayerData(Object.InputAuthority, out PlayerCombatData localData))
        {
            if (Time.time >= _nextRegisterAttempt)
            {
                _nextRegisterAttempt = Time.time + 1f;
                TryRegisterInGameState();
            }
            return;
        }

        if (localData.Health <= 0) return;

        TryResolveLocalCamera();

        // 1. Obtener datos del arma actual
        int currentId = (_myState != null) ? _myState.CurrentWeaponId : 0;
        WeaponDefinition weaponData = WeaponDatabase.Get(currentId);

        // 2. Lógica de AUTO-SWITCH: Si cambias al Rifle, activamos el auto por ti
        if (currentId != _lastWeaponIdCheck)
        {
            if (weaponData.IsAutomatic) _autoFireMode = true;
            else _autoFireMode = false;
            
            _lastWeaponIdCheck = currentId;
            Debug.Log($"Arma cambiada a: {weaponData.DisplayName}. AutoMode: {_autoFireMode}");
        }

        // 3. Permitir cambio manual con la 'E' (por si el jugador quiere semiauto en el rifle)
        if (Input.GetKeyDown(KeyCode.E))
        {
            _autoFireMode = !_autoFireMode;
            Debug.Log("Cambio manual de modo. Auto: " + _autoFireMode);
        }

        // 4. Lógica de disparo inteligente
        // Solo será automático si el ARMA lo permite Y el MODO está activo
        bool isCurrentlyAuto = weaponData.IsAutomatic && _autoFireMode;

        bool shooting = isCurrentlyAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (shooting)
        {
            TryShoot(weaponData);
        }

        // ==========================================
        // ACCIONES SECUNDARIAS (RECOMPENSAS DE RACHA)
        // ==========================================
        if (Input.GetKeyDown(KeyCode.G) && localData.HasGrenade) 
            SpawnGrenadeVisual(gameState);
            
        if (Input.GetKeyDown(KeyCode.F) && localData.HasAirstrike) 
            gameState.RPC_RequestUseAirstrike(Object.InputAuthority);
            
        if (Input.GetKeyDown(KeyCode.T) && localData.HasTurret) 
        {
            // Calcula una posición 2 metros delante de ti, usando la rotación de tu cuerpo
            Vector3 placementPosition = transform.position + transform.forward * 2f;
            
            // Aseguramos que se plante a la altura de tus pies (suelo)
            placementPosition.y = transform.position.y;

            gameState.RPC_RequestUseTurret(Object.InputAuthority, placementPosition);
        }
    }

    private void TryShoot(WeaponDefinition weaponData)
    {
        // Control de cadencia (FireRate de la Database)
        if (Time.time < _nextFireTime) return;
        _nextFireTime = Time.time + weaponData.FireRate;

        // Cálculo de daño por rareza
        int currentRarity = (_myState != null) ? _myState.CurrentWeaponRarity : 0;
        int finalDamage = WeaponDatabase.GetFinalDamage(weaponData.WeaponId, currentRarity);

        // Posicionamiento del proyectil
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

        // Spawn en red
        Runner.Spawn(projectilePrefab, spawnPos, spawnRot, Object.InputAuthority,
            (runner, obj) =>
            {
                Projectile p = obj.GetComponent<Projectile>();
                if (p != null) p.SetDamage(finalDamage);
            });
    }

    #region Soporte Visual y Registro
    private void TryResolveLocalCamera()
    {
        ConfigureViewForAuthority();
        if (!HasInputAuthority || playerCamera != null) return;
        ownedCameras = GetComponentsInChildren<Camera>(true);
        if (ownedCameras.Length > 0) playerCamera = ownedCameras[0];
    }

    private void ConfigureViewForAuthority()
    {
        bool enableLocalView = HasInputAuthority;
        foreach (var cam in GetComponentsInChildren<Camera>(true)) if (cam != null) cam.enabled = enableLocalView;
        foreach (var aud in GetComponentsInChildren<AudioListener>(true)) if (aud != null) aud.enabled = enableLocalView;
    }

    private void TryRegisterInGameState()
    {
        if (!HasInputAuthority) return;
        if (GameState.TryGetInstance(out GameState gameState))
            gameState.RPC_RequestRegisterPlayer(Object.InputAuthority);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_BroadcastGrenadeThrow(Vector3 spawnPos, Vector3 velocity)
    {
        if (grenadePrefabVisual == null) return;
        GameObject go = Instantiate(grenadePrefabVisual, spawnPos, Random.rotation);
        GrenadeVisual gv = go.GetComponent<GrenadeVisual>();
        if (gv != null)
        {
            gv.Launch(velocity);
            if (HasInputAuthority)
            {
                gv.OnExplode = (p) => { if (GameState.TryGetInstance(out GameState gs)) gs.RPC_RequestUseGrenade(Object.InputAuthority, p); };
            }
        }
    }

    private void SpawnGrenadeVisual(GameState gameState)
    {
        Vector3 throwDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + throwDir * 0.5f;
        Vector3 velocity = throwDir.normalized * grenadeThrowForce + Vector3.up * grenadeArcUp;
        RPC_BroadcastGrenadeThrow(spawnPos, velocity);
    }

    public void DespawnOwnedAvatar() { if (HasStateAuthority) Runner.Despawn(Object); }
    #endregion
}