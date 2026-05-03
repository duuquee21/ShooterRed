using Fusion;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [Header("Configuración en el Editor")]
    public int editorWeaponId = 1; 
    public WeaponRarity editorRarity = WeaponRarity.Normal;
    
    public float respawnTime = 10f; 

    [Header("Efectos de Rareza")]
    public Light rarityLight; 
    public Renderer rarityBeam; 

    [Header("Animación Visual")]
    public Transform visualModel; 
    public Collider pickupCollider; 
    public float spinSpeed = 90f; 
    public float bobSpeed = 2f;   
    public float bobHeight = 0.15f; 
    private float _startVisualY;

    [Networked] public int WeaponId { get; set; }
    [Networked] public int RarityLevel { get; set; } 
    [Networked] public NetworkBool IsConsumed { get; set; } 

    [Networked] private TickTimer RespawnTimer { get; set; }
    [Networked] private NetworkBool _wasConsumed { get; set; }

    private bool _canPickupLocal = false;
    private PlayerState _localPlayerState = null;
    private Renderer[] _allRenderers; 
    private int _lastRarityLevel = -1; 

    public override void Spawned()
    {
        if (visualModel != null)
        {
            _startVisualY = visualModel.localPosition.y;
            _allRenderers = visualModel.GetComponentsInChildren<Renderer>(true);
        }

        if (pickupCollider == null) pickupCollider = GetComponent<Collider>();

        if (HasStateAuthority)
        {
            WeaponId = editorWeaponId;
            RerollRarity();
            IsConsumed = false;
            _wasConsumed = false;
        }

        ApplyRarityVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsConsumed && !_wasConsumed)
        {
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
        }
        _wasConsumed = IsConsumed;

        if (IsConsumed && RespawnTimer.Expired(Runner))
        {
            RerollRarity();
            IsConsumed = false;
            _wasConsumed = false;
            RespawnTimer = TickTimer.None;
        }
    }

    // ========================================================================
    // NUEVO: EL HOST EJECUTA ESTO CUANDO EL CLIENTE SE LO PIDE
    // ========================================================================
    // RpcTargets.All → todos los clientes reciben este RPC y ocultan el pickup al instante
    // Solo el StateAuthority actualiza IsConsumed (estado autoritativo para el respawn timer)
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ConsumeWeapon()
    {
        // Todos los clientes ocultan visualmente el pickup de forma inmediata
        if (_allRenderers != null)
            foreach (var r in _allRenderers)
                if (r != null) r.enabled = false;

        if (rarityLight  != null) rarityLight.enabled  = false;
        if (rarityBeam   != null) rarityBeam.enabled   = false;
        if (pickupCollider != null) pickupCollider.enabled = false;

        // Solo el dueño del objeto actualiza la variable de red (para el timer de respawn)
        if (HasStateAuthority && !IsConsumed)
            IsConsumed = true;
    }

    private void RerollRarity()
    {
        int luck = Random.Range(1, 101);
        if (luck <= 60)      RarityLevel = (int)WeaponRarity.Normal;
        else if (luck <= 90) RarityLevel = (int)WeaponRarity.Especial;
        else                 RarityLevel = (int)WeaponRarity.Epico;
    }

    private void ApplyRarityVisuals()
    {
        Color rarityColor = Color.white; 
        if (RarityLevel == (int)WeaponRarity.Especial) rarityColor = Color.cyan; 
        if (RarityLevel == (int)WeaponRarity.Epico) rarityColor = new Color(0.6f, 0f, 1f); 

        if (rarityLight != null) rarityLight.color = rarityColor;
        if (rarityBeam != null)
        {
            rarityBeam.material.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.4f);
            rarityBeam.material.EnableKeyword("_EMISSION");
            rarityBeam.material.SetColor("_EmissionColor", rarityColor * 1.5f);
        }
    }

    public override void Render()
    {
        if (Object == null || !Object.IsValid) return;

        bool isVisible = !IsConsumed;
        
        if (_allRenderers != null)
        {
            foreach (var r in _allRenderers)
            {
                if (r != null && r.enabled != isVisible) r.enabled = isVisible;
            }
        }

        if (rarityLight != null && rarityLight.enabled != isVisible) rarityLight.enabled = isVisible;
        if (rarityBeam != null && rarityBeam.enabled != isVisible) rarityBeam.enabled = isVisible;
        if (pickupCollider != null && pickupCollider.enabled != isVisible) pickupCollider.enabled = isVisible;

        if (RarityLevel != _lastRarityLevel)
        {
            _lastRarityLevel = RarityLevel;
            ApplyRarityVisuals();
        }

        if (IsConsumed && _canPickupLocal)
        {
            _canPickupLocal = false;
            if (InteractionMessage.Instance != null) InteractionMessage.Instance.Show(""); 
        }

        if (isVisible && visualModel != null)
        {
            visualModel.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
            float newY = _startVisualY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visualModel.localPosition = new Vector3(visualModel.localPosition.x, newY, visualModel.localPosition.z);
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;

        if (_canPickupLocal && _localPlayerState != null && !IsConsumed)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                _canPickupLocal = false;
                
                if (InteractionMessage.Instance != null) 
                    InteractionMessage.Instance.Show("¡Arma recogida!", 2f); 
                
                _localPlayerState.RPC_InteractWithPickup(Object.Id);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.IsValid || IsConsumed) return;
        
        PlayerState ps = other.GetComponentInParent<PlayerState>();
        if (ps != null && ps.HasInputAuthority)
        {
            _canPickupLocal = true;
            _localPlayerState = ps;
            
            if (InteractionMessage.Instance != null)
                InteractionMessage.Instance.Show("PULSA 'F' PARA RECOGER", 9999f); 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.IsValid) return;

        PlayerState ps = other.GetComponentInParent<PlayerState>();
        if (ps != null && ps.HasInputAuthority)
        {
            _canPickupLocal = false;
            _localPlayerState = null;
            
            if (InteractionMessage.Instance != null)
                InteractionMessage.Instance.Show(""); 
        }
    }
}