using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Razorpay.Api;

namespace Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly string _keyId;
        private readonly string _keySecret;

        public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _keyId = _configuration["Razorpay:KeyId"] ?? "rzp_test_dummy";
            _keySecret = _configuration["Razorpay:KeySecret"] ?? "dummy_secret";
        }

        public async Task<Result<CreateOrderResponseDto>> CreateOrderAsync(decimal amount, string receipt)
        {
            try
            {
                if (_keyId == "rzp_test_dummy")
                {
                    _logger.LogWarning("Using dummy Razorpay keys. Orders will be mocked.");
                    // Return a mocked order for testing when keys aren't configured
                    return Result<CreateOrderResponseDto>.Success(new CreateOrderResponseDto
                    {
                        OrderId = "order_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", ""),
                        Amount = amount,
                        Currency = "INR"
                    });
                }

                RazorpayClient client = new RazorpayClient(_keyId, _keySecret);
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", (int)(amount * 100)); // amount in the smallest currency unit
                options.Add("receipt", receipt);
                options.Add("currency", "INR");

                // Razorpay SDK is synchronous, so we wrap it in Task.Run if we want true async,
                // but for simple API calls it's fine to just run it, or wrap it.
                var order = await Task.Run(() => client.Order.Create(options));

                string orderId = order["id"].ToString();

                return Result<CreateOrderResponseDto>.Success(new CreateOrderResponseDto
                {
                    OrderId = orderId,
                    Amount = amount,
                    Currency = "INR"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Razorpay order");
                return Result<CreateOrderResponseDto>.Failure("Failed to initiate payment");
            }
        }

        public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
        {
            if (_keyId == "rzp_test_dummy")
            {
                _logger.LogWarning("Using dummy Razorpay keys. Bypassing signature verification.");
                return true; // Auto-verify for testing without keys
            }

            try
            {
                var attributes = new Dictionary<string, string>
                {
                    { "razorpay_order_id", orderId },
                    { "razorpay_payment_id", paymentId },
                    { "razorpay_signature", signature }
                };

                Utils.verifyPaymentSignature(attributes);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment signature verification failed");
                return false;
            }
        }
    }
}
