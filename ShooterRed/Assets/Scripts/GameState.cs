using Fusion;
using UnityEngine;

// Los tres estados posibles de la partida
public enum MatchState : byte
{
    Waiting = 0,  // esperando que haya al menos 2 jugadores
    Playing = 1,  // partida en curso, se puede disparar
    Ended = 2     // alguien llegó al ScoreLimit, partida terminada
}

// Estructura con todos los datos de combate de un jugador
// INetworkStruct es necesario para que Fusion pueda replicarla por red
public struct PlayerCombatData : INetworkStruct
{
    public int Health;   // vida actual (0 = muerto)
    public int Kills;    // eliminaciones totales
    public int Deaths;   // veces que ha muerto
    public int Streak;   // racha actual de kills sin morir
    public int Score;    // puntuación total acumulada
    public bool HasGrenade;
    public bool HasAirstrike;
    public bool HasTurret;
}

// GameState es el árbitro de la partida.
// Es el único que puede modificar stats, validar daño y resolver muertes.
// Hereda de NetworkBehaviour para existir como objeto de red en Fusion.
public class GameState : NetworkBehaviour
{
    // Singleton para acceder a GameState desde cualquier script sin buscarlo
    public static GameState Instance { get; private set; }

    [Header("Reglas globales")]
    [Networked] public int ScoreLimit { get; set; }       // kills necesarias para ganar
    [Networked] public MatchState State { get; set; }     // estado actual de la partida (replicado en red)
    [Networked] public TickTimer MatchTimer { get; set; } // temporizador de red (no usado aún)


    [Header("Recompensas de racha")]
    [SerializeField] private NetworkPrefabRef turretPrefab; // prefab de la torreta registrado en Fusion
    [Header("Umbrales de racha (kills necesarios)" )]
    [SerializeField] private int grenadeStreakThreshold  = 3;
    [SerializeField] private int airstrikeStreakThreshold = 5;
    [SerializeField] private int turretStreakThreshold   = 10;
    [Header("Datos globales por jugador")]
    // Diccionario replicado: todos los clientes ven la misma tabla en tiempo real
    // Clave = ID del jugador, Valor = sus stats de combate
    [Networked, Capacity(16)]
    public NetworkDictionary<PlayerRef, PlayerCombatData> Players => default;

    // Flag que indica que Spawned() ya se ejecutó y las propiedades [Networked] son accesibles
    private bool _isSpawned;

    // IsNetworkReady: solo para LEER propiedades de red (evita el crash de acceso prematuro)
    public bool IsNetworkReady => _isSpawned && Object != null && Runner != null;

    // CanRegister: para el RPC de registro no exigimos _isSpawned para evitar problemas de timing
    private bool CanRegister => Object != null && Runner != null && HasStateAuthority;

