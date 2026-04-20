using UnityEngine;

public enum WeaponRarity : byte
{
    Common    = 0,
    Uncommon  = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4
}

public static class WeaponRarityExtensions
{
    public static string Label(this WeaponRarity r) => r switch
    {
        WeaponRarity.Common    => "Común",
        WeaponRarity.Uncommon  => "Poco común",
        WeaponRarity.Rare      => "Raro",
        WeaponRarity.Epic      => "Épico",
        WeaponRarity.Legendary => "Legendario",
        _                      => "?"
    };

    public static Color RarityColor(this WeaponRarity r) => r switch
    {
        WeaponRarity.Common    => Color.white,
        WeaponRarity.Uncommon  => Color.green,
        WeaponRarity.Rare      => new Color(0.3f, 0.6f, 1f),    // azul
        WeaponRarity.Epic      => new Color(0.65f, 0.2f, 1f),   // morado
        WeaponRarity.Legendary => new Color(1f, 0.6f, 0.05f),   // dorado
        _                      => Color.white
    };
}
