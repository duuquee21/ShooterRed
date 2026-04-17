using Fusion;
using UnityEngine;

// Heredamos de NetworkBehaviour para que Fusion reconozca este script
public class SimpleSpawner : NetworkBehaviour
{
    [Header("Pon aqu� tu Prefab del Jugador")]
    public NetworkPrefabRef playerPrefab;

    // Spawned() es un m�todo m�gico de Fusion.
    // Se ejecuta autom�ticamente en cuanto el jugador termina de cargar esta escena y se conecta a la sala.
    public override void Spawned()
    {
        Debug.Log("�He entrado a la sala! Creando mi avatar...");

        // Elegimos una posici�n aleatoria (para que si entran dos a la vez, no aparezcan uno dentro del otro)
        Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 5f, Random.Range(-3f, 3f));
        if (Physics.Raycast(randomPosition, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
        {
            randomPosition.y = hit.point.y + 1f;
        }
        else
        {
            randomPosition.y = 1f;
        }

        // Runner.Spawn le dice a la red que cree el objeto. 
        // Le pasamos el prefab, la posici�n, la rotaci�n (ninguna) y de qui�n es (nuestro).
        Runner.Spawn(playerPrefab, randomPosition, Quaternion.identity, Runner.LocalPlayer);
    }


    private void Update()
    {
        // 1. Primero comprobamos que la red est� conectada y funcionando
        if (Runner != null && Runner.IsRunning)
        {
            // 2. Si pulsas ESPACIO y a�n NO has spawneado...
            if (Input.GetKeyDown(KeyCode.Space) )
            {
                

                

                
                Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
                Runner.Spawn(playerPrefab, randomPosition, Quaternion.identity, Runner.LocalPlayer);
            }
        }
    }
}