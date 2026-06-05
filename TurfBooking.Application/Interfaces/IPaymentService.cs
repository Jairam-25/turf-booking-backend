using Application.DTOs;
using Application.Common.Result;

namespace Application.Interfaces
{
    public interface IPaymentService
    {
        Task<Result<CreateOrderResponseDto>> CreateOrderAsync(decimal amount, string receipt);
        bool VerifyPaymentSignature(string orderId, string paymentId, string signature);
    }
}
