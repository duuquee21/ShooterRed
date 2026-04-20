using Fusion;
using UnityEngine;

// Pickup de arma en el escenario.
// Requiere en Unity: NetworkObject + Collider (Is Trigger) + MeshRenderer en algún hijo.
// La rareza se asigna en el Inspector; la authority la aplica en Spawned().
public class WeaponPickup : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int          editorWeaponId = 1;
    [SerializeField] private WeaponRarity editorRarity   = WeaponRarity.Common;

    // Estado replicado: todos los clientes saben qué arma y rareza representa
    [Networked] public int WeaponId     { get; set; }
    [Networked] public int RarityLevel  { get; set; }

    private MeshRenderer _renderer;
    private bool _consumed;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            WeaponId    = editorWeaponId;
            RarityLevel = (int)editorRarity;
        }

        _renderer = GetComponentInChildren<MeshRenderer>();
        ApplyRarityColor();
    }

    // Render() se llama cada fotograma en todos los clientes → mantiene el color actualizado
    public override void Render()
    {
        ApplyRarityColor();
        // Rotación visual para hacerlo visible en el escenario
        transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
    }

    // Cuando el CharacterController del jugador local entra en el trigger
    private void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;

        // Solo actúa el cliente cuyo jugador tocó el pickup
        PlayerCombatIntent pci = other.GetComponentInParent<PlayerCombatIntent>();
        if (pci == null || !pci.HasInputAuthority) return;

        _consumed = true; // evita que el mismo cliente llame al RPC dos veces
        RPC_RequestPickup(Runner.LocalPlayer);
    }

    // La solicitud va a la StateAuthority del pickup (quien lo spawneó / master client)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPickup(PlayerRef requester)
    {
        NetworkObject playerObj = Runner.GetPlayerObject(requester);
        if (playerObj == null) return;

        PlayerState ps = playerObj.GetComponent<PlayerState>();
        if (ps == null) return;

        ps.CurrentWeaponId    = WeaponId;
        ps.CurrentWeaponRarity = RarityLevel;

        // Elimina el pickup del mundo compartido para todos los clientes
        Runner.Despawn(Object);
    }

    private void ApplyRarityColor()
    {
        if (_renderer == null) return;
        _renderer.material.color = ((WeaponRarity)RarityLevel).RarityColor();
    }
}
