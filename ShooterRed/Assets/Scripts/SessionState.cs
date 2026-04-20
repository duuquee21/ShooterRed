using Fusion;

public class RoomSessionState : NetworkBehaviour
{
    [Networked] public int ConnectedPlayers { get; set; }
    [Networked] public int MaxPlayers { get; set; }
    [Networked] public NetworkBool IsOpen { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            MaxPlayers = 8;
            IsOpen = true;
            ConnectedPlayers = 0;
        }
    }
}