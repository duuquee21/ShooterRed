using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    [Networked, OnChangedRender(nameof(OnScoreChanged))]
    public int Score { get; set; }

    [Networked]
    public int Kills { get; set; }

    [Networked]
    public int Deaths { get; set; }

    [Networked, OnChangedRender(nameof(OnStreakChanged))]
    public int Streak { get; set; }

    // Nombre del jugador replicado a todos los clientes (máx 32 caracteres)
    [Networked]
    public NetworkString<_32> PlayerName { get; set; }

    // Arma actualmente equipada y su rareza
    [Networked, OnChangedRender(nameof(OnWeaponChanged))]
    public int CurrentWeaponId { get; set; }

    [Networked, OnChangedRender(nameof(OnWeaponChanged))]
    public int CurrentWeaponRarity { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Health = 100;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            Streak = 0;
            CurrentWeaponId    = 0;
            CurrentWeaponRarity = 0;

            // Sincroniza el nombre que el jugador escribió en el menú
            string name = NetworkManager.Instance != null ? NetworkManager.Instance.LocalPlayerName : "Jugador";
            if (string.IsNullOrEmpty(name)) name = "Jugador";
            PlayerName = name;
        }

        OnHealthChanged();
        OnScoreChanged();
        OnStreakChanged();
        OnWeaponChanged();
    }

    private void OnHealthChanged()
    {
        Debug.Log("[Visual] Health actualizado: " + Health);
    }

    private void OnScoreChanged()
    {
        Debug.Log("[Visual] Score actualizado: " + Score);
    }

    private void OnStreakChanged()
    {
        Debug.Log("[Visual] Racha actualizada: " + Streak);
    }

    private void OnWeaponChanged()
    {
        var def = WeaponDatabase.Get(CurrentWeaponId);
        var rarity = (WeaponRarity)CurrentWeaponRarity;
        Debug.Log($"[Visual] Arma: {def.DisplayName} ({rarity.Label()})");
    }
}