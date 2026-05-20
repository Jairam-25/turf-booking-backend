using Application.Common.Result;
using FluentValidation;

namespace TurfBooking.API.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionMiddleware> _logger = logger;
        private readonly IWebHostEnvironment _env = env;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex, _env.IsDevelopment());
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, bool isDevelopment)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;

            List<string>? errors = null;
            var message = "An unexpected error occurred. Please try again later.";

            if (exception is ValidationException validationException)
            {
                message = "Validation failed.";
                errors = validationException.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
            }
            else if (exception is KeyNotFoundException)
            {
                message = "The requested resource was not found.";
            }
            else if (exception is UnauthorizedAccessException)
            {
                message = "You are not authorized to perform this action.";
            }
            else
            {
                if (isDevelopment)
                {
                    message = exception.Message;
                    errors = new List<string> { exception.StackTrace ?? string.Empty };
                }
            }

            var response = ApiResponse<object>.FailureResponse(message, errors, statusCode);

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}

