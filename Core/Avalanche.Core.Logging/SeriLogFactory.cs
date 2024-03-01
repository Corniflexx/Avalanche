public static class SeriLogFactory
{
    public static ILogger CreateCustomSeriLog(string partitionName, LogEventLevel minimumLogLevel = LogEventLevel.Verbose)
    {
        if (string.IsNullOrWhiteSpace(partitionName))
        {
            throw new ArgumentException("Partition name must not be null or empty.", nameof(partitionName));
        }

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(minimumLogLevel))
            .Enrich.WithExceptionDetails()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Partition}] {Message:lj}{NewLine}{Exception}")
            .Enrich.WithProperty("Partition", partitionName);

        return loggerConfiguration.CreateLogger();
    }
}
