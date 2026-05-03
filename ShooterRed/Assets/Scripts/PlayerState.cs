using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))] public int Health { get; set; }
    [Networked, OnChangedRender(nameof(OnScoreChanged))] public int Score { get; set; }
    [Networked, OnChangedRender(nameof(OnStatsChanged))] public int Kills { get; set; }
    [Networked, OnChangedRender(nameof(OnStatsChanged))] public int Deaths { get; set; }
    [Networked, OnChangedRender(nameof(OnStreakChanged))] public int Streak { get; set; }
    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))] public NetworkString<_32> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnCurrentWeaponChanged))] public int CurrentWeaponId { get; set; }
    [Networked, OnChangedRender(nameof(OnCurrentWeaponChanged))] public int CurrentWeaponRarity { get; set; }

    [Header("Referencias Visuales")]
    public PlayerWeaponVisuals weaponVisuals; 

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Health = 100; Score = 0; Kills = 0; Deaths = 0; Streak = 0;
            CurrentWeaponId = 0; 
            CurrentWeaponRarity = (int)WeaponRarity.Normal;
            // El nombre ya fue asignado en SimpleSpawner.SpawnPlayer() callback
        }

        OnPlayerNameChanged();
        OnHealthChanged();
        OnCurrentWeaponChanged();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_InteractWithPickup(NetworkId pickupId)
    {
        if (Runner.TryFindObject(pickupId, out NetworkObject pickupObj))
        {
            WeaponPickup pickup = pickupObj.GetComponent<WeaponPickup>();
            
            if (pickup != null && !pickup.IsConsumed)
            {
                // 1. Nos equipamos el arma (¡Funciona porque somos dueños de nuestro propio cuerpo!)
                CurrentWeaponId = pickup.WeaponId;
                CurrentWeaponRarity = pickup.RarityLevel;
                
                // 2. Le pedimos al dueño del mapa (Host) que consuma la caja de forma oficial
                pickup.RPC_ConsumeWeapon();
            }
        }
    }

    private void OnPlayerNameChanged()
    {
        Debug.Log($"[PlayerState OnPlayerNameChanged] Nombre replicado: '{PlayerName}'");
        // Sincronizar el nombre en el diccionario de GameState para que ScoreboardHud lo vea siempre
        if (GameState.TryGetInstance(out GameState gs) && Runner != null)
            gs.RPC_UpdatePlayerName(Object.StateAuthority, PlayerName.ToString());
    }
    private void OnHealthChanged() => Debug.Log($"Vida: {Health}");
    private void OnScoreChanged() => Debug.Log($"Score: {Score}");
    private void OnStatsChanged() => Debug.Log($"Kills: {Kills}");
    private void OnStreakChanged() => Debug.Log($"Racha: {Streak}");

    public void OnCurrentWeaponChanged()
    {
        if (weaponVisuals != null)
        {
            weaponVisuals.RefreshVisuals();
        }
    }
}