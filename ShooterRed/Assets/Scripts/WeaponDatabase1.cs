using UnityEngine;

// 1. Rarezas con multiplicadores claros
public enum WeaponRarity 
{ 
    Normal = 0,   // Daño x1.0
    Especial = 1, // Daño x1.5
    Epico = 2     // Daño x2.0
}

public struct WeaponDefinition
{
    public int WeaponId;
    public string DisplayName;
    public int BaseDamage;
    public float FireRate; 
    public int MaxAmmo;
    public bool IsAutomatic; // <--- NUEVO: ¿Permite mantener pulsado?
}

public static class WeaponDatabase
{
    public static WeaponDefinition Get(int id)
    {
        return id switch
        {
            // Pistola: Semiautomática
            0 => new WeaponDefinition { WeaponId = 0, DisplayName = "Pistola Base", BaseDamage = 10, FireRate = 0.3f, MaxAmmo = 15, IsAutomatic = false },
            
            // Rifle: AUTOMÁTICO
            1 => new WeaponDefinition { WeaponId = 1, DisplayName = "Rifle de Asalto", BaseDamage = 25, FireRate = 0.1f, MaxAmmo = 30, IsAutomatic = true },
            
            // Escopeta: Semiautomática
            2 => new WeaponDefinition { WeaponId = 2, DisplayName = "Escopeta Pesada", BaseDamage = 80, FireRate = 0.8f, MaxAmmo = 5, IsAutomatic = false },
            
            // Sniper: Semiautomático
            3 => new WeaponDefinition { WeaponId = 3, DisplayName = "Francotirador", BaseDamage = 100, FireRate = 1.5f, MaxAmmo = 3, IsAutomatic = false },
            
            _ => new WeaponDefinition { WeaponId = -1, DisplayName = "Desarmado", IsAutomatic = false }
        };
    }

    // El estándar de la industria es centralizar el cálculo del daño final aquí
    public static int GetFinalDamage(int id, int rarityLevel)
    {
        WeaponDefinition def = Get(id);
        if (def.WeaponId == -1) return 0;

        float multiplier = rarityLevel switch
        {
            1 => 1.5f, // Especial
            2 => 2.0f, // Epico
            _ => 1.0f  // Normal
        };

        return Mathf.RoundToInt(def.BaseDamage * multiplier);
    }
}