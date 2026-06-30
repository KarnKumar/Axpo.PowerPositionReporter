using Axpo.PowerPositionReporter.Domain.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Axpo.PowerPositionReporter.Application.Extensions
    {

    /// <summary>
    /// resilience service extensions for adding retry policies to the power trade service.
    /// </summary>
    public static class ResilienceServiceExtensions
        {
        public const string PowerTradeRetryPipeline = "power-trade-retry";

        private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(15);

        public static IServiceCollection AddPowerTradeResilience (
            this IServiceCollection services, IConfiguration configuration )
            {
            var maxRetries = configuration.GetValue<int>("PowerPositionReporter:MaxRetryAttempts");

            services.AddResiliencePipeline<string, IEnumerable<PowerTrade>> (
                PowerTradeRetryPipeline,
                ( pipelineBuilder, context ) =>
                {
                    var logger = context.ServiceProvider.GetRequiredService<IReportLogger>();

                    pipelineBuilder.AddRetry (new RetryStrategyOptions<IEnumerable<PowerTrade>>
                        {
                        MaxRetryAttempts = maxRetries,
                        Delay = RetryBaseDelay,
                        BackoffType = DelayBackoffType.Exponential,
                        ShouldHandle = new PredicateBuilder<IEnumerable<PowerTrade>> ()
                            .Handle<Exception> (ex => ex is not OperationCanceledException),
                        OnRetry = args =>
                        {
                            logger.Warning (
                                $"[POWER-SVC] Retry attempt={args.AttemptNumber + 1} │ delay={args.RetryDelay} │ reason={args.Outcome.Exception?.Message ?? "unknown"}");
                            return ValueTask.CompletedTask;
                        }
                        });
                });

            return services;
            }
        }
    }