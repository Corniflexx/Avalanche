namespace Avalanche.Core.Services;

public abstract class MarshaledService
{
    /// <summary>
    /// The order at which this Marshaled will be loaded.
    /// </summary>
    public abstract int LoadOrder { get; }

    public abstract bool WorldSpecific { get; set; }

    /// <summary>
    /// Called ONCE when the Marshaled starts.
    /// </summary>
    public abstract Task Start();

    /// <summary>
    /// Called ONCE when the Marshaled stops.
    /// </summary>
    public abstract Task Stop();

    /// <summary>
    /// This can be used to process time based actions.
    /// </summary>
    public abstract Task Tick();

}
