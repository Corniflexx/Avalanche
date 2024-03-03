namespace Avalanche.Core.Networking.Connectivity;

public class NetworkActor(NetworkServer networkServer, Socket socket) : NetworkEvents
{
    #region Fields and Constants

    private const int ReceiveTimeoutSeconds = 30;

    private SeriLog Logger;
    private Memory<byte> Buffer;
    private Socket Socket = socket;
    public INetDragonCipher Cipher;
    private readonly object SendLock;
    private readonly int FooterLength;
    public readonly string IPAddress;
    public readonly byte[] PacketFooter;
    private NetworkServer Server = networkServer;
    private CancellationTokenSource ShutdownToken;

    #endregion

    #region Receive Operations

    private async Task ExchangingAsync()
    {
        var timeout = new CancellationTokenSource();
        int consumed = 0, examined = 0, remaining = 0;

        if (Socket.Connected && !this.ShutdownToken.IsCancellationRequested)
        {
            try
            {
                // Receive data from the client socket
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    timeout.Token, this.ShutdownToken.Token);
                var receiveOperation = Socket.ReceiveAsync(Buffer[..], SocketFlags.None, cancellation.Token);

                timeout.CancelAfter(TimeSpan.FromSeconds(ReceiveTimeoutSeconds));
                examined = await receiveOperation;
                if (examined == 0)
                {
                    await Disconnect();
                    return;
                }
                if (examined < 9) throw new Exception("Invalid length");
            }
            catch (Exception e)
            {
                if (e is SocketException)
                {
                    SocketException socketEx = e as SocketException;
                    if (socketEx.SocketErrorCode < SocketError.ConnectionAborted ||
                        socketEx.SocketErrorCode > SocketError.Shutdown)
                        Console.WriteLine(e);
                }

                await Disconnect();
                return;
            }
             Cipher.Decrypt(Buffer[..9].Span,Buffer[..9].Span);
            consumed = BitConverter.ToUInt16(Buffer.Span.Slice(7, 2)) + 7;
            if (consumed > examined)
            {
                await Disconnect();
                return;
            }
            Cipher.Decrypt(Buffer[9..(consumed)].Span,Buffer[9..(consumed)].Span);

            // Process the exchange now that bytes are decrypted
            if (!this.Exchanged(Buffer[..consumed].Span))
            {
                await Disconnect();
                return;
            }
            if (consumed < examined)
            {
                Cipher.Decrypt(Buffer[consumed..examined].Span,Buffer[consumed..examined].Span);

                if (!this.SplitProcess(examined + remaining, ref consumed))
                {
                    await Disconnect();
                    return;
                }

                remaining = examined - consumed;
                Buffer[consumed..examined].CopyTo(Buffer);
            }
        }
        await this.ReceivingAsync(remaining);
    }
    private Task ReceivingAsync(int remaining)
    {
        return this.ReceivingAsync(0);
    }
    public async Task ReceiveAsync(int remaining)
    {
        try
        {
            var timeout = new CancellationTokenSource();
            int examined = 0;

            while ((Socket.Connected && !ShutdownToken.IsCancellationRequested))
            {
                try
                {
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                       timeout.Token, this.ShutdownToken.Token);

                    var receiveOperation = Socket.ReceiveAsync(Buffer[remaining..], SocketFlags.None,
                          cancellation.Token);

                    timeout.CancelAfter(TimeSpan.FromSeconds(ReceiveTimeoutSeconds));
                    examined = await receiveOperation;
                    if (examined == 0)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException exception)
                {
                   Logger.Network.Error($"NetworkActor - ReceiveAsync() {exception}");
                    await Disconnect();
                    break;
                }
                catch(SocketException socketException)
                {
                    if (socketException.SocketErrorCode < SocketError.ConnectionAborted ||
                        socketException.SocketErrorCode > SocketError.Shutdown)
                    {
                        Logger.Network.Error($"NetworkActor - ReceiveAsync() {socketException}");
                    }
                }
            }
            await Disconnect();
        }
        catch (Exception exception)
        {

            Logger.Network.Error($"NetworkActor - ReceiveAsync() {exception}");
            await Disconnect();
        }
    }

    #endregion

    #region Send and Splitting Data

    public virtual Task SendAsync(byte[] packet)
    {
        var encrypted = new byte[packet.Length + this.PacketFooter.Length];
        packet.CopyTo(encrypted, 0);

        BitConverter.TryWriteBytes(encrypted, (ushort)packet.Length);
        Array.Copy(this.PacketFooter, 0, encrypted, packet.Length, this.PacketFooter.Length);

        lock (SendLock)
        {
            try
            {
                this.Cipher.Encrypt(encrypted, encrypted);
                this.Socket?.Send(encrypted, SocketFlags.None);
                return Task.CompletedTask;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode < SocketError.ConnectionAborted ||e.SocketErrorCode > SocketError.Shutdown)
                {
                    Logger.Network.Error("NetworkActor - SendAsync()");
                }
                return Task.FromException(e);
            }
        }
    }
    private bool SplitProcess(int examined, ref int consumed)
    {
        try
        {
            var buffer = Buffer.Span;

            while (consumed + 2 < examined)
            {
                var length = BitConverter.ToUInt16(buffer.Slice(consumed, 2));

                if (length == 0)
                    return false;

                var expected = consumed + length + FooterLength;

                if (length > buffer.Length)
                    return false;

                if (expected > examined)
                    break;

                var data = buffer.Slice(consumed, length + FooterLength).ToArray();
                var packet = new PacketBase(data);
                Server.Received?.Invoke(this, packet);
                consumed += length + FooterLength;
            }

            return true;
        }
        catch (Exception exception)
        {
            Logger.Network.Error($"NetworkActor - SplitProcess() {exception}");
            Disconnect();
            return false;
        }
    }

    #endregion

    #region Disconnecting

    public Task Disconnect()
    {
        try
        {
            ShutdownToken.Cancel();
            if (Socket.IsBound)
            {
                Socket?.Shutdown(SocketShutdown.Both);
            }
            Socket?.Disconnect(false);
            Socket?.Close();
            Server?.AcceptanceSemaphore.Release();
            Server?.BufferManager.Return(Buffer);
            Server?.Disconnected?.Invoke(this).ConfigureAwait(false);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
             Logger.Network.Error($"NetworkActor - Disconnect() {exception}");
        }
        return Task.CompletedTask;
    }

    #endregion
}
