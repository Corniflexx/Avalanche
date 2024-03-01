namespace Avalanche.Core.Networking.Security;

public sealed class BruteForceProtection
{
    #region Fields and Propertise

    public bool Active { get; set; }
    private readonly ConcurrentDictionary<string, long> _blockedConnections;
    private ConcurrentDictionary<string, int> _recentConnections;
    private readonly uint _maximumAttempts;
    private readonly uint _timeOut;

    #endregion

    #region Constructor

    public BruteForceProtection(uint maxConnectionsPerMinute, uint timeOut)
    {
        this.Active = true;
        this._maximumAttempts = maxConnectionsPerMinute;
        this._timeOut = timeOut;
        this._recentConnections = new ConcurrentDictionary<string, int>();
        this._blockedConnections = new ConcurrentDictionary<string, long>();

        Task.Run(async () =>
        {
            while (this.Active)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                this._recentConnections = new ConcurrentDictionary<string, int>();
                foreach (var key in this._blockedConnections.Keys)
                {
                    if (this._blockedConnections[key] < Environment.TickCount)
                        this._blockedConnections.TryRemove(key);
                }
            }
        });
    }

    #endregion

    #region Authenticate Method

    public bool Authenticate(string ip)
    {
        if (this._blockedConnections.TryGetValue(ip, out var connection))
            return false;

        if (this._recentConnections.TryGetValue(ip, out _))
        {
            if (++this._recentConnections[ip] <= this._maximumAttempts) return true;
            this._blockedConnections.TryAdd(ip, Environment.TickCount + this._timeOut * 60000);
            this._recentConnections.TryRemove(ip);
            return false;
        }
        else
            this._recentConnections.TryAdd(ip, 1);
        return true;
    }
    public void ClearBlocked() => this._blockedConnections.Clear();

    #endregion
}
