using Fusion;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int editorWeaponId = 1;
    [SerializeField] private WeaponRarity editorRarity = WeaponRarity.Common;

    // NUEVO: Referencia a tu script de mensaje flotante
    [Header("UI Local")]
    public PickupFloatingMessage floatingMessage;

    // Estado replicado
    [Networked] public int WeaponId { get; set; }
    [Networked] public int RarityLevel { get; set; }
    [Networked] public NetworkBool IsPickedUp { get; set; }

    private MeshRenderer _renderer;
    private bool _canPickup = false;
    private PlayerCombatIntent _localPlayerInTrigger = null;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            WeaponId = editorWeaponId;
            RarityLevel = (int)editorRarity;
            IsPickedUp = false;
        }

        _renderer = GetComponentInChildren<MeshRenderer>();
        ApplyRarityColor(); 
        
        // Nos aseguramos de que el texto empiece oculto
        if (floatingMessage != null) 
            floatingMessage.Show(false);
    }

    public override void Render()
    {
        transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPickedUp) return; 

        PlayerCombatIntent pci = other.GetComponentInParent<PlayerCombatIntent>();
        if (pci == null || !pci.HasInputAuthority) return;

        _canPickup = true;
        _localPlayerInTrigger = pci;
        
        // NUEVO: Mostramos el cartel flotante solo a nosotros
        if (floatingMessage != null)
            floatingMessage.Show(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCombatIntent pci = other.GetComponentInParent<PlayerCombatIntent>();
        if (pci != null && pci.HasInputAuthority)
        {
            _canPickup = false;
            _localPlayerInTrigger = null;
            
            // NUEVO: Ocultamos el cartel al alejarnos
            if (floatingMessage != null)
                floatingMessage.Show(false);
        }
    }

    private void Update()
    {
        if (_canPickup && _localPlayerInTrigger != null && !IsPickedUp)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                _canPickup = false;
                _localPlayerInTrigger = null;
                
                // NUEVO: Ocultamos el cartel inmediatamente al recoger el arma
                if (floatingMessage != null)
                    floatingMessage.Show(false);

                RPC_RequestPickup(Runner.LocalPlayer);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPickup(PlayerRef requester)
    {
        if (IsPickedUp) return; 
        IsPickedUp = true;
        RPC_GrantPickup(requester, WeaponId, RarityLevel);
        Runner.Despawn(Object);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GrantPickup([RpcTarget] PlayerRef player, int grantedWeaponId, int grantedRarity)
    {
        NetworkObject playerObj = Runner.GetPlayerObject(player);
        if (playerObj == null) return;

        PlayerState ps = playerObj.GetComponent<PlayerState>();
        if (ps == null) return;

        ps.CurrentWeaponId = grantedWeaponId;
        ps.CurrentWeaponRarity = grantedRarity;
    }

    private void ApplyRarityColor()
    {
        if (_renderer == null) return;
        _renderer.material.color = ((WeaponRarity)RarityLevel).RarityColor();
    }
}