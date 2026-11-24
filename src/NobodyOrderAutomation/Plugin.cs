using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobodyOrderAutomation;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "com.nobody221126.nobodyorderautomation";
    public const string PluginName = "Nobody Order Automation";
    public const string PluginVersion = "1.0.0";

    internal static ManualLogSource ModLog { get; private set; } = null!;

    public override void Load()
    {
        ModLog = Log;
        AddComponent<OrderAutomationController>();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded; direct native Ship All is enabled.");
    }
}

public sealed class OrderAutomationController : MonoBehaviour
{
    private static OrderAutomationController? _instance;

    private bool _firstUpdateObserved;
    private bool _batchActive;
    private int _confirmedCount;
    private int? _batchTotal;
    private int _insufficientCount;
    private int _duplicateProtectedCount;
    private readonly HashSet<int> _processedOrderIdsThisSession = new();
    private ShipAllButtonController? _shipAllButton;

    internal static bool FeatureEnabled => _instance != null;
    internal static bool BatchActive => _instance?._batchActive == true;
    internal static int BatchConfirmed => _instance?._confirmedCount ?? 0;
    internal static int? BatchTotal => _instance?._batchTotal;

    public OrderAutomationController(IntPtr pointer) : base(pointer)
    {
    }

    public void Awake()
    {
        _instance = this;
        _shipAllButton = new ShipAllButtonController();
    }

    public void Update()
    {
        if (!_firstUpdateObserved)
        {
            _firstUpdateObserved = true;
            Plugin.ModLog.LogInfo("Unity Update loop confirmed. Ship All is ready.");
        }

        _shipAllButton?.Tick();

    }

    internal static bool TryBeginShipAll()
    {
        if (_instance == null)
        {
            Plugin.ModLog.LogWarning("Ship All ignored because order automation is unavailable.");
            return false;
        }

        if (_instance._batchActive)
        {
            Plugin.ModLog.LogInfo("Ship All ignored because a batch is already active.");
            return false;
        }

        _instance.RunShipAll();
        return true;
    }

