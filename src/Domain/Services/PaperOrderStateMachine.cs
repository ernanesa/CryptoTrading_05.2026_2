using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Services;

public static class PaperOrderStateMachine
{
    private const decimal QuantityTolerance = 0.00000001m;

    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> ValidTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.New] =
            [
                OrderStatus.Open,
                OrderStatus.Rejected,
                OrderStatus.Cancelled,
                OrderStatus.Expired
            ],
            [OrderStatus.Open] =
            [
                OrderStatus.PartiallyFilled,
                OrderStatus.Filled,
                OrderStatus.Cancelled,
                OrderStatus.Expired
            ],
            [OrderStatus.PartiallyFilled] =
            [
                OrderStatus.PartiallyFilled,
                OrderStatus.Filled,
                OrderStatus.Cancelled,
                OrderStatus.Expired
            ],
            [OrderStatus.Filled] = [],
            [OrderStatus.Rejected] = [],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.Expired] = []
        };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Filled or OrderStatus.Rejected or OrderStatus.Cancelled or OrderStatus.Expired;

    public static PaperOrderEvent Created(PaperOrder order, DateTime occurredAt, string reason = "Paper order created") =>
        new()
        {
            PaperOrderId = order.Id,
            ClientOrderId = order.ClientOrderId,
            Symbol = order.Symbol,
            FromStatus = null,
            ToStatus = OrderStatus.New,
            EventType = "Created",
            Reason = reason,
            CreatedAt = occurredAt
        };

    public static PaperOrderEvent Activate(PaperOrder order, DateTime occurredAt, string reason = "Order accepted by paper matching engine") =>
        Transition(order, OrderStatus.Open, "Accepted", reason, occurredAt);

    public static PaperOrderEvent Reject(PaperOrder order, DateTime occurredAt, string reason) =>
        Transition(order, OrderStatus.Rejected, "Rejected", reason, occurredAt);

    public static PaperOrderEvent Cancel(PaperOrder order, DateTime occurredAt, string reason) =>
        Transition(order, OrderStatus.Cancelled, "Cancelled", reason, occurredAt);

    public static PaperOrderEvent Expire(PaperOrder order, DateTime occurredAt, string reason) =>
        Transition(order, OrderStatus.Expired, "Expired", reason, occurredAt);

    public static PaperOrderEvent ApplyFill(PaperOrder order, decimal fillQuantity, decimal fillPrice, decimal fee, DateTime occurredAt, string reason = "Paper order fill")
    {
        if (fillQuantity <= 0)
            throw new InvalidOperationException("Fill quantity must be positive.");
        if (fillPrice <= 0)
            throw new InvalidOperationException("Fill price must be positive.");
        if (fee < 0)
            throw new InvalidOperationException("Fill fee cannot be negative.");
        if (IsTerminal(order.Status))
            throw new InvalidOperationException($"Cannot fill terminal paper order {order.ClientOrderId} with status {order.Status}.");
        if (fillQuantity - order.RemainingQuantity > QuantityTolerance)
            throw new InvalidOperationException($"Fill quantity exceeds remaining quantity for paper order {order.ClientOrderId}.");

        var fromStatus = order.Status;
        var previousFilled = order.FilledQuantity;
        var newFilledQuantity = previousFilled + fillQuantity;
        var newStatus = order.Quantity - newFilledQuantity <= QuantityTolerance
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;

        EnsureCanTransition(order, newStatus);

        order.FilledQuantity = newStatus == OrderStatus.Filled ? order.Quantity : newFilledQuantity;
        order.AverageFillPrice = ((order.AverageFillPrice * previousFilled) + (fillPrice * fillQuantity)) / newFilledQuantity;
        order.FeePaid += fee;
        order.Status = newStatus;
        order.UpdatedAt = occurredAt;

        return CreateEvent(order, fromStatus, newStatus, newStatus == OrderStatus.Filled ? "Filled" : "PartiallyFilled", reason, occurredAt, fillQuantity, fillPrice, fee);
    }

    private static PaperOrderEvent Transition(PaperOrder order, OrderStatus nextStatus, string eventType, string reason, DateTime occurredAt)
    {
        var fromStatus = order.Status;
        EnsureCanTransition(order, nextStatus);

        order.Status = nextStatus;
        order.UpdatedAt = occurredAt;

        return CreateEvent(order, fromStatus, nextStatus, eventType, reason, occurredAt);
    }

    private static void EnsureCanTransition(PaperOrder order, OrderStatus nextStatus)
    {
        if (!CanTransition(order.Status, nextStatus))
            throw new InvalidOperationException($"Invalid paper order transition for {order.ClientOrderId}: {order.Status} -> {nextStatus}.");
    }

    private static PaperOrderEvent CreateEvent(
        PaperOrder order,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        string eventType,
        string reason,
        DateTime occurredAt,
        decimal? fillQuantity = null,
        decimal? fillPrice = null,
        decimal? fee = null) =>
        new()
        {
            PaperOrderId = order.Id,
            ClientOrderId = order.ClientOrderId,
            Symbol = order.Symbol,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            EventType = eventType,
            Reason = reason,
            FillQuantity = fillQuantity,
            FillPrice = fillPrice,
            Fee = fee,
            CreatedAt = occurredAt
        };
}
