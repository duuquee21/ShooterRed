using System.Collections.Generic;
using UnityEngine;

public class KillFeedHud : MonoBehaviour
{
    public static KillFeedHud Instance { get; private set; }

    [Header("Estilo")]
    [SerializeField] private int fontSize = 16;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float entryLifetime = 4f;
    [SerializeField] private int maxEntries = 5;

    private struct FeedEntry
    {
        public string Text;
        public float ExpireTime;
    }

    private readonly List<FeedEntry> _entries = new List<FeedEntry>();
    private GUIStyle _style;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddEntry(string attacker, string victim)
    {
        if (_entries.Count >= maxEntries)
            _entries.RemoveAt(0);

        _entries.Add(new FeedEntry
        {
            Text = attacker + " eliminó a " + victim,
            ExpireTime = Time.time + entryLifetime
        });
    }

    private void Update()
    {
        _entries.RemoveAll(e => Time.time >= e.ExpireTime);
    }

    private void OnGUI()
    {
        if (_entries.Count == 0)
            return;

        EnsureStyle();

        float rowH = fontSize + 8;
        float x = Screen.width - 330f;
        float y = 20f;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            float remaining = _entries[i].ExpireTime - Time.time;
            float alpha = Mathf.Clamp01(remaining);

            // Fondo
            GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
            GUI.DrawTexture(new Rect(x - 8, y, 320, rowH), Texture2D.whiteTexture);

            // Texto
            Color c = textColor;
            c.a = alpha;
            _style.normal.textColor = c;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, 310, rowH), _entries[i].Text, _style);

            y += rowH + 3f;
        }

        GUI.color = Color.white;
    }

    private void EnsureStyle()
    {
        if (_style != null)
            return;

        _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
        _style.normal.textColor = textColor;
    }
}