    private void RunShipAll()
    {
        WarehousePanel? panel = UnityEngine.Object.FindObjectOfType<WarehousePanel>();
        Il2CppSystem.Collections.Generic.List<OnLineOrderInfo>? orders = panel?.waitOrderList;
        if (panel == null || panel.Pointer == IntPtr.Zero || orders == null)
        {
            Plugin.ModLog.LogError("Ship All stopped: WarehousePanel order model is unavailable.");
            return;
        }

        FrameWork.PlayerManager? playerManager = FrameWork.GameManager.Instance?.GetPlayerManager();
        WarehouseSystem? warehouse = playerManager?.GetWarehouseSystem;
        OnLineShopSystem? shop = playerManager?.GetOnLineShopSystem;
        if (warehouse == null || shop == null)
        {
            Plugin.ModLog.LogError("Ship All stopped: warehouse or online-shop business system is unavailable.");
            return;
        }

        List<BatchOrder> snapshot = SnapshotOrders(orders);
        _batchActive = true;
        _batchTotal = snapshot.Count;
        _confirmedCount = 0;
        _insufficientCount = 0;
        _duplicateProtectedCount = 0;
        Plugin.ModLog.LogInfo($"Direct Ship All started: snapshot={snapshot.Count}.");

        BatchProcessResult? result = null;
        string completionReason = "all-snapshot-orders-examined";
        try
        {
            GameShipAllRuntime runtime = new(
                warehouse,
                shop,
                orders);
            result = ShipAllBatchProcessor.Process(
                snapshot,
                _processedOrderIdsThisSession,
                runtime,
                ReportBatchEvent);
            completionReason = result.CompletionReason;
            _confirmedCount = result.ConfirmedCount;
            _insufficientCount = result.InsufficientCount;
            _duplicateProtectedCount = result.DuplicateCount;
        }
        catch (Exception exception)
        {
            completionReason = "unexpected-exception";
            Plugin.ModLog.LogError(
                $"Ship All stopped by {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            int removedFromPanel = 0;
            int hiddenRenderedRows = 0;
            try
            {
                removedFromPanel = RemoveProcessedOrdersFromPanel(
                    orders,
                    result?.OrderIdsToRemoveFromPanel ?? new HashSet<int>());
            }
            catch (Exception exception)
            {
                Plugin.ModLog.LogWarning(
                    $"Orders were processed but stale rows could not be removed from the warehouse model: " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            try
            {
                panel.RefreshPanel();
                hiddenRenderedRows = HideOutOfRangeRenderedOrderRows(
                    panel,
                    orders.Count);
            }
            catch (Exception exception)
            {
                Plugin.ModLog.LogWarning(
                    $"Orders were processed but the warehouse UI refresh failed: " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            Plugin.ModLog.LogInfo(
                $"Direct Ship All complete: reason={completionReason}, confirmed={_confirmedCount}, " +
                $"insufficient={_insufficientCount}, duplicateProtected={_duplicateProtectedCount}, " +
                $"malformed={result?.MalformedCount ?? 0}, removedFromPanel={removedFromPanel}, " +
                $"hiddenRenderedRows={hiddenRenderedRows}, snapshot={snapshot.Count}.");
            _batchActive = false;
        }
    }

    private static void ReportBatchEvent(BatchEvent batchEvent)
    {
        BatchOrder order = batchEvent.Order;
        switch (batchEvent.Kind)
        {
            case BatchEventKind.Insufficient:
                Plugin.ModLog.LogDebug(
                    $"Skipped order {order.OrderId}: insufficient inventory " +
                    $"(itemId={order.ItemId}, required={order.Quantity}, available={batchEvent.Available}).");
                break;
            case BatchEventKind.Malformed:
                Plugin.ModLog.LogWarning(
                    $"Skipped malformed order snapshot: orderId={order.OrderId}, " +
                    $"itemId={order.ItemId}, quantity={order.Quantity}.");
                break;
            case BatchEventKind.Failed:
                Plugin.ModLog.LogError(
                    $"Ship All stopped at order {order.OrderId}: {batchEvent.Error}");
                break;
        }
    }

    private static int HideOutOfRangeRenderedOrderRows(
        WarehousePanel panel,
        int remainingOrderCount)
    {
        Il2CppSystem.Collections.Generic.Dictionary<GameObject, int>? renderedRows =
            panel.onlineObjDic;
        if (renderedRows == null)
        {
            return 0;
        }

        int hidden = 0;
        foreach (var renderedRow in renderedRows)
        {
            GameObject? row = renderedRow.Key;
            if (row == null || renderedRow.Value < remainingOrderCount)
            {
                continue;
            }

            row.SetActive(false);
            hidden++;
        }

        return hidden;
    }

    private static int RemoveProcessedOrdersFromPanel(
        Il2CppSystem.Collections.Generic.List<OnLineOrderInfo> orders,
        HashSet<int> processedOrderIds)
    {
        int removed = 0;
        for (int index = orders.Count - 1; index >= 0; index--)
        {
            OnLineOrderInfo? order = orders[index];
            if (order == null || !processedOrderIds.Contains(order.OrderId))
            {
                continue;
            }

            orders.RemoveAt(index);
            removed++;
        }

        return removed;
    }

    private static List<BatchOrder> SnapshotOrders(
        Il2CppSystem.Collections.Generic.List<OnLineOrderInfo> orders)
    {
        List<BatchOrder> snapshot = new(orders.Count);
        HashSet<int> seenOrderIds = new();

        for (int index = 0; index < orders.Count; index++)
        {
            OnLineOrderInfo? order = orders[index];
            if (order == null || !seenOrderIds.Add(order.OrderId))
            {
                continue;
            }

            ItemGoodsConfig? itemConfig = order.itemGoodsConfig;
            snapshot.Add(new BatchOrder(
                order.OrderId,
                itemConfig?.itemID ?? 0,
                order.ShopCount));
        }

        return snapshot;
    }

    private sealed class GameShipAllRuntime : IShipAllRuntime
    {
        private readonly WarehouseSystem _warehouse;
        private readonly OnLineShopSystem _shop;
        private readonly Dictionary<int, OnLineOrderInfo> _ordersById = new();
        internal GameShipAllRuntime(
            WarehouseSystem warehouse,
            OnLineShopSystem shop,
            Il2CppSystem.Collections.Generic.List<OnLineOrderInfo> orders)
        {
            _warehouse = warehouse;
            _shop = shop;
            for (int index = 0; index < orders.Count; index++)
            {
                OnLineOrderInfo? order = orders[index];
                if (order != null && order.OrderId > 0)
                {
                    _ordersById.TryAdd(order.OrderId, order);
                }
            }
        }

        public int GetAvailable(BatchOrder order)
        {
            return _warehouse.GetItemCount(order.ItemId);
        }

        public bool TryShip(BatchOrder order, out string error)
        {
            if (!_ordersById.TryGetValue(order.OrderId, out OnLineOrderInfo? nativeOrder) ||
                nativeOrder == null || nativeOrder.Pointer == IntPtr.Zero)
            {
                error = $"native order {order.OrderId} is unavailable";
                return false;
            }

            WareItemInfo? inventoryItem = _warehouse.GetWareitemInfo(order.ItemId);
            if (inventoryItem == null || inventoryItem.Pointer == IntPtr.Zero)
            {
                error = $"inventory item {order.ItemId} is unavailable";
                return false;
            }

            GameShipmentCommitRuntime commitRuntime = new(
                _warehouse,
                _shop,
                nativeOrder,
                inventoryItem);
            bool committed = ShipmentCommitter.TryCommit(
                commitRuntime,
                order.Quantity,
                out error);
            if (committed)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Plugin.ModLog.LogWarning($"Order {order.OrderId} committed with warning: {error}");
                    error = string.Empty;
                }
            }

            return committed;
        }

        private sealed class GameShipmentCommitRuntime : IShipmentCommitRuntime
        {
            private readonly WarehouseSystem _warehouse;
            private readonly OnLineShopSystem _shop;
            private readonly OnLineOrderInfo _order;
            private readonly WareItemInfo _inventoryItem;

            internal GameShipmentCommitRuntime(
                WarehouseSystem warehouse,
                OnLineShopSystem shop,
                OnLineOrderInfo order,
                WareItemInfo inventoryItem)
            {
                _warehouse = warehouse;
                _shop = shop;
                _order = order;
                _inventoryItem = inventoryItem;
            }

            public int OrderId => _order.OrderId;
            public int ItemId => _inventoryItem.itemmID;
            public int InventoryDurability => _inventoryItem.Durability;
            public int OrderDurability
            {
                get => _order.Durability;
                set => _order.Durability = value;
            }
            public bool IsInDelivery => _order.OrderStatus == OnLineOrderStatus.InDelivery;

            public int RemoveInventory(int quantity)
            {
                return _warehouse.RemoveItem(ItemId, quantity);
            }

            public void RestoreInventory(int quantity)
            {
                _warehouse.AddItem(_inventoryItem, quantity);
            }

            public void TransitionToInDelivery()
            {
                _shop.ChangeOrderStatus(OrderId, OnLineOrderStatus.InDelivery);
            }
        }
    }
}
