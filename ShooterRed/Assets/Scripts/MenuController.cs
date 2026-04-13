using UnityEngine;
using TMPro; // Necesario para la UI

public class MenuController : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;

    public void OnClickCreateRoom()
    {
        if (ValidateInputs())
        {
            // 1. Guardamos el nombre en el NetworkManager
            NetworkManager.Instance.LocalPlayerName = playerNameInput.text;

            // 2. Le decimos al NetworkManager que cree la sala
            NetworkManager.Instance.StartHost(roomNameInput.text);
        }
    }

    public void OnClickJoinRoom()
    {
        if (ValidateInputs())
        {
            NetworkManager.Instance.LocalPlayerName = playerNameInput.text;
            NetworkManager.Instance.StartClient(roomNameInput.text);
        }
    }

    public void OnClickQuit()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    // Peque�a ayuda para evitar que los jugadores entren sin nombre o sin sala
    private bool ValidateInputs()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || string.IsNullOrEmpty(roomNameInput.text))
        {
            Debug.LogWarning("�Falta el nombre del jugador o de la sala!");
            return false;
        }
        return true;
    }
}