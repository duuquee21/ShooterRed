using Fusion;
using UnityEngine;

// Heredamos de NetworkBehaviour para que Fusion reconozca este script
public class SimpleSpawner : NetworkBehaviour
{
    [Header("Pon aquí tu Prefab del Jugador")]
    public NetworkPrefabRef playerPrefab;

    // Spawned() es un método mágico de Fusion.
    // Se ejecuta automáticamente en cuanto el jugador termina de cargar esta escena y se conecta a la sala.
    public override void Spawned()
    {
        Debug.Log("¡He entrado a la sala! Creando mi avatar...");

        // Elegimos una posición aleatoria (para que si entran dos a la vez, no aparezcan uno dentro del otro)
        Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));

        // Runner.Spawn le dice a la red que cree el objeto. 
        // Le pasamos el prefab, la posición, la rotación (ninguna) y de quién es (nuestro).
        Runner.Spawn(playerPrefab, randomPosition, Quaternion.identity, Runner.LocalPlayer);
    }


    private void Update()
    {
        // 1. Primero comprobamos que la red está conectada y funcionando
        if (Runner != null && Runner.IsRunning)
        {
            // 2. Si pulsas ESPACIO y aún NO has spawneado...
            if (Input.GetKeyDown(KeyCode.Space) )
            {
                

                

                
                Vector3 randomPosition = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
                Runner.Spawn(playerPrefab, randomPosition, Quaternion.identity, Runner.LocalPlayer);
            }
        }
    }
}