using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    [Networked, OnChangedRender(nameof(OnScoreChanged))]
    public int Score { get; set; }

    // ¡Extra! Le ponemos OnChangedRender a Kills y Deaths también 
    // por si luego quieres actualizar un marcador (Scoreboard)
    [Networked, OnChangedRender(nameof(OnStatsChanged))]
    public int Kills { get; set; }

    [Networked, OnChangedRender(nameof(OnStatsChanged))]
    public int Deaths { get; set; }

    [Networked, OnChangedRender(nameof(OnStreakChanged))]
    public int Streak { get; set; }

    // CORRECCIÓN 1: Añadido el OnChangedRender para que escuche los cambios de red
    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    // Arma actualmente equipada y su rareza
    [Networked, OnChangedRender(nameof(OnWeaponChanged))]
    public int CurrentWeaponId { get; set; }

    [Networked, OnChangedRender(nameof(OnWeaponChanged))]
    public int CurrentWeaponRarity { get; set; }

    [Header("Referencias Visuales")]
    public PlayerWeaponVisuals weaponVisuals; // <-- AÑADE ESTO

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Health = 100;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            Streak = 0;
            
            // ¡ESTE ES EL ÚNICO CAMBIO! 
            // 1 = Tu arma básica (Asegúrate de que el 1 sea el ID correcto)
            CurrentWeaponId = 1; 
            
            CurrentWeaponRarity = 0; // Rareza común (Common)

            string name = NetworkManager.Instance != null ? NetworkManager.Instance.LocalPlayerName : "Jugador";
            if (string.IsNullOrEmpty(name)) name = "Jugador";
            PlayerName = name;
        }

        // Forzamos la actualización visual para TODOS los clientes
        // Esto asegura que si entras a una partida ya empezada, veas todo tal y como está
        OnPlayerNameChanged();
        OnHealthChanged();
        OnScoreChanged();
        OnStatsChanged();
        OnStreakChanged();
        OnWeaponChanged();
    }

    // ========================================================================
    // CALLBACKS VISUALES (Se ejecutan solos cuando cambian las variables de red)
    // ========================================================================
    
    private void OnPlayerNameChanged()
    {
        // Aquí podrías actualizar el TextMeshPro flotante encima de la cabeza del jugador
        Debug.Log($"[PlayerState] Nombre replicado actualizado: {PlayerName}");
    }

    private void OnHealthChanged()
    {
        // Aquí podrías actualizar la barra de vida en el Canvas
        Debug.Log($"[Visual] Health actualizado: {Health}");
    }

    private void OnScoreChanged()
    {
        Debug.Log($"[Visual] Score actualizado: {Score}");
    }

    private void OnStatsChanged()
    {
        // Útil si pulsas la tecla TAB para ver las bajas y muertes
        Debug.Log($"[Visual] Stats - Kills: {Kills} | Deaths: {Deaths}");
    }

    private void OnStreakChanged()
    {
        Debug.Log($"[Visual] Racha actualizada: {Streak}");
    }

    private void OnWeaponChanged()
    {
        if (CurrentWeaponId <= 0)
        {
            Debug.Log("[Visual] Jugador desarmado.");
            // Le decimos al script visual que equipe el ID 0 (nada)
            if (weaponVisuals != null) weaponVisuals.EquipWeaponVisual(0); 
            return;
        }

        var def = WeaponDatabase.Get(CurrentWeaponId);
        var rarity = (WeaponRarity)CurrentWeaponRarity;
        
        Debug.Log($"[Visual] Arma equipada: {def.DisplayName} ({rarity})");
        
        // ¡AQUÍ ESTÁ LA MAGIA! Le pasamos el ID al script visual
        if (weaponVisuals != null) 
            weaponVisuals.EquipWeaponVisual(CurrentWeaponId);
    }
}