using Fusion;
using UnityEngine;

public class PlayerWeaponVisuals : NetworkBehaviour
{
    private PlayerState _playerState;

    [Header("Modelos 3D (Hijos de la cámara)")]
    public GameObject pistolModel;
    public GameObject rifleModel;
    public GameObject shotgunModel;
    public GameObject sniperModel;

    public override void Spawned()
    {
        _playerState = GetComponent<PlayerState>();
        RefreshVisuals();
    }

    // Esta función se llama desde el Callback de red de PlayerState
    public void RefreshVisuals()
    {
        // Fallback por si acaso el Spawned no llegó a ejecutarse a tiempo
        if (_playerState == null) 
            _playerState = GetComponent<PlayerState>();

        if (_playerState == null)
        {
            Debug.LogError("[ERROR CRÍTICO] PlayerWeaponVisuals no puede encontrar el PlayerState en este objeto.");
            return;
        }

        int currentId = _playerState.CurrentWeaponId;
        Debug.Log($"[DOMINÓ 3] Visuals recibiendo orden de mostrar el arma ID: {currentId}");

        // 1. Apagar todo
        if (pistolModel) pistolModel.SetActive(false);
        else Debug.LogWarning("Falta asignar pistolModel en el Inspector");

        if (rifleModel) rifleModel.SetActive(false);
        if (shotgunModel) shotgunModel.SetActive(false);
        if (sniperModel) sniperModel.SetActive(false);

        // 2. Encender el actual
        switch (currentId)
        {
            case 0: if (pistolModel) pistolModel.SetActive(true); break;
            case 1: if (rifleModel) rifleModel.SetActive(true); break;
            case 2: if (shotgunModel) shotgunModel.SetActive(true); break;
            case 3: if (sniperModel) sniperModel.SetActive(true); break;
        }
    }
}