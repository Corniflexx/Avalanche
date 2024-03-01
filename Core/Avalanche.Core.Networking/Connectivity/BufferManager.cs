namespace Avalanche.Core.Networking.Connectivity;

public class BufferManager : ObjectManager<Memory<byte>>
{
    public BufferManager(int bufferSize = 1024) : base(() => new Memory<byte>(new byte[bufferSize]))
    {
    }

    public override void Return(Memory<byte> obj)
    {
        obj.Span.Clear();
        base.Return(obj);
    }
}
