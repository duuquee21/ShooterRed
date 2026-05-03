using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    [Networked, OnChangedRender(nameof(OnScoreChanged))]
    public int Score { get; set; }

    [Networked, OnChangedRender(nameof(OnStatsChanged))]
    public int Kills { get; set; }

    [Networked, OnChangedRender(nameof(OnStatsChanged))]
    public int Deaths { get; set; }

    [Networked, OnChangedRender(nameof(OnStreakChanged))]
    public int Streak { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    // ========================================================================
    // ESTADO DEL ARMA
    // ========================================================================
    [Networked, OnChangedRender(nameof(OnCurrentWeaponChanged))]
    public int CurrentWeaponId { get; set; }

    [Networked, OnChangedRender(nameof(OnCurrentWeaponChanged))]
    public int CurrentWeaponRarity { get; set; }

    [Header("Referencias Visuales")]
    public PlayerWeaponVisuals weaponVisuals; 

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Health = 100;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            Streak = 0;
            
            // Arma base al nacer
            CurrentWeaponId = 0; 
            CurrentWeaponRarity = (int)WeaponRarity.Normal;

            string name = NetworkManager.Instance != null ? NetworkManager.Instance.LocalPlayerName : "Jugador";
            PlayerName = name;
        }

        // Forzamos actualización inicial visual
        OnPlayerNameChanged();
        OnHealthChanged();
        OnCurrentWeaponChanged();
    }

    // ========================================================================
    // NUEVO: INTERACCIÓN SEGURA CON OBJETOS DEL MAPA (RPC)
    // El cliente pide al servidor interactuar con una caja usando su propio personaje
    // ========================================================================
    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_InteractWithPickup(NetworkId pickupId)
    {
        // 1. El servidor busca la caja en el mapa usando el DNI
        if (Runner.TryFindObject(pickupId, out NetworkObject pickupObj))
        {
            WeaponPickup pickup = pickupObj.GetComponent<WeaponPickup>();
            
            // 2. Si la caja existe y nadie la ha cogido aún...
            if (pickup != null && !pickup.IsConsumed)
            {
                pickup.IsConsumed = true; // Bloqueamos para que nadie más la coja al mismo tiempo
                
                // 3. ¡Nos ponemos el arma! (Modificamos el [Networked] de este PlayerState)
                CurrentWeaponId = pickup.WeaponId;
                CurrentWeaponRarity = pickup.RarityLevel;
                
                Debug.Log($"[Servidor] El jugador {Object.InputAuthority} recogió el arma {CurrentWeaponId}");
                
                // 4. Destruimos la caja del mapa
                Runner.Despawn(pickupObj);
            }
        }
        else
        {
            Debug.LogWarning($"[Servidor] No se encontró el objeto interactuable con ID {pickupId}. Quizás alguien lo recogió antes.");
        }
    }

    // ========================================================================
    // CALLBACKS VISUALES
    // ========================================================================
    private void OnPlayerNameChanged() => Debug.Log($"Nombre: {PlayerName}");
    private void OnHealthChanged() => Debug.Log($"Vida: {Health}");
    private void OnScoreChanged() => Debug.Log($"Score: {Score}");
    private void OnStatsChanged() => Debug.Log($"Kills: {Kills}");
    private void OnStreakChanged() => Debug.Log($"Racha: {Streak}");

    // ========================================================================
    // REACCIÓN VISUAL DEL ARMA
    // ========================================================================
    public void OnCurrentWeaponChanged()
    {
        Debug.Log($"[DOMINÓ 2] El PlayerState de [Player:{Object.InputAuthority}] ha detectado que su ID de arma ahora es: {CurrentWeaponId}");

        if (weaponVisuals != null)
        {
            weaponVisuals.RefreshVisuals();
        }
        else
        {
            Debug.LogError("[ERROR CRÍTICO] ¡weaponVisuals ESTÁ VACÍO! Falta arrastrar el script PlayerWeaponVisuals al Inspector del PlayerState.");
        }
    }
}