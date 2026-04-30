using UnityEngine;

public class PlayerWeaponVisuals : MonoBehaviour
{
    [Header("Modelos 3D de las Armas")]
    [Tooltip("El hueco 0 déjalo vacío. El hueco 1 pon el Rifle. El hueco 2 pon el Sniper.")]
    public GameObject[] weaponModels;

    // Esta función la llamará el PlayerState cuando cambie la variable de red
    public void EquipWeaponVisual(int weaponId)
    {
        // 1. Apagamos TODAS las armas por seguridad
        foreach (var model in weaponModels)
        {
            if (model != null) model.SetActive(false);
        }

        // 2. Si el ID es 0 (Desarmado) o no existe, nos quedamos con las manos vacías
        if (weaponId <= 0 || weaponId >= weaponModels.Length) 
        {
            return; 
        }

        // 3. Encendemos SOLO el modelo 3D del arma que toca
        if (weaponModels[weaponId] != null)
        {
            weaponModels[weaponId].SetActive(true);
        }
    }
}