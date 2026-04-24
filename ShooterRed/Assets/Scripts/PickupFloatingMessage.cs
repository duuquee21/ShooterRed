using UnityEngine;
using TMPro;

// Mensaje flotante sobre el pickup
public class PickupFloatingMessage : MonoBehaviour
{
    public Canvas floatingCanvas; // Canvas en World Space, hijo del pickup
    public TextMeshProUGUI messageText;

    private void Awake()
    {
        if (floatingCanvas != null)
            floatingCanvas.enabled = false;
    }

    public void Show(bool show)
    {
        if (floatingCanvas != null)
            floatingCanvas.enabled = show;
    }
}
