using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.OwnerRequests.Commands;

public record ApproveOwnerRequestCommand(int RequestId, int SuperAdminId) : IRequest<Result<bool>>;

public class ApproveOwnerRequestCommandHandler : IRequestHandler<ApproveOwnerRequestCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public ApproveOwnerRequestCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(ApproveOwnerRequestCommand request, CancellationToken cancellationToken)
    {
        var ownerRequest = await _unitOfWork.OwnerRequests.GetByIdAsync(request.RequestId, cancellationToken);
        if (ownerRequest == null) return Result<bool>.Failure("Request not found");

        if (ownerRequest.Status != "Pending") return Result<bool>.Failure("Request is not pending");

        var user = await _userRepository.GetByIdAsync(ownerRequest.UserId);
        if (user == null) return Result<bool>.Failure("User not found");

        var turf = await _unitOfWork.Turfs.GetByIdAsync(ownerRequest.TurfId);
        if (turf == null) return Result<bool>.Failure("Turf not found");

        // 1. Update User Role
        user.Role = "Owner";

        // 2. Assign Ownership
        turf.OwnerId = user.Id;

        // 3. Update Status
        ownerRequest.Status = "Approved";
        ownerRequest.ApprovedAt = DateTime.UtcNow;

        // 4. Audit Log
        var auditLog = new AuditLog
        {
            UserId = request.SuperAdminId,
            Action = "OwnerRequestApproved",
            Details = $"SuperAdmin {request.SuperAdminId} approved Request {request.RequestId} for User {user.Id} on Turf {turf.Id}"
        };
        await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
