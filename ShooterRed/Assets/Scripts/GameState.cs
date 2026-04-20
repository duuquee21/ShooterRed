using Fusion;
using UnityEngine;

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
}

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Reglas globales")]
    [Networked] public int ScoreLimit { get; set; }
    [Networked] public MatchState State { get; set; }
    [Networked] public TickTimer MatchTimer { get; set; }

    [Header("Datos globales por jugador")]
    [Networked, Capacity(16)]
    public NetworkDictionary<PlayerRef, PlayerCombatData> Players => default;

    public bool IsNetworkReady => Object != null && Runner != null;

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

        if (HasStateAuthority)
        {
            ScoreLimit = 15;
            State = MatchState.Waiting;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public bool CanValidateGlobalRules()
    {
        return HasStateAuthority;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRegisterPlayer(PlayerRef player)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules())
            return;

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
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(PlayerRef attacker, PlayerRef target, int damage)
    {
        if (!IsNetworkReady || !CanValidateGlobalRules())
            return;

        if (State != MatchState.Playing)
            return;

        if (damage <= 0 || damage > 100)
            return;

        if (!Players.ContainsKey(attacker) || !Players.ContainsKey(target))
            return;

        if (attacker == target)
            return;

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

        targetData.Deaths += 1;
        targetData.Streak = 0;
        targetData.Health = 0;
        Players.Set(target, targetData);

        PlayerCombatData attackerData = Players[attacker];
        attackerData.Kills += 1;
        attackerData.Streak += 1;
        attackerData.Score += 100;
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
        Debug.Log("Kill validada por Master: " + attacker + " elimino a " + victim + " | Racha: " + streak);
    }

    public bool TryGetPlayerData(PlayerRef player, out PlayerCombatData data)
    {
        data = default;

        if (!IsNetworkReady)
            return false;

        return Players.TryGet(player, out data);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyDeath(PlayerRef victim, float respawnDelay)
    {
        if (Runner == null || Runner.LocalPlayer != victim)
            return;

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

        SimpleSpawner.Instance.RespawnLocalPlayerAfterDelay(respawnDelay);
    }
}