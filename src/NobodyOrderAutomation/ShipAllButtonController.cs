using System;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NobodyOrderAutomation;

internal sealed class ShipAllButtonController
{
    private const float HorizontalGap = 24f;
    private const float FallbackWidth = 262f;
    private const float FallbackHeight = 56f;

    private WarehousePanel? _panel;
    private Button? _sourceButton;
    private RectTransform? _sourceRect;
    private Button? _button;
    private GameObject? _buttonObject;
    private RectTransform? _buttonRect;
    private bool _creationFailed;

    internal void Tick()
    {
        WarehousePanel? panel = UnityEngine.Object.FindObjectOfType<WarehousePanel>();
        bool warehouseVisible = panel != null
            && panel.Pointer != IntPtr.Zero
            && panel.gameObject != null
            && panel.gameObject.activeInHierarchy;

        if (!warehouseVisible)
        {
            Reset();
            return;
        }

        WarehousePanel activePanel = panel!;
        bool panelChanged = _panel == null || _panel.Pointer != activePanel.Pointer;
        if (panelChanged || (!IsButtonAlive() && !_creationFailed))
        {
            Reset();
            _panel = activePanel;
            TryCreateNativeButton(activePanel);
        }

        if (IsButtonAlive())
        {
            ApplyNativeButtonLayout();
            _button!.interactable = OrderAutomationController.FeatureEnabled
                && !OrderAutomationController.BatchActive;
        }
    }

    internal void Reset()
    {
        if (_buttonObject != null)
        {
            try
            {
                UnityEngine.Object.Destroy(_buttonObject);
            }
            catch
            {
                // The warehouse hierarchy may already have destroyed the clean UI button.
            }
        }

        _sourceButton = null;
        _sourceRect = null;
        _button = null;
        _buttonObject = null;
        _buttonRect = null;
        _creationFailed = false;
        _panel = null;
    }

    private void TryCreateNativeButton(WarehousePanel panel)
    {
        Button? source = panel.closeBtn;
        if (source == null || source.Pointer == IntPtr.Zero || source.gameObject == null)
        {
            Plugin.ModLog.LogWarning("Ship All native button deferred: no visible warehouse close-button template is available.");
            return;
        }

        GameObject? cleanButton = null;
        string stage = "resolve close button";
        try
        {
            RectTransform? sourceRect = source.GetComponent<RectTransform>();
            Transform? parent = source.transform.parent;
            if (sourceRect == null || parent == null)
            {
                Plugin.ModLog.LogWarning("Ship All native button creation failed: close button lacks a usable UI parent.");
                return;
            }

            stage = "create clean root";
            cleanButton = new GameObject(
                "NobodyOrderAutomation_ShipAll",
                new[]
                {
                    Il2CppType.Of<RectTransform>(),
                });
            cleanButton.layer = source.gameObject.layer;
            cleanButton.transform.SetParent(parent, false);

            stage = "bind RectTransform";
            Component rawRect = cleanButton.GetComponent(Il2CppType.Of<RectTransform>());
            RectTransform buttonRect = new(rawRect.Pointer);

            stage = "add visual components";
            cleanButton.AddComponent(Il2CppType.Of<CanvasRenderer>());
            Component rawImage = cleanButton.AddComponent(Il2CppType.Of<Image>());
            Component rawButton = cleanButton.AddComponent(Il2CppType.Of<Button>());
            Component rawLayout = cleanButton.AddComponent(Il2CppType.Of<LayoutElement>());
            Image image = new(rawImage.Pointer);
            Button button = new(rawButton.Pointer);
            LayoutElement layoutElement = new(rawLayout.Pointer);

            layoutElement.ignoreLayout = true;

            stage = "copy visual style";
            CopyImageStyle(source, image);
            CopyButtonStyle(source, button, image);
            stage = "create label";
            CreateLabel(cleanButton.transform, source);

            stage = "bind click";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener((UnityAction)HandleClick);

            _sourceButton = source;
            _sourceRect = sourceRect;
            _buttonObject = cleanButton;
            _button = button;
            _buttonRect = buttonRect;
            ApplyNativeButtonLayout();
            Plugin.ModLog.LogDebug(
                $"Clean Ship All button created beside template={source.gameObject.name}: " +
                $"size=({buttonRect.rect.width:0.##},{buttonRect.rect.height:0.##}), " +
                $"position=({buttonRect.anchoredPosition.x:0.##},{buttonRect.anchoredPosition.y:0.##}).");
        }
        catch (Exception exception)
        {
            if (cleanButton != null)
            {
                try
                {
                    UnityEngine.Object.Destroy(cleanButton);
                }
                catch
                {
                    // Preserve the original creation failure in the diagnostic below.
                }
            }

            _creationFailed = true;
            Plugin.ModLog.LogWarning(
                $"Ship All native button creation failed during {stage}: " +
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private void ApplyNativeButtonLayout()
    {
        if (_sourceButton == null
            || _sourceButton.Pointer == IntPtr.Zero
            || _sourceRect == null
            || _sourceRect.Pointer == IntPtr.Zero
            || _buttonObject == null
            || _buttonRect == null
            || _buttonRect.Pointer == IntPtr.Zero)
        {
            return;
        }

        if (!_buttonObject.activeSelf)
        {
            _buttonObject.SetActive(true);
        }

        float width = ShipAllButtonLayout.ResolveDimension(_sourceRect.rect.width, FallbackWidth);
        float height = ShipAllButtonLayout.ResolveDimension(_sourceRect.rect.height, FallbackHeight);

        _buttonRect.anchorMin = _sourceRect.anchorMin;
        _buttonRect.anchorMax = _sourceRect.anchorMax;
        _buttonRect.pivot = _sourceRect.pivot;
        _buttonRect.sizeDelta = new Vector2(width, height);
        _buttonRect.localScale = _sourceRect.localScale;
        _buttonRect.localRotation = _sourceRect.localRotation;
        _buttonRect.anchoredPosition = new Vector2(
            ShipAllButtonLayout.ComputeLeftAnchoredX(
                _sourceRect.anchoredPosition.x,
                width,
                HorizontalGap),
            _sourceRect.anchoredPosition.y);
        _buttonRect.SetAsLastSibling();
    }

    private static void CopyImageStyle(Button source, Image destination)
    {
        Component? rawSourceImage = source.GetComponentInChildren(Il2CppType.Of<Image>(), true);
        if (rawSourceImage == null || rawSourceImage.Pointer == IntPtr.Zero)
        {
            destination.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            return;
        }

        Image sourceImage = new(rawSourceImage.Pointer);

        destination.sprite = sourceImage.sprite;
        destination.overrideSprite = sourceImage.overrideSprite;
        destination.material = sourceImage.material;
        destination.color = sourceImage.color;
        destination.type = sourceImage.type;
        destination.preserveAspect = sourceImage.preserveAspect;
        destination.raycastTarget = true;
    }

    private static void CopyButtonStyle(Button source, Button destination, Graphic targetGraphic)
    {
        destination.transition = source.transition;
        destination.colors = source.colors;
        destination.spriteState = source.spriteState;
        destination.navigation = source.navigation;
        destination.targetGraphic = targetGraphic;
        destination.interactable = true;
    }

    private static void CreateLabel(Transform parent, Button source)
    {
        Component? rawSourceText = source.GetComponentInChildren(Il2CppType.Of<Text>(), true);
        if (rawSourceText != null && rawSourceText.Pointer != IntPtr.Zero)
        {
            CreateLegacyLabel(parent, new Text(rawSourceText.Pointer));
            return;
        }

        Component? rawSourceTmp = source.GetComponentInChildren(Il2CppType.Of<TextMeshProUGUI>(), true);
        if (rawSourceTmp != null && rawSourceTmp.Pointer != IntPtr.Zero)
        {
            CreateTmpLabel(parent, new TextMeshProUGUI(rawSourceTmp.Pointer));
            return;
        }

        CreateLegacyLabel(parent, null);
    }

    private static void CreateLegacyLabel(Transform parent, Text? template)
    {
        GameObject labelObject = new GameObject(
            "Label",
            new[]
            {
                Il2CppType.Of<RectTransform>(),
            });
        labelObject.layer = parent.gameObject.layer;
        labelObject.transform.SetParent(parent, false);

        Component rawRect = labelObject.GetComponent(Il2CppType.Of<RectTransform>());
        labelObject.AddComponent(Il2CppType.Of<CanvasRenderer>());
        Component rawText = labelObject.AddComponent(Il2CppType.Of<Text>());
        RectTransform rect = new(rawRect.Pointer);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Text label = new(rawText.Pointer);
        label.text = "Ship All";
        label.raycastTarget = false;
        label.alignment = TextAnchor.MiddleCenter;
        if (template != null)
        {
            label.font = template.font;
            label.fontSize = template.fontSize;
            label.fontStyle = template.fontStyle;
            label.color = template.color;
            label.lineSpacing = template.lineSpacing;
            label.supportRichText = template.supportRichText;
        }
        else
        {
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 22;
            label.color = Color.white;
        }
    }

    private static void CreateTmpLabel(Transform parent, TMP_Text template)
    {
        GameObject labelObject = new GameObject(
            "Label",
            new[]
            {
                Il2CppType.Of<RectTransform>(),
            });
        labelObject.layer = parent.gameObject.layer;
        labelObject.transform.SetParent(parent, false);

        Component rawRect = labelObject.GetComponent(Il2CppType.Of<RectTransform>());
        labelObject.AddComponent(Il2CppType.Of<CanvasRenderer>());
        Component rawText = labelObject.AddComponent(Il2CppType.Of<TextMeshProUGUI>());
        RectTransform rect = new(rawRect.Pointer);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        TextMeshProUGUI label = new(rawText.Pointer);
        label.text = "Ship All";
        label.raycastTarget = false;
        label.font = template.font;
        label.fontSize = template.fontSize;
        label.fontStyle = template.fontStyle;
        label.color = template.color;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void HandleClick()
    {
        OrderAutomationController.TryBeginShipAll();
    }

    private bool IsButtonAlive()
    {
        return _button != null
            && _button.Pointer != IntPtr.Zero
            && _buttonObject != null
            && _buttonRect != null
            && _buttonRect.Pointer != IntPtr.Zero
            && _sourceButton != null
            && _sourceButton.Pointer != IntPtr.Zero
            && _sourceRect != null
            && _sourceRect.Pointer != IntPtr.Zero;
    }
}
