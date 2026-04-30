using UnityEngine;
using TMPro;

// Mensaje flotante sobre el pickup
public class PickupFloatingMessage : MonoBehaviour
{
    [Header("Referencias")]
    public Canvas floatingCanvas; // Canvas en World Space, hijo del pickup
    public TextMeshProUGUI messageText;

    private Camera _mainCamera;

    private void Awake()
    {
        if (floatingCanvas != null)
            floatingCanvas.enabled = false;
            
        // Guardamos la referencia a la cámara principal para no buscarla en cada fotograma
        _mainCamera = Camera.main; 
    }

    public void Show(bool show)
    {
        if (floatingCanvas != null)
            floatingCanvas.enabled = show;
    }

    // NUEVO: Efecto "Billboard" (Siempre mira a la cámara)
    private void LateUpdate()
    {
        // Solo calculamos la rotación si el texto está visible en pantalla
        if (floatingCanvas != null && floatingCanvas.enabled && _mainCamera != null)
        {
            // Hacemos que el canvas gire para mirar exactamente hacia donde está nuestra cámara
            floatingCanvas.transform.rotation = Quaternion.LookRotation(floatingCanvas.transform.position - _mainCamera.transform.position);
        }
    }
}