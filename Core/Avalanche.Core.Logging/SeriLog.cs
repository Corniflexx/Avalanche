namespace Avalanche.Core.Logging;

public class SeriLog
{
    public ILogger Network { get; set; }
    public ILogger Transmission { get; set; }

    public SeriLog()
    {
        this.Network = SeriLogFactory.CreateCustomSeriLog("Network");
        this.Transmission = SeriLogFactory.CreateCustomSeriLog("Transmission");
    }
}
