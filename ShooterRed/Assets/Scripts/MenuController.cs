
using UnityEngine;
using TMPro;


public class MenuController : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;

    [Header("Paneles")]
    public GameObject menuPanel; // El panel principal del menú — asígnalo en el Inspector

    // Botón: Crear o Unirse a una sala escribiendo el nombre
    public void OnClickCreateOrJoinRoom()
    {
        if (ValidatePlayerName() && ValidateRoomName())
        {
            NetworkManager.Instance.LocalPlayerName = playerNameInput.text;
            Debug.Log($"[MenuController] Nombre asignado: '{playerNameInput.text}'");
            NetworkManager.Instance.CreateOrJoinRoom(roomNameInput.text);
        }
    }

    // Botón: Quick Join (Entrar a la primera sala que pille)
    public void OnClickQuickJoin()
    {
        if (ValidatePlayerName())
        {
            NetworkManager.Instance.LocalPlayerName = playerNameInput.text;
            NetworkManager.Instance.QuickJoinRoom();
        }
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    private bool ValidatePlayerName()
    {
        if (string.IsNullOrEmpty(playerNameInput.text))
        {
            Debug.LogWarning("¡Falta el nombre del jugador!");
            return false;
        }
        return true;
    }

    public void OnClickJoinRoomPanel()
    {
        if (ValidatePlayerName())
        {
            NetworkManager.Instance.LocalPlayerName = playerNameInput.text;
            // Ocultamos el menú principal para que no tape el panel de salas
            if (menuPanel != null) menuPanel.SetActive(false);
            NetworkManager.Instance.JoinRoom();
        }
    }

    private bool ValidateRoomName()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            Debug.LogWarning("¡Falta el nombre de la sala!");
            return false;
        }
        return true;
    }
}