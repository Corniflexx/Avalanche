namespace Avalanche.Core.Services;

public class RandomnessService : BackgroundService
{
    #region Fields and Properties

    private Channel<Double> BufferChannel;
    protected Random Generator;

    #endregion

    #region Constructors

    public RandomnessService(int capacity = 10000)
    {
        this.BufferChannel = Channel.CreateBounded<Double>(capacity);
        this.Generator = new Random();
    }

    #endregion

    #region Services

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.BufferChannel.Writer.WriteAsync(
                this.Generator.NextDouble(),
                stoppingToken);
        }
    }

    public async Task<int> NextAsync(int minValue, int maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException();

        var range = (long)maxValue - minValue;
        if (range > (long)Int32.MaxValue)
            throw new ArgumentOutOfRangeException();

        var value = await this.BufferChannel.Reader.ReadAsync();
        var result = ((int)(value * range) + minValue);
        return result;
    }
    public async Task NextBytesAsync(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(await this.NextAsync(0, 255));
    }

    #endregion
}
