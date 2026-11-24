using System;
using System.Collections.Generic;

namespace NobodyOrderAutomation;

internal readonly struct BatchOrder
{
    internal BatchOrder(int orderId, int itemId, int quantity)
    {
        OrderId = orderId;
        ItemId = itemId;
        Quantity = quantity;
    }

    internal int OrderId { get; }
    internal int ItemId { get; }
    internal int Quantity { get; }
}

internal interface IShipAllRuntime
{
    int GetAvailable(BatchOrder order);
    bool TryShip(BatchOrder order, out string error);
}

internal enum BatchEventKind
{
    Confirmed,
    Duplicate,
    Insufficient,
    Malformed,
    Failed,
}

internal readonly struct BatchEvent
{
    internal BatchEvent(BatchEventKind kind, BatchOrder order, int available = 0, string error = "")
    {
        Kind = kind;
        Order = order;
        Available = available;
        Error = error;
    }

    internal BatchEventKind Kind { get; }
    internal BatchOrder Order { get; }
    internal int Available { get; }
    internal string Error { get; }
}

internal sealed class BatchProcessResult
{
    internal string CompletionReason { get; set; } = "all-snapshot-orders-examined";
    internal int ConfirmedCount { get; set; }
    internal int InsufficientCount { get; set; }
    internal int DuplicateCount { get; set; }
    internal int MalformedCount { get; set; }
    internal HashSet<int> OrderIdsToRemoveFromPanel { get; } = new();
}

internal static class ShipAllBatchProcessor
{
    internal static BatchProcessResult Process(
        IReadOnlyList<BatchOrder> orders,
        HashSet<int> alreadyProcessedOrderIds,
        IShipAllRuntime runtime,
        Action<BatchEvent>? report = null)
    {
        BatchProcessResult result = new();

        foreach (BatchOrder order in orders)
        {
            if (alreadyProcessedOrderIds.Contains(order.OrderId))
            {
                result.DuplicateCount++;
                result.OrderIdsToRemoveFromPanel.Add(order.OrderId);
                report?.Invoke(new BatchEvent(BatchEventKind.Duplicate, order));
                continue;
            }

            if (order.OrderId <= 0 || order.ItemId <= 0 || order.Quantity <= 0)
            {
                result.MalformedCount++;
                report?.Invoke(new BatchEvent(BatchEventKind.Malformed, order));
                continue;
            }

            int available = runtime.GetAvailable(order);
            if (available < order.Quantity)
            {
                result.InsufficientCount++;
                report?.Invoke(new BatchEvent(BatchEventKind.Insufficient, order, available));
                continue;
            }

            if (!runtime.TryShip(order, out string error))
            {
                result.CompletionReason = $"runtime-failed-order-{order.OrderId}";
                report?.Invoke(new BatchEvent(BatchEventKind.Failed, order, available, error));
                break;
            }

            alreadyProcessedOrderIds.Add(order.OrderId);
            result.OrderIdsToRemoveFromPanel.Add(order.OrderId);
            result.ConfirmedCount++;
            report?.Invoke(new BatchEvent(BatchEventKind.Confirmed, order, available));
        }

        return result;
    }
}
