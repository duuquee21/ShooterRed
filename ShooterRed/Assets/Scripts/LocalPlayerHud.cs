using Fusion;
using UnityEngine;

public class LocalPlayerHud : MonoBehaviour
{
    [Header("Estilo")]
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int marginLeft = 20;
    [SerializeField] private int marginBottom = 20;

    private GUIStyle _style;
    private PlayerCombatData _data;
    private bool _hasData;
    private int  _currentWeaponId;
    private int  _currentWeaponRarity;

    private void Update()
    {
        _hasData = false;

        if (NetworkManager.Instance == null || NetworkManager.Instance.Runner == null)
            return;

        if (!NetworkManager.Instance.Runner.IsRunning)
            return;

        if (!GameState.TryGetInstance(out GameState gameState) || gameState == null)
            return;

        PlayerRef me = NetworkManager.Instance.Runner.LocalPlayer;

        if (!gameState.TryGetPlayerData(me, out PlayerCombatData data))
            return;

        _data = data;
        _hasData = true;

        // Leer arma equipada desde PlayerState
        NetworkObject myObj = NetworkManager.Instance.Runner.GetPlayerObject(me);
        if (myObj != null)
        {
            PlayerState ps = myObj.GetComponent<PlayerState>();
            if (ps != null)
            {
                _currentWeaponId     = ps.CurrentWeaponId;
                _currentWeaponRarity = ps.CurrentWeaponRarity;
            }
        }
    }

    private void OnGUI()
    {
        if (!_hasData)
            return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = fontSize;
            _style.fontStyle = FontStyle.Bold;
        }

        _style.normal.textColor = textColor;

        int lineHeight = fontSize + 6;
        int x = marginLeft;
        int y = Screen.height - marginBottom - lineHeight * 4;

        GUI.Label(new Rect(x, y, 300, lineHeight), "HP:    " + _data.Health, _style);
        GUI.Label(new Rect(x, y + lineHeight, 300, lineHeight), "Score: " + _data.Score, _style);
        GUI.Label(new Rect(x, y + lineHeight * 2, 300, lineHeight), "Racha: " + _data.Streak, _style);

        // Arma equipada con color según rareza
        var def    = WeaponDatabase.Get(_currentWeaponId);
        var rarity = (WeaponRarity)_currentWeaponRarity;
        GUIStyle weaponStyle = new GUIStyle(_style);
        weaponStyle.normal.textColor = rarity.RarityColor();
        GUI.Label(new Rect(x, y + lineHeight * 3, 350, lineHeight),
            $"Arma: {def.DisplayName}  [{rarity.Label()}]", weaponStyle);

        // Recompensas de racha disponibles
        GUIStyle rewardStyle = new GUIStyle(GUI.skin.label);
        rewardStyle.fontSize  = fontSize - 2;
        rewardStyle.fontStyle = FontStyle.Bold;

        int ry = y - lineHeight * 2;

        if (_data.HasGrenade)
        {
            rewardStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x, ry, 300, lineHeight), "[G] Granada disponible", rewardStyle);
            ry -= lineHeight;
        }
        if (_data.HasAirstrike)
        {
            rewardStyle.normal.textColor = new Color(1f, 0.5f, 0f);
            GUI.Label(new Rect(x, ry, 300, lineHeight), "[F] Ataque aereo disponible", rewardStyle);
            ry -= lineHeight;
        }
        if (_data.HasTurret)
        {
            rewardStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(x, ry, 300, lineHeight), "[T] Torreta disponible", rewardStyle);
        }
    }
}