namespace Avalanche.Core.Networking.Connectivity;

public abstract class NetworkServer(string address, int port , int bufferSize, int maxConnectionsAllowed,int backlog, bool delay, bool fragment, SeriLog logger, NetworkEvents.ClientConnection connected, NetworkEvents.ClientReceive received, NetworkEvents.ClientConnection disconnected)
{
    #region Propertise

    private Socket Socket { get; set; }
    private SeriLog Logger { get; set; } 
    private EndPoint EndPoint { get; set; }
    public BufferManager BufferManager { get; set; }
    public Semaphore AcceptanceSemaphore { get;  set; }
    private BruteForceProtection BruteForceProtection { get; set; }
    public CancellationTokenSource ShutdownToken { get; set; }
    public NetworkEvents.ClientConnection Connected { get; set; } 
    public NetworkEvents.ClientReceive Received { get; set; }
    public NetworkEvents.ClientConnection Disconnected { get; set; } 
    
    #endregion

    #region Initialize Method

    public Task<bool> Init()
    {
        try
        {
            // Initialize and configure server socket
            this.EndPoint = new IPEndPoint(address is "localhost" or "127.0.0.1" or "0.0.0.0" ? IPAddress.Any : Dns.GetHostEntryAsync(address).Result.AddressList.First(), port);
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.LingerState = new LingerOption(false, 0);
            this.Socket.NoDelay = !delay;
            this.Socket.DontFragment = fragment;
            this.ShutdownToken = new CancellationTokenSource();
            this.Connected = connected;
            this.Received = received;
            this.Disconnected = disconnected;

            // Initialize management mechanisms
            this.AcceptanceSemaphore = new Semaphore(maxConnectionsAllowed, maxConnectionsAllowed);
            this.BufferManager = new BufferManager(bufferSize);
            this.BruteForceProtection = new BruteForceProtection(30, 15);
            this.Logger = logger;

            // bind the server socket

            this.Socket.Bind(EndPoint);
            this.Socket.Listen(backlog);
            Task.Run(Accept);
            return Task.FromResult(true);
        }
        catch (Exception exception)
        {

            Logger.Network.Error($"NetworkServer - Init() {exception}");
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Accept Method

    private async Task Accept()
    {
        try
        {
            while (Socket.IsBound && !ShutdownToken.IsCancellationRequested)
            {
                if (!AcceptanceSemaphore.WaitOne(TimeSpan.FromSeconds(5)))
                    continue;


                var client = new NetworkActor(this, await Socket.AcceptAsync());
                if (!this.BruteForceProtection.Authenticate(client.ToString()))
                {
                    await client.Disconnect();
                    continue;
                }
                Connected?.Invoke(client).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            Logger.Network.Error($"NetworkServer - Accept() {exception}");
        }
    }

    #endregion

    #region Stop Method 

    public Task Stop()
    {
        try
        {
            ShutdownToken.Cancel();
            if (Socket.IsBound)
            {
                Socket.Shutdown(SocketShutdown.Both);
            }
            Socket.Close();
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Network.Error(e, "NetworkServer - Stop()");
            return Task.FromException(e);
        }
    }

    #endregion
}
