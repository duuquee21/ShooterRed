using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomListPanel : MonoBehaviour
{
    public static RoomListPanel Instance { get; set; }

    [Header("UI References")]
    public GameObject roomButtonPrefab;
    public Transform contentParent;
    public GameObject panel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(List<SessionInfo> rooms)
    {
        ClearList();
        panel.SetActive(true);

        foreach (var room in rooms)
        {
            GameObject btnObj = Instantiate(roomButtonPrefab, contentParent);
            var btn = btnObj.GetComponent<Button>();
            var txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null)
                txt.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";

            string roomName = room.Name;
            if (btn != null)
                btn.onClick.AddListener(() => OnRoomSelected(roomName));
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
        ClearList();
        // Restauramos el menú principal si existe
        MenuController mc = FindFirstObjectByType<MenuController>();
        if (mc != null && mc.menuPanel != null)
            mc.menuPanel.SetActive(true);
    }

    private void ClearList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }

    private void OnRoomSelected(string roomName)
    {
        Hide();
        NetworkManager.Instance.CreateOrJoinRoom(roomName);
    }
}