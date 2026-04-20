using Fusion;
using UnityEngine;

public class TestDamageButton : MonoBehaviour
{
    [SerializeField] private int damageAmount = 25;

    public void OnClickDealDamage()
    {
        if (!GameState.TryGetInstance(out GameState gameState))
        {
            Debug.LogWarning("GameState no encontrado.");
            return;
        }

        NetworkRunner runner = NetworkManager.Instance?.Runner;
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogWarning("Runner no disponible.");
            return;
        }

        PlayerRef me = runner.LocalPlayer;

        // Busca el primer jugador que NO seas tú
        PlayerRef target = PlayerRef.None;
        foreach (var entry in gameState.Players)
        {
            if (entry.Key != me)
            {
                target = entry.Key;
                break;
            }
        }

        if (target == PlayerRef.None)
        {
            Debug.LogWarning("No hay otro jugador en la sala todavia.");
            return;
        }

        Debug.Log("Enviando solicitud de daño a: " + target);
        gameState.RPC_RequestDamage(me, target, damageAmount);
    }
}