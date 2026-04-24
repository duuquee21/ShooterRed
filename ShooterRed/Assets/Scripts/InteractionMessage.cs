using UnityEngine;

// Muestra mensajes de interacción en pantalla (singleton)
public class InteractionMessage : MonoBehaviour
{
    public static InteractionMessage Instance { get; private set; }
    private string _message = "";
    private float _showUntil = 0f;

    [Header("Estilo")]
    public int fontSize = 28;
    public Color textColor = Color.yellow;
    public float messageDuration = 2f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string msg, float duration = -1f)
    {
        _message = msg;
        _showUntil = Time.time + (duration > 0 ? duration : messageDuration);
    }

    private void OnGUI()
    {
        if (Time.time > _showUntil || string.IsNullOrEmpty(_message)) return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.alignment = TextAnchor.LowerCenter;
        float w = Screen.width;
        float h = Screen.height;
        GUI.Label(new Rect(0, h - 120, w, 60), _message, style);
    }
}
