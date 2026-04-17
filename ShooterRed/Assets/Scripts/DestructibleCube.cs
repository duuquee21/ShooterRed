using Fusion;
using UnityEngine;

public class DestructibleCube : NetworkBehaviour
{
    [Networked] public int Hits { get; set; } = 0;
    public int maxHits = 3;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void TakeHitRpc()
    {
        if (!HasStateAuthority) return;

        Hits++;
        Debug.Log($"Cube hit! Hits: {Hits}");

        if (Hits >= maxHits)
        {
            Runner.Despawn(Object);
            Debug.Log("Cube destroyed!");
        }
    }
}