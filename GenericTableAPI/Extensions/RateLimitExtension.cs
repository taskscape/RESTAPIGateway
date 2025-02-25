using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GenericTableAPI.Extensions
{
    public static class RateLimitExtension
    {
        public static IServiceCollection AddRateLimit(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            RateLimitOptions rateLimitOptions = new();
            configurationSection.Bind(rateLimitOptions);

            if(string.IsNullOrEmpty(rateLimitOptions.Type))
                return services;

            rateLimitOptions.Validate();

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // Determine partition key based on configuration
                    string partitionKey;
                    if (rateLimitOptions.Mode == "PerUser")
                    {
                        partitionKey = context.User.Identity?.Name ?? context.Request.Headers["X-User-ID"].FirstOrDefault() ?? "anonymous";
                    }
                    else // Default: Per-IP
                    {
                        partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }

#pragma warning disable CS8629 // Typ wartości dopuszczający wartość null może być równy null.
                    return rateLimitOptions.Type.ToLower() switch
                    {
                        "fixedwindow" => RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = (int)rateLimitOptions.PermitLimit,
                                Window = TimeSpan.FromSeconds((int)rateLimitOptions.WindowSeconds)
                            }),

                        "slidingwindow" => RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
                            new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = (int)rateLimitOptions.PermitLimit,
                                Window = TimeSpan.FromSeconds((int)rateLimitOptions.WindowSeconds),
                                SegmentsPerWindow = (int)rateLimitOptions.SegmentsPerWindow
                            }),

                        "tokenbucket" => RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
                            new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = (int)rateLimitOptions.PermitLimit,
                                TokensPerPeriod = (int)rateLimitOptions.TokensPerPeriod,
                                ReplenishmentPeriod = TimeSpan.FromSeconds((int)rateLimitOptions.ReplenishmentPeriodSeconds),
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = (int)rateLimitOptions.QueueLimit
                            }),

                        "concurrency" => RateLimitPartition.GetConcurrencyLimiter(partitionKey, _ =>
                            new ConcurrencyLimiterOptions
                            {
                                PermitLimit = (int)rateLimitOptions.PermitLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = (int)rateLimitOptions.QueueLimit
                            }),

                        _ => throw new ArgumentException("Invalid rate limiting type specified in configuration.")
                    };
#pragma warning restore CS8629 // Typ wartości dopuszczający wartość null może być równy null.
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }


    public class RateLimitOptions
    {
        public string? Type { get; set; } = "";
        public string? Mode { get; set; } = "PerIP";
        public int? PermitLimit { get; set; }
        public int? WindowSeconds { get; set; }
        public int? SegmentsPerWindow { get; set; }
        public int? QueueLimit { get; set; }
        public int? TokensPerPeriod { get; set; }
        public int? ReplenishmentPeriodSeconds { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Type))
                return;

            if (Type.ToLower() == "fixedwindow")
            {
                if (PermitLimit == null)
                    throw new ArgumentNullException(nameof(PermitLimit));
                if (WindowSeconds == null)
                    throw new ArgumentNullException(nameof(WindowSeconds));
            }
            else if (Type.ToLower() == "slidingwindow")
            {
                if (PermitLimit == null)
                    throw new ArgumentNullException(nameof(PermitLimit));
                if (WindowSeconds == null)
                    throw new ArgumentNullException(nameof(WindowSeconds));
                if (SegmentsPerWindow == null)
                    throw new ArgumentNullException(nameof(SegmentsPerWindow));
            }
            else if (Type.ToLower() == "tokenbucket")
            {
                if (PermitLimit == null)
                    throw new ArgumentNullException(nameof(PermitLimit));
                if (TokensPerPeriod == null)
                    throw new ArgumentNullException(nameof(TokensPerPeriod));
                if (ReplenishmentPeriodSeconds == null)
                    throw new ArgumentNullException(nameof(ReplenishmentPeriodSeconds));
                if (QueueLimit == null)
                    throw new ArgumentNullException(nameof(QueueLimit));
            }
            else if (Type.ToLower() == "concurrency")
            {
                if (PermitLimit == null)
                    throw new ArgumentNullException(nameof(PermitLimit));
                if (QueueLimit == null)
                    throw new ArgumentNullException(nameof(QueueLimit));
            }
            else
                throw new ArgumentException("Invalid rate limiting type specified in configuration.");

        }
    }
}
