namespace Avalanche.Core.Networking.Connectivity;

public class NetworkEvents
{
    public delegate Task ClientConnection(NetworkActor networkActor);
    public delegate Task ClientReceive(NetworkActor networkActor, PacketBase packet);

    protected virtual bool Exchanged(ReadOnlySpan<byte> buffer)
    {
        throw new NotImplementedException();
    }
}
