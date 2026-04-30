using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // <-- AÑADIDO PARA USAR TEXTMESHPRO

public class RoomListPanel : MonoBehaviour
{
    public static RoomListPanel Instance { get; private set; }

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
            {
                // ¡AQUÍ ESTÁ LA MAGIA!
                // Juntamos el nombre de la sala con los jugadores actuales y los máximos
                txt.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
            }

            btn.onClick.AddListener(() => OnRoomSelected(room.Name));
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
        ClearList();
    }

    private void ClearList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnRoomSelected(string roomName)
    {
        Hide();
        NetworkManager.Instance.CreateOrJoinRoom(roomName);
    }
}