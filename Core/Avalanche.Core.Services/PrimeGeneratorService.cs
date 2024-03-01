namespace Avalanche.Core.Services;

public class PrimeGeneratorService : BackgroundService
{
    #region Fields and Properties

    private int BitLength;
    private Channel<BigInteger> BufferChannel;
    protected Random Generator;

    #endregion

    #region Constructors

    public PrimeGeneratorService(int capacity = 100, int bitLength = 256)
    {
        this.BitLength = bitLength;
        this.BufferChannel = Channel.CreateBounded<BigInteger>(capacity);
        this.Generator = new Random();
    }

    #endregion

    #region Services

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.BufferChannel.Writer.WriteAsync(
                BigInteger.ProbablePrime(this.BitLength, this.Generator),
                stoppingToken);
        }
    }

    public async Task<BigInteger> NextAsync()
    {

        return await this.BufferChannel.Reader.ReadAsync();
    }

    #endregion
}