    // Devuelve la instancia de GameState de forma segura desde cualquier script
    public static bool TryGetInstance(out GameState gameState)
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<GameState>();
        }

        gameState = Instance;
        return gameState != null;
    }

    // Spawned() se ejecuta cuando este objeto de red aparece en la sesión
    public override void Spawned()
    {
        Instance = this;
        _isSpawned = true; // a partir de aquí las propiedades [Networked] son accesibles

        // Solo el State Authority inicializa los valores globales
        // Si lo hicieran todos los clientes, habría conflictos
        if (HasStateAuthority)
        {
            ScoreLimit = 15;
            State = MatchState.Waiting;
        }
    }

    // Se limpia la referencia cuando el objeto desaparece de la sesión
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSpawned = false;
        if (Instance == this)
            Instance = null;
    }

    // Solo el State Authority puede validar reglas globales (daño, kills, etc.)
    public bool CanValidateGlobalRules()
    {
        return HasStateAuthority;
    }

    // RPC de cliente → autoridad: registra a un jugador en el diccionario al entrar o al respawnear
    // RpcSources.All = cualquier cliente puede llamarlo
    // RpcTargets.StateAuthority = solo lo ejecuta quien tiene la autoridad
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRegisterPlayer(PlayerRef player)
    {
        // Usamos CanRegister en lugar de IsNetworkReady para no depender de _isSpawned
        if (!CanRegister)
            return;

        // Si ya existe (viene de un respawn), solo le restaura la vida y resetea la racha
        if (Players.ContainsKey(player))
        {
            PlayerCombatData existingData = Players[player];
            existingData.Health = 100;
            existingData.Streak = 0;
            Players.Set(player, existingData);
            return;
        }

        // Primera vez que entra: crea sus datos desde cero
        PlayerCombatData data = new PlayerCombatData
        {
            Health = 100,
            Kills = 0,
            Deaths = 0,
            Streak = 0,
            Score = 0
        };

        Players.Set(player, data);

        // Si ya hay 2 o más jugadores, arranca la partida
        if (State == MatchState.Waiting && Players.Count >= 2)
        {
            State = MatchState.Playing;
        }
    }

    // RPC de cliente → autoridad: solicita aplicar daño a un jugador
    // El cliente NUNCA aplica el daño por su cuenta, solo lo solicita aquí
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(PlayerRef attacker, PlayerRef target, int damage)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules())
            return;

        // No se puede disparar si la partida no está activa
        if (State != MatchState.Playing)
            return;

        // Validación de seguridad: el daño debe estar en un rango razonable
        if (damage <= 0 || damage > 100)
            return;

        // Ambos jugadores deben estar registrados en la partida
        if (!Players.ContainsKey(attacker) || !Players.ContainsKey(target))
            return;

        // No te puedes disparar a ti mismo
        if (attacker == target)
            return;

        // Leemos los datos actuales del objetivo del diccionario de red
        PlayerCombatData targetData = Players[target];
        targetData.Health -= damage;

        // Si sigue vivo después del daño, solo actualizamos su vida
        if (targetData.Health > 0)
        {
            Players.Set(target, targetData);

            // Sincronizamos también el componente PlayerState del avatar para que
            // los callbacks visuales ([OnChangedRender]) se disparen correctamente
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

        // Actualizamos stats de la víctima
        targetData.Deaths += 1;
        targetData.Streak = 0;    // se rompe la racha al morir
        targetData.Health = 0;
        // Al morir se pierden TODAS las recompensas pendientes
        targetData.HasGrenade = false;
        targetData.HasAirstrike = false;
        targetData.HasTurret = false;
        Players.Set(target, targetData);

        // Actualizamos stats del atacante
        PlayerCombatData attackerData = Players[attacker];
        attackerData.Kills += 1;
        attackerData.Streak += 1;  // suma a la racha del que mató
        attackerData.Score += 100; // 100 puntos por kill

        // Desbloquear recompensas de racha ANTES de guardar en el diccionario
        if (attackerData.Streak >= grenadeStreakThreshold)  attackerData.HasGrenade   = true;
        if (attackerData.Streak >= airstrikeStreakThreshold) attackerData.HasAirstrike = true;
        if (attackerData.Streak >= turretStreakThreshold)   attackerData.HasTurret    = true;

        // Guardamos todos los cambios (kills, streak, score y recompensas) de una vez
        Players.Set(attacker, attackerData);

        // Notificamos a todos los clientes: aparece en el kill feed
        RPC_KillFeed(attacker, target, attackerData.Streak);

        // Notificamos solo a la víctima para que inicie su proceso de respawn
        RPC_NotifyDeath(target, 5f);

        // Si el atacante llegó al límite de kills, la partida termina
        if (attackerData.Kills >= ScoreLimit)
        {
            State = MatchState.Ended;
        }
    }

    // RPC de autoridad → todos: notifica una kill para mostrarla en el kill feed
    // Va en sentido contrario: la autoridad informa a todos los clientes
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

    // Devuelve el nombre del jugador desde su PlayerState, o "Jugador X" como fallback
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

    // Método de lectura segura del diccionario para otros scripts (HUD, Scoreboard...)
    public bool TryGetPlayerData(PlayerRef player, out PlayerCombatData data)
    {
        data = default;

        if (!IsNetworkReady)
            return false;

        return Players.TryGet(player, out data);
    }

    // RPC de autoridad → todos: notifica la muerte de un jugador concreto
    // Cada cliente lo recibe, pero solo actúa el que es la víctima
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyDeath(PlayerRef victim, float respawnDelay)
    {
        // Solo actúa el cliente cuyo jugador local es la víctima
        if (Runner == null || Runner.LocalPlayer != victim)
            return;

        // Destruye el avatar de la víctima en la escena
        NetworkObject victimObject = Runner.GetPlayerObject(victim);
        if (victimObject != null)
        {
            PlayerCombatIntent combatIntent = victimObject.GetComponent<PlayerCombatIntent>();
            if (combatIntent != null)
            {
                combatIntent.DespawnOwnedAvatar();
            }
        }

        if (SimpleSpawner.Instance == null)
        {
            Debug.LogWarning("No existe SimpleSpawner para respawnear al jugador " + victim);
            return;
        }

        // Inicia la corrutina de espera antes de volver a aparecer
        SimpleSpawner.Instance.RespawnLocalPlayerAfterDelay(respawnDelay);
    }

    // RPC para usar la granada (racha 3) — daño en la posición exacta donde explota
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
        Debug.Log("Granada usada por: " + requester);
    }

    // RPC para usar el ataque aéreo (racha 5) — daño de área instantáneo radio amplio
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUseAirstrike(PlayerRef requester)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules()) return;
        if (State != MatchState.Playing) return;

        if (!Players.TryGet(requester, out PlayerCombatData data)) return;
        if (!data.HasAirstrike) return;

        data.HasAirstrike = false;
        Players.Set(requester, data);

        NetworkObject requesterObj = Runner.GetPlayerObject(requester);
        if (requesterObj == null) return;
        Vector3 center = requesterObj.transform.position;

        float radius = 15f;
        foreach (var kvp in Players)
        {
            if (kvp.Key == requester) continue;
            if (kvp.Value.Health <= 0) continue;
            NetworkObject enemyObj = Runner.GetPlayerObject(kvp.Key);
            if (enemyObj == null) continue;
            float dist = Vector3.Distance(center, enemyObj.transform.position);
            if (dist <= radius)
            {
                int dmg = Mathf.RoundToInt(Mathf.Lerp(25f, 75f, 1f - (dist / radius)));
                RPC_RequestDamage(requester, kvp.Key, dmg);
            }
        }
        Debug.Log("Ataque aereo usado por: " + requester);
    }

    // RPC para desplegar la torreta (racha 10)
    // No aplica daño directamente — spawnea un NetworkObject que actúa por su cuenta
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUseTurret(PlayerRef requester, Vector3 position)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules()) return;
        if (State != MatchState.Playing) return;

        if (!Players.TryGet(requester, out PlayerCombatData data)) return;
        if (!data.HasTurret) return;

        // Consume la recompensa
        data.HasTurret = false;
        Players.Set(requester, data);

        // Spawnea la torreta en la posición del jugador
        // El prefab debe estar registrado en Fusion Network Project Config
        if (turretPrefab.IsValid)
        {
            Runner.Spawn(turretPrefab, position, Quaternion.identity, requester);
        }

        Debug.Log("Torreta desplegada por: " + requester);
    }
}