using Unity.Netcode.Components;

/// <summary>
/// Owner-authoritative NetworkTransform: each player simulates their own flight
/// locally (Movement.cs is fully client-side) and streams the result to everyone
/// else. Remote avatars are visual proxies only.
/// </summary>
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;

    protected override void Awake()
    {
        base.Awake();
        SyncScaleX = SyncScaleY = SyncScaleZ = false;
        Interpolate = true;
    }
}
