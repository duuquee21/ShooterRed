using Fusion;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [Header("Configuración en el Editor")]
    public int editorWeaponId = 1; 
    public WeaponRarity editorRarity = WeaponRarity.Normal;

    [Header("Efectos de Rareza")]
    public Light rarityLight; // La bombilla en el suelo
    public Renderer rarityBeam; // El pilar de luz (cilindro)

    [Header("Animación Visual")]
    public Transform visualModel; // El modelo 3D de tu arma
    public float spinSpeed = 90f; // Velocidad de giro
    public float bobSpeed = 2f;   // Velocidad de subida/bajada
    public float bobHeight = 0.15f; // Cuánto sube y baja
    private float _startVisualY;

    // Estado del Pickup en el mundo compartido
    [Networked] public int WeaponId { get; set; }
    [Networked] public int RarityLevel { get; set; } 
    [Networked] public NetworkBool IsConsumed { get; set; }

    private bool _canPickupLocal = false;
    private PlayerState _localPlayerState = null;

    public override void Spawned()
    {
        // Solo el Servidor/Host elige la rareza aleatoria
        if (HasStateAuthority)
        {
            WeaponId = editorWeaponId;
            
            // Tiramos un dado del 1 al 100
            int luck = Random.Range(1, 101);

            if (luck <= 60)      // 60% de probabilidad
                RarityLevel = (int)WeaponRarity.Normal;
            else if (luck <= 90) // 30% de probabilidad
                RarityLevel = (int)WeaponRarity.Especial;
            else                 // 10% de probabilidad
                RarityLevel = (int)WeaponRarity.Epico;
            
            IsConsumed = false;
        }

        // Después de elegirla, guardamos su posición inicial para flotar
        if (visualModel != null)
            _startVisualY = visualModel.localPosition.y;

        ApplyRarityVisuals();
    }

    private void ApplyRarityVisuals()
    {
        Color rarityColor = Color.white; 
        if (RarityLevel == (int)WeaponRarity.Especial) rarityColor = Color.cyan; 
        if (RarityLevel == (int)WeaponRarity.Epico) rarityColor = new Color(0.6f, 0f, 1f); 

        if (rarityLight != null)
        {
            rarityLight.color = rarityColor;
        }

        if (rarityBeam != null)
        {
            rarityBeam.material.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.4f);
            rarityBeam.material.EnableKeyword("_EMISSION");
            rarityBeam.material.SetColor("_EmissionColor", rarityColor * 1.5f);
        }
    }

    private void Update()
    {
        // ==========================================
        // ANIMACIÓN: Rotar y Flotar
        // ==========================================
        if (visualModel != null)
        {
            visualModel.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
            float newY = _startVisualY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visualModel.localPosition = new Vector3(visualModel.localPosition.x, newY, visualModel.localPosition.z);
        }

        // ==========================================
        // LÓGICA DE RECOGIDA (Input Local)
        // ==========================================
        if (_canPickupLocal && _localPlayerState != null && !IsConsumed)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                _canPickupLocal = false;
                
                // Feedback visual inmediato en la UI
                if (InteractionMessage.Instance != null) 
                    InteractionMessage.Instance.Show("¡Arma recogida!", 2f); 
                
                // LA CLAVE ARQUITECTÓNICA: Usamos el PlayerState del cliente 
                // para enviar el RPC, mandando el ID de esta caja.
                _localPlayerState.RPC_InteractWithPickup(Object.Id);
            }
        }
    }

    // ==========================================
    // DETECCIÓN DEL JUGADOR
    // ==========================================
    private void OnTriggerEnter(Collider other)
    {
        if (IsConsumed) return;
        
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