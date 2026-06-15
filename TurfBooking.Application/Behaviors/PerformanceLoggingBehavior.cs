using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors
{
    public class PerformanceLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<PerformanceLoggingBehavior<TRequest, TResponse>> _logger;

        public PerformanceLoggingBehavior(ILogger<PerformanceLoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await next();

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                var requestName = typeof(TRequest).Name;
                var requestData = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
                
                // Exclude passwords or sensitive data simply by not logging it if the type implies it, but here we just serialize.
                if (requestName.Contains("Login") || requestName.Contains("Password"))
                {
                    requestData = "[REDACTED]";
                }

                _logger.LogWarning("Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@RequestData}",
                    requestName, stopwatch.ElapsedMilliseconds, requestData);
            }

            return response;
        }
    }
}
