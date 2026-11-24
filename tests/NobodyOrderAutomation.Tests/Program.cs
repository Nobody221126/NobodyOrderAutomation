using NobodyOrderAutomation;
using System;
using System.Collections.Generic;

internal static class Program
{
    private static int Main()
    {
        (string Name, Action Test)[] tests =
        {
            ("non-contiguous order IDs", NonContiguousOrderIds),
            ("insufficient first order does not block later order", InsufficientFirstOrderDoesNotBlockLaterOrder),
            ("inventory depletion mid-batch", InventoryDepletionMidBatch),
            ("repeat run is duplicate-safe", RepeatRunIsDuplicateSafe),
            ("malformed order is preserved", MalformedOrderIsPreserved),
            ("shipment copies durability and transitions to delivery", ShipmentCopiesDurabilityAndTransitions),
            ("shipment transition failure rolls back inventory and durability", ShipmentTransitionFailureRollsBack),
            ("shipment exception after native commit is not rolled back", ShipmentExceptionAfterCommitIsAccepted),
            ("partial inventory removal is rolled back", PartialInventoryRemovalIsRolledBack),
            ("Ship All button is placed left of Quit without overlap", ShipAllButtonIsPlacedLeftOfQuit),
            ("Ship All button dimensions reject invalid native geometry", ShipAllButtonDimensionsRejectInvalidGeometry),
        };

        int failed = 0;
        foreach ((string name, Action test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS: {name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL: {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"RESULT: {tests.Length - failed}/{tests.Length} passed");
        return failed == 0 ? 0 : 1;
    }

    private static void NonContiguousOrderIds()
    {
        FakeRuntime runtime = new(new Dictionary<int, int> { [10] = 3 });
        BatchProcessResult result = Run(runtime,
            new BatchOrder(5212, 10, 1),
            new BatchOrder(5225, 10, 1),
            new BatchOrder(5300, 10, 1));

        Equal(3, result.ConfirmedCount);
        Equal(0, result.InsufficientCount);
        Equal(0, runtime.Inventory[10]);
    }

    private static void InsufficientFirstOrderDoesNotBlockLaterOrder()
    {
        FakeRuntime runtime = new(new Dictionary<int, int> { [10] = 0, [20] = 1 });
        BatchProcessResult result = Run(runtime,
            new BatchOrder(100, 10, 1),
            new BatchOrder(200, 20, 1));

        Equal(1, result.InsufficientCount);
        Equal(1, result.ConfirmedCount);
        True(!result.OrderIdsToRemoveFromPanel.Contains(100), "insufficient order must remain visible");
        True(result.OrderIdsToRemoveFromPanel.Contains(200), "fulfilled later order must be removed");
    }

    private static void InventoryDepletionMidBatch()
    {
        FakeRuntime runtime = new(new Dictionary<int, int> { [10] = 2 });
        BatchProcessResult result = Run(runtime,
            new BatchOrder(1, 10, 1),
            new BatchOrder(2, 10, 1),
            new BatchOrder(3, 10, 1));

        Equal(2, result.ConfirmedCount);
        Equal(1, result.InsufficientCount);
        Equal(0, runtime.Inventory[10]);
        True(!result.OrderIdsToRemoveFromPanel.Contains(3), "order encountered after depletion must remain");
    }

    private static void RepeatRunIsDuplicateSafe()
    {
        FakeRuntime runtime = new(new Dictionary<int, int> { [10] = 2 });
        HashSet<int> processed = new();
        BatchOrder[] orders = { new(1, 10, 1), new(2, 10, 1) };

        BatchProcessResult first = ShipAllBatchProcessor.Process(orders, processed, runtime);
        BatchProcessResult second = ShipAllBatchProcessor.Process(orders, processed, runtime);

        Equal(2, first.ConfirmedCount);
        Equal(0, second.ConfirmedCount);
        Equal(2, second.DuplicateCount);
        Equal(0, runtime.Inventory[10]);
        Equal(2, runtime.ShippedOrderIds.Count);
    }

    private static void MalformedOrderIsPreserved()
    {
        FakeRuntime runtime = new(new Dictionary<int, int> { [10] = 1 });
        BatchProcessResult result = Run(runtime,
            new BatchOrder(0, 10, 1),
            new BatchOrder(2, 10, 1));

        Equal(1, result.MalformedCount);
        Equal(1, result.ConfirmedCount);
        True(!result.OrderIdsToRemoveFromPanel.Contains(0), "malformed order must not be removed");
    }

    private static BatchProcessResult Run(FakeRuntime runtime, params BatchOrder[] orders)
    {
        return ShipAllBatchProcessor.Process(orders, new HashSet<int>(), runtime);
    }

    private static void ShipmentCopiesDurabilityAndTransitions()
    {
        FakeShipmentCommitRuntime runtime = new(inventoryCount: 3, inventoryDurability: 82, orderDurability: 0);

        bool committed = ShipmentCommitter.TryCommit(runtime, quantity: 1, out string error);

        True(committed, error);
        Equal(2, runtime.InventoryCount);
        Equal(82, runtime.OrderDurability);
        Equal(1, runtime.TransitionCalls);
        True(runtime.IsInDelivery, "order must be in delivery after a successful shipment");
        Equal(0, runtime.RestoreCalls);
    }

    private static void ShipmentTransitionFailureRollsBack()
    {
        FakeShipmentCommitRuntime runtime = new(inventoryCount: 3, inventoryDurability: 82, orderDurability: 17)
        {
            ThrowDuringTransition = true,
        };

        bool committed = ShipmentCommitter.TryCommit(runtime, quantity: 1, out _);

        True(!committed, "transition failure must fail the shipment");
        Equal(3, runtime.InventoryCount);
        Equal(17, runtime.OrderDurability);
        Equal(1, runtime.RestoreCalls);
        True(!runtime.IsInDelivery, "failed transition must leave the order waiting");
    }

    private static void ShipmentExceptionAfterCommitIsAccepted()
    {
        FakeShipmentCommitRuntime runtime = new(inventoryCount: 3, inventoryDurability: 82, orderDurability: 17)
        {
            ThrowDuringTransition = true,
            CommitBeforeThrow = true,
        };

        bool committed = ShipmentCommitter.TryCommit(runtime, quantity: 1, out string error);

        True(committed, error);
        Equal(2, runtime.InventoryCount);
        Equal(82, runtime.OrderDurability);
        Equal(0, runtime.RestoreCalls);
        True(runtime.IsInDelivery, "native committed state must win over a wrapper exception");
    }

    private static void PartialInventoryRemovalIsRolledBack()
    {
        FakeShipmentCommitRuntime runtime = new(inventoryCount: 3, inventoryDurability: 82, orderDurability: 17)
        {
            ForcedRemovedQuantity = 1,
        };

        bool committed = ShipmentCommitter.TryCommit(runtime, quantity: 2, out _);

        True(!committed, "partial inventory removal must fail the shipment");
        Equal(3, runtime.InventoryCount);
        Equal(17, runtime.OrderDurability);
        Equal(1, runtime.RestoreCalls);
        Equal(0, runtime.TransitionCalls);
    }

    private static void ShipAllButtonIsPlacedLeftOfQuit()
    {
        const float sourceX = -8f;
        const float width = 262f;
        const float gap = 24f;

        float shipAllX = ShipAllButtonLayout.ComputeLeftAnchoredX(sourceX, width, gap);

        Equal(-294f, shipAllX);
        True(sourceX - shipAllX >= width + gap,
            "Ship All must remain a full button width plus the requested gap left of Quit");
        Equal(-270f, ShipAllButtonLayout.ComputeLeftAnchoredX(sourceX, -width, -gap));
    }

    private static void ShipAllButtonDimensionsRejectInvalidGeometry()
    {
        Equal(262f, ShipAllButtonLayout.ResolveDimension(262f, 150f));
        Equal(262f, ShipAllButtonLayout.ResolveDimension(-262f, 150f));
        Equal(150f, ShipAllButtonLayout.ResolveDimension(0f, 150f));
        Equal(150f, ShipAllButtonLayout.ResolveDimension(float.PositiveInfinity, 150f));
        Equal(150f, ShipAllButtonLayout.ResolveDimension(float.NaN, 150f));
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"expected {expected}, got {actual}");
        }
    }

    private static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class FakeRuntime : IShipAllRuntime
    {
        internal FakeRuntime(Dictionary<int, int> inventory)
        {
            Inventory = inventory;
        }

        internal Dictionary<int, int> Inventory { get; }
        internal List<int> ShippedOrderIds { get; } = new();

        public int GetAvailable(BatchOrder order)
        {
            return Inventory.TryGetValue(order.ItemId, out int count) ? count : 0;
        }

        public bool TryShip(BatchOrder order, out string error)
        {
            int available = GetAvailable(order);
            if (available < order.Quantity)
            {
                error = "insufficient";
                return false;
            }

            Inventory[order.ItemId] = available - order.Quantity;
            ShippedOrderIds.Add(order.OrderId);
            error = string.Empty;
            return true;
        }
    }

    private sealed class FakeShipmentCommitRuntime : IShipmentCommitRuntime
    {
        internal FakeShipmentCommitRuntime(int inventoryCount, int inventoryDurability, int orderDurability)
        {
            InventoryCount = inventoryCount;
            InventoryDurability = inventoryDurability;
            OrderDurability = orderDurability;
        }

        public int OrderId => 5212;
        public int ItemId => 10;
        public int InventoryDurability { get; }
        public int OrderDurability { get; set; }
        public bool IsInDelivery { get; private set; }
        internal int InventoryCount { get; private set; }
        internal int TransitionCalls { get; private set; }
        internal int RestoreCalls { get; private set; }
        internal bool ThrowDuringTransition { get; init; }
        internal bool CommitBeforeThrow { get; init; }
        internal int? ForcedRemovedQuantity { get; init; }

        public int RemoveInventory(int quantity)
        {
            int removed = ForcedRemovedQuantity ?? quantity;
            InventoryCount -= removed;
            return removed;
        }

        public void RestoreInventory(int quantity)
        {
            RestoreCalls++;
            InventoryCount += quantity;
        }

        public void TransitionToInDelivery()
        {
            TransitionCalls++;
            if (CommitBeforeThrow)
            {
                IsInDelivery = true;
            }

            if (ThrowDuringTransition)
            {
                throw new InvalidOperationException("simulated native transition failure");
            }

            IsInDelivery = true;
        }
    }
}
