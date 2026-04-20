// Datos estáticos de cada arma. No se replican por red: todos los clientes comparten
// la misma tabla y solo se replica el identificador equipado en PlayerState.
public struct WeaponDefinition
{
    public int    WeaponId;
    public string DisplayName;
    public int    BaseDamage;
    public float  BaseFireRate; // segundos entre disparos
}

public static class WeaponDatabase
{
    // Tabla de armas. El índice debe coincidir con WeaponId.
    public static readonly WeaponDefinition[] All = new WeaponDefinition[]
    {
        new WeaponDefinition { WeaponId = 0, DisplayName = "Pistola",  BaseDamage = 25, BaseFireRate = 0.40f },
        new WeaponDefinition { WeaponId = 1, DisplayName = "Rifle",    BaseDamage = 35, BaseFireRate = 0.15f },
        new WeaponDefinition { WeaponId = 2, DisplayName = "Escopeta", BaseDamage = 55, BaseFireRate = 0.90f },
    };

    public static WeaponDefinition Get(int id)
    {
        if (id >= 0 && id < All.Length) return All[id];
        return All[0]; // fallback: Pistola
    }

    // Cada nivel de rareza aumenta el daño base un 25 %
    public static int GetDamage(int weaponId, int rarityLevel)
    {
        var def = Get(weaponId);
        return UnityEngine.Mathf.RoundToInt(def.BaseDamage * (1f + rarityLevel * 0.25f));
    }

    // Cada nivel de rareza reduce el tiempo entre disparos un 5 %
    public static float GetFireRate(int weaponId, int rarityLevel)
    {
        var def = Get(weaponId);
        return UnityEngine.Mathf.Max(0.05f, def.BaseFireRate * (1f - rarityLevel * 0.05f));
    }
}
