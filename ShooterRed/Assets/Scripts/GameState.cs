using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Los tres estados posibles de la partida
public enum MatchState : byte
{
    Waiting = 0,  
    Playing = 1,  
    Ended = 2     
}

public struct PlayerCombatData : INetworkStruct
{
    public int Health;   
    public int Kills;    
    public int Deaths;   
    public int Streak;   
    public int Score;    
    public bool HasGrenade;
    public bool HasAirstrike;
    public bool HasTurret;
}

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Reglas globales")]
    [Networked] public int ScoreLimit { get; set; }       
    
    [Networked, OnChangedRender(nameof(OnStateChanged))] 
    public MatchState State { get; set; }     
    
    [Networked] public TickTimer MatchTimer { get; set; } 

    [Header("Recompensas de racha")]
    [SerializeField] private NetworkPrefabRef turretPrefab; 
    
    // NUEVO: Referencia al prefab puramente visual de la granada
    [SerializeField] private GrenadeVisual grenadeVisualPrefab; 

    [Header("Umbrales de racha (kills necesarios)" )]
    [SerializeField] private int grenadeStreakThreshold  = 3;
    [SerializeField] private int airstrikeStreakThreshold = 5;
    [SerializeField] private int turretStreakThreshold   = 10;
    
    [Header("Datos globales por jugador")]
    [Networked, Capacity(16)]
    public NetworkDictionary<PlayerRef, PlayerCombatData> Players => default;

    private bool _isSpawned;

    public bool IsNetworkReady => _isSpawned && Object != null && Runner != null;
    private bool CanRegister => Object != null && Runner != null && HasStateAuthority;

    public static bool TryGetInstance(out GameState gameState)
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<GameState>();
        }
        gameState = Instance;
        return gameState != null;
    }

    public override void Spawned()
    {
        Instance = this;
        _isSpawned = true; 

        if (HasStateAuthority)
        {
            ScoreLimit = 15;
            State = MatchState.Waiting;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSpawned = false;
        if (Instance == this)
            Instance = null;
    }

    public bool CanValidateGlobalRules()
    {
        return HasStateAuthority;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (State == MatchState.Playing)
        {
            if (MatchTimer.Expired(Runner))
            {
                Debug.Log("[Servidor] ¡Se acabó el tiempo!");
                State = MatchState.Ended; 
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRegisterPlayer(PlayerRef player)
    {
        if (!CanRegister) return;

        if (Players.ContainsKey(player))
        {
            PlayerCombatData existingData = Players[player];
            existingData.Health = 100;
            existingData.Streak = 0;
            Players.Set(player, existingData);
            return;
        }

        PlayerCombatData data = new PlayerCombatData
        {
            Health = 100,
            Kills = 0,
            Deaths = 0,
            Streak = 0,
            Score = 0
        };

        Players.Set(player, data);

        if (State == MatchState.Waiting && Players.Count >= 2)
        {
            State = MatchState.Playing;
            MatchTimer = TickTimer.CreateFromSeconds(Runner, 300f); 
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(PlayerRef attacker, PlayerRef target, int damage)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules()) return;
        if (State != MatchState.Playing) return;
        if (damage <= 0 || damage > 100) return;
        if (!Players.ContainsKey(attacker) || !Players.ContainsKey(target)) return;
        if (attacker == target) return;

        PlayerCombatData targetData = Players[target];
        targetData.Health -= damage;

        if (targetData.Health > 0)
        {
            Players.Set(target, targetData);

            NetworkObject aliveTargetObj = Runner.GetPlayerObject(target);
            if (aliveTargetObj != null)
            {
                PlayerState aliveTargetPs = aliveTargetObj.GetComponent<PlayerState>();
                if (aliveTargetPs != null)
                {
                    aliveTargetPs.Health = targetData.Health;
                }
            }
            return;
        }

        // --- MUERTE: llegó a 0 vida o menos ---
        targetData.Deaths += 1;
        targetData.Streak = 0;    
        targetData.Health = 0;
        targetData.HasGrenade = false;
        targetData.HasAirstrike = false;
        targetData.HasTurret = false;
        Players.Set(target, targetData);

        PlayerCombatData attackerData = Players[attacker];
        attackerData.Kills += 1;
        attackerData.Streak += 1;  
        attackerData.Score += 100; 

        if (attackerData.Streak >= grenadeStreakThreshold)  attackerData.HasGrenade   = true;
        if (attackerData.Streak >= airstrikeStreakThreshold) attackerData.HasAirstrike = true;
        if (attackerData.Streak >= turretStreakThreshold)   attackerData.HasTurret    = true;

        Players.Set(attacker, attackerData);

        RPC_KillFeed(attacker, target, attackerData.Streak);
        RPC_NotifyDeath(target, 5f);

        if (attackerData.Kills >= ScoreLimit)
        {
            State = MatchState.Ended; 
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_KillFeed(PlayerRef attacker, PlayerRef victim, int streak)
    {
        if (KillFeedHud.Instance != null)
        {
            string attackerName = GetPlayerName(attacker);
            string victimName = GetPlayerName(victim);
            KillFeedHud.Instance.AddEntry(attackerName, victimName);
        }
    }

    public string GetPlayerName(PlayerRef player)
    {
        if (Runner == null) return "Jugador " + player.PlayerId;
        NetworkObject obj = Runner.GetPlayerObject(player);
        if (obj == null) return "Jugador " + player.PlayerId;
        PlayerState ps = obj.GetComponent<PlayerState>();
        if (ps == null) return "Jugador " + player.PlayerId;
        string name = ps.PlayerName.ToString();
        return string.IsNullOrEmpty(name) ? "Jugador " + player.PlayerId : name;
    }

    public bool TryGetPlayerData(PlayerRef player, out PlayerCombatData data)
    {
        data = default;
        if (!IsNetworkReady) return false;
        return Players.TryGet(player, out data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyDeath(PlayerRef victim, float respawnDelay)
    {
        if (Runner == null || Runner.LocalPlayer != victim) return;

        NetworkObject victimObject = Runner.GetPlayerObject(victim);
        if (victimObject != null)
        {
            PlayerCombatIntent combatIntent = victimObject.GetComponent<PlayerCombatIntent>();
            if (combatIntent != null)
            {
                combatIntent.DespawnOwnedAvatar(); 
            }

            Runner.Despawn(victimObject);
            Runner.SetPlayerObject(victim, null); 
        }

        if (SimpleSpawner.Instance == null) return;
        SimpleSpawner.Instance.RespawnLocalPlayerAfterDelay(respawnDelay);
    }

    // ========================================================================
    // GRANADA NORMAL
    // ========================================================================
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUseGrenade(PlayerRef requester, Vector3 explosionPos)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules()) return;
        if (State != MatchState.Playing) return;

        if (!Players.TryGet(requester, out PlayerCombatData data)) return;
        if (!data.HasGrenade) return;

        data.HasGrenade = false;
        Players.Set(requester, data);

        float radius = 8f;
        foreach (var kvp in Players)
        {
            if (kvp.Key == requester) continue;
            if (kvp.Value.Health <= 0) continue;
            NetworkObject enemyObj = Runner.GetPlayerObject(kvp.Key);
            if (enemyObj == null) continue;
            float dist = Vector3.Distance(explosionPos, enemyObj.transform.position);
            if (dist <= radius)
            {
                int dmg = Mathf.RoundToInt(Mathf.Lerp(10f, 50f, 1f - (dist / radius)));
                RPC_RequestDamage(requester, kvp.Key, dmg);
            }
        }
    }

    // ========================================================================
    // ATAQUE AÉREO: LLUVIA DE GRANADAS (NUEVO)
    // ========================================================================
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUseAirstrike(PlayerRef requester)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules()) return;
        if (State != MatchState.Playing) return;

        if (!Players.TryGet(requester, out PlayerCombatData data)) return;
        if (!data.HasAirstrike) return;

        data.HasAirstrike = false;
        Players.Set(requester, data);

        // Generamos UNA semilla aleatoria para coordinar todos los clientes
        int magicSeed = Random.Range(int.MinValue, int.MaxValue);

        // Avisamos a los clientes para que dibujen las granadas
        RPC_PlayAirstrikeVisuals(magicSeed);

        // El servidor empieza a contar para aplicar el daño
        StartCoroutine(AirstrikeDamageRoutine(requester, magicSeed));

        Debug.Log("Ataque aéreo solicitado por: " + requester);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAirstrikeVisuals(int seed)
    {
        if (grenadeVisualPrefab == null) return;

        Random.State oldState = Random.state;
        Random.InitState(seed);

        int numberOfBombs = 10;
        float mapSize = 25f; 

        for (int i = 0; i < numberOfBombs; i++)
        {
            Vector3 randomPoint = new Vector3(Random.Range(-mapSize, mapSize), 0, Random.Range(-mapSize, mapSize));
            Vector3 spawnPos = new Vector3(randomPoint.x, 20f, randomPoint.z); // Caen desde el cielo
            
            GrenadeVisual bomb = Instantiate(grenadeVisualPrefab, spawnPos, Quaternion.identity);
            bomb.Launch(new Vector3(0, -5f, 0)); // Caída recta
        }

        Random.state = oldState;
    }

    private IEnumerator AirstrikeDamageRoutine(PlayerRef requester, int seed)
    {
        // Esperamos lo que tarda la mecha de la granada (2.5s)
        yield return new WaitForSeconds(2.5f);

        Random.State oldState = Random.state;
        Random.InitState(seed);

        int numberOfBombs = 10;
        float mapSize = 25f;
        float radius = 8f;

        Vector3[] explosionPoints = new Vector3[numberOfBombs];
        for (int i = 0; i < numberOfBombs; i++)
        {
            explosionPoints[i] = new Vector3(Random.Range(-mapSize, mapSize), 0, Random.Range(-mapSize, mapSize));
        }
        Random.state = oldState;

        foreach (Vector3 pos in explosionPoints)
        {
            foreach (var kvp in Players)
            {
                if (kvp.Value.Health <= 0) continue;
                // Si quieres que el ataque aéreo mate al que lo lanzó, comenta la siguiente línea:
                // if (kvp.Key == requester) continue; 

                NetworkObject enemyObj = Runner.GetPlayerObject(kvp.Key);
                if (enemyObj == null) continue;

                float dist = Vector3.Distance(pos, enemyObj.transform.position);
                if (dist <= radius)
                {
                    int dmg = Mathf.RoundToInt(Mathf.Lerp(20f, 60f, 1f - (dist / radius)));
                    RPC_RequestDamage(requester, kvp.Key, dmg);
                }
            }
        }
    }

    // ========================================================================
    // TORRETA
    // ========================================================================
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUseTurret(PlayerRef requester, Vector3 position)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules()) return;
        if (State != MatchState.Playing) return;

        if (!Players.TryGet(requester, out PlayerCombatData data)) return;
        if (!data.HasTurret) return;

        data.HasTurret = false;
        Players.Set(requester, data);

        if (turretPrefab.IsValid)
        {
            // EL CAMBIO ESTÁ AQUÍ: Usamos un callback para decirle quién es su dueño
            Runner.Spawn(turretPrefab, position, Quaternion.identity, requester, 
                (runner, obj) => 
                {
                    TurretController turret = obj.GetComponent<TurretController>();
                    if (turret != null)
                    {
                        turret.Owner = requester; // ¡Ya sabe que no debe dispararte a ti!
                    }
                });
        }
    }

    // ========================================================================
    // EVENTOS DE CAMBIO DE ESTADO (UI y Lobby)
    // ========================================================================
    public void OnStateChanged()
    {
        if (State == MatchState.Playing)
        {
            Debug.Log("¡LA PARTIDA HA COMENZADO!");
        }
        else if (State == MatchState.Ended)
        {
            Debug.Log("¡FIN DE LA PARTIDA!");
            StartCoroutine(ReturnToLobbyRoutine());
        }
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        // 10 segundos para ver el Scoreboard
        yield return new WaitForSeconds(10f);

        Debug.Log("Desconectando y volviendo al Lobby...");

        if (Runner != null)
        {
            Runner.Shutdown();
        }

        SceneManager.LoadScene("LobbyScene"); 
    }
}