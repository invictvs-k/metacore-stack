using Polly;
using Polly.Retry;

namespace RoomOperator.Core;

public sealed class RetryPolicyConfig
{
  public int MaxAttempts { get; set; } = 3;
  public int InitialDelayMs { get; set; } = 100;
  public int MaxDelayMs { get; set; } = 5000;
  public double JitterFactor { get; set; } = 0.2;
}

public sealed class RetryPolicyFactory
{
  private readonly RetryPolicyConfig _config;
  private readonly ILogger<RetryPolicyFactory> _logger;
  private readonly Random _random = new();

  public RetryPolicyFactory(RetryPolicyConfig config, ILogger<RetryPolicyFactory> logger)
  {
    _config = config;
    _logger = logger;
  }

  public ResiliencePipeline<T> CreatePolicy<T>()
  {
    return new ResiliencePipelineBuilder<T>()
        .AddRetry(new RetryStrategyOptions<T>
        {
          MaxRetryAttempts = _config.MaxAttempts,
          Delay = TimeSpan.FromMilliseconds(_config.InitialDelayMs),
          BackoffType = DelayBackoffType.Exponential,
          UseJitter = true,
          OnRetry = args =>
              {
                _logger.LogWarning(
                        "Retry attempt {Attempt} after {Delay}ms due to: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                return ValueTask.CompletedTask;
              }
        })
        .Build();
  }

  public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
  {
    var pipeline = CreatePolicy<T>();
    return await pipeline.ExecuteAsync(async token => await action(), ct);
  }

  public async Task ExecuteAsync(Func<Task> action, CancellationToken ct = default)
  {
    var pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
          MaxRetryAttempts = _config.MaxAttempts,
          Delay = TimeSpan.FromMilliseconds(_config.InitialDelayMs),
          BackoffType = DelayBackoffType.Exponential,
          UseJitter = true,
          OnRetry = args =>
              {
                _logger.LogWarning(
                        "Retry attempt {Attempt} after {Delay}ms due to: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                return ValueTask.CompletedTask;
              }
        })
        .Build();

    await pipeline.ExecuteAsync(async token => await action(), ct);
  }
}
