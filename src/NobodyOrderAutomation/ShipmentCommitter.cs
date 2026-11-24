using System;

namespace NobodyOrderAutomation;

internal interface IShipmentCommitRuntime
{
    int OrderId { get; }
    int ItemId { get; }
    int InventoryDurability { get; }
    int OrderDurability { get; set; }
    bool IsInDelivery { get; }

    int RemoveInventory(int quantity);
    void RestoreInventory(int quantity);
    void TransitionToInDelivery();
}

internal static class ShipmentCommitter
{
    internal static bool TryCommit(
        IShipmentCommitRuntime runtime,
        int quantity,
        out string error)
    {
        if (quantity <= 0)
        {
            error = $"invalid shipment quantity {quantity}";
            return false;
        }

        int originalOrderDurability = runtime.OrderDurability;
        int removed;
        try
        {
            removed = runtime.RemoveInventory(quantity);
        }
        catch (Exception exception)
        {
            error = $"native RemoveItem failed: {exception.GetType().Name}: {exception.Message}";
            return false;
        }

        if (removed != quantity)
        {
            bool inventoryRolledBack = TryRestoreInventory(runtime, removed);
            error = $"native RemoveItem returned {removed}, expected {quantity}; " +
                $"inventoryRollback={(inventoryRolledBack ? "ok" : "failed")}";
            return false;
        }

        runtime.OrderDurability = runtime.InventoryDurability;
        try
        {
            runtime.TransitionToInDelivery();
            if (runtime.IsInDelivery)
            {
                error = string.Empty;
                return true;
            }

            runtime.OrderDurability = originalOrderDurability;
            bool inventoryRolledBack = TryRestoreInventory(runtime, removed);
            error = "native ChangeOrderStatus returned without entering InDelivery; " +
                $"inventoryRollback={(inventoryRolledBack ? "ok" : "failed")}";
            return false;
        }
        catch (Exception exception)
        {
            if (runtime.IsInDelivery)
            {
                error = $"native ChangeOrderStatus committed before raising " +
                    $"{exception.GetType().Name}: {exception.Message}";
                return true;
            }

            runtime.OrderDurability = originalOrderDurability;
            bool inventoryRolledBack = TryRestoreInventory(runtime, removed);
            error = $"native ChangeOrderStatus failed: {exception.GetType().Name}: {exception.Message}; " +
                $"inventoryRollback={(inventoryRolledBack ? "ok" : "failed")}";
            return false;
        }
    }

    private static bool TryRestoreInventory(IShipmentCommitRuntime runtime, int quantity)
    {
        if (quantity <= 0)
        {
            return quantity == 0;
        }

        try
        {
            runtime.RestoreInventory(quantity);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
