using System;
using Domain.Common;

namespace Domain.Events;

public record BookingCreatedEvent(
    int BookingId,
    int UserId,
    int SlotId,
    DateTime BookingDate) : IDomainEvent;
