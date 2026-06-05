using Application.Common.Result;
using MediatR;
using Domain.Entities;
using Application.Interfaces;

namespace Application.Features.OwnerRequests.Commands;

public record CreateOwnerRequestCommand(int UserId, int TurfId, string BusinessName, string ContactNumber, string ProofDocumentUrl) : IRequest<Result<int>>;

public class CreateOwnerRequestCommandHandler : IRequestHandler<CreateOwnerRequestCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOwnerRequestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateOwnerRequestCommand request, CancellationToken cancellationToken)
    {
        // Check if there is already a pending request for this turf
        var allRequests = await _unitOfWork.OwnerRequests.GetAllAsync(cancellationToken);
        var existingRequest = allRequests.FirstOrDefault(r => r.TurfId == request.TurfId && r.Status != "Rejected");
            
        if (existingRequest != null)
        {
            return Result<int>.Failure("An owner request for this turf is already pending or approved.");
        }

        var newRequest = new OwnerRequest
        {
            UserId = request.UserId,
            TurfId = request.TurfId,
            BusinessName = request.BusinessName,
            ContactNumber = request.ContactNumber,
            ProofDocumentUrl = request.ProofDocumentUrl,
            Status = "Pending"
        };

        await _unitOfWork.OwnerRequests.AddAsync(newRequest);
        
        // Audit log
        var auditLog = new AuditLog
        {
            UserId = request.UserId,
            Action = "OwnerRequestCreated",
            Details = $"User {request.UserId} requested ownership of Turf {request.TurfId}"
        };
        await _unitOfWork.AuditLogs.AddAsync(auditLog);

        await _unitOfWork.SaveChangesAsync();

        return Result<int>.Success(newRequest.Id);
    }
}
