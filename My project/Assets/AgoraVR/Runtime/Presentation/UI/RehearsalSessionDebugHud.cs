using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgoraVR.Presentation.UI
{

public sealed class RehearsalSessionDebugHud : MonoBehaviour
{
    public readonly struct ActionButtonData
    {
        public ActionButtonData(string label, Action onClick)
        {
            Label = label;
            OnClick = onClick;
        }

        public string Label { get; }

        public Action OnClick { get; }
    }

    private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

    private Text _bodyText;
    private RectTransform _buttonRoot;
    private ScrollRect _scrollRect;
    private Text _sessionText;
    private Text _timerText;
    private Text _titleText;

    public static RehearsalSessionDebugHud CreateIfMissing()
    {
        RehearsalSessionDebugHud existingHud = FindAnyObjectByType<RehearsalSessionDebugHud>();
        if (existingHud != null)
        {
            return existingHud;
        }

        EnsureEventSystemExists();

        GameObject canvasObject = new GameObject("Rehearsal Session HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RehearsalSessionDebugHud hud = canvasObject.AddComponent<RehearsalSessionDebugHud>();
        hud.BuildView();
        return hud;
    }

    public void ShowScreen(
        string title,
        string body,
        string sessionSummary,
        float? remainingSeconds,
        params ActionButtonData[] buttons)
    {
        _titleText.text = title;
        _bodyText.text = body;
        _sessionText.text = sessionSummary;
        SetRemainingSeconds(remainingSeconds);
        RebuildButtons(buttons);
        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void SetRemainingSeconds(float? remainingSeconds)
    {
        if (remainingSeconds.HasValue)
        {
            _timerText.text = $"Time Left: {Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds.Value))}s";
            _timerText.gameObject.SetActive(true);
            return;
        }

        _timerText.text = string.Empty;
        _timerText.gameObject.SetActive(false);
    }

    private static void EnsureEventSystemExists()
    {
        EventSystem existingEventSystem = FindAnyObjectByType<EventSystem>();
        if (existingEventSystem != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private void BuildView()
    {
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject frameObject = CreateChild("Frame", gameObject);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 0f);
        frameRect.anchorMax = new Vector2(0f, 1f);
        frameRect.pivot = new Vector2(0f, 0.5f);
        frameRect.sizeDelta = new Vector2(560f, -48f);
        frameRect.anchoredPosition = new Vector2(24f, 0f);

        Image frameBackground = frameObject.AddComponent<Image>();
        frameBackground.color = new Color(0.06f, 0.08f, 0.13f, 0.88f);

        ScrollRect scrollRect = frameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;
        _scrollRect = scrollRect;

        GameObject viewportObject = CreateChild("Viewport", frameObject);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(0f, 0f);
        viewportRect.offsetMax = new Vector2(0f, 0f);
        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = CreateChild("Content", viewportObject);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _titleText = CreateLabel("Title", contentObject.transform, defaultFont, 34, FontStyle.Bold);
        _titleText.color = Color.white;

        _sessionText = CreateLabel("Session", contentObject.transform, defaultFont, 18, FontStyle.Normal);
        _sessionText.color = new Color(0.81f, 0.86f, 0.96f, 1f);

        _timerText = CreateLabel("Timer", contentObject.transform, defaultFont, 22, FontStyle.Bold);
        _timerText.color = new Color(1f, 0.84f, 0.47f, 1f);

        _bodyText = CreateLabel("Body", contentObject.transform, defaultFont, 20, FontStyle.Normal);
        _bodyText.color = new Color(0.93f, 0.95f, 1f, 1f);
        _bodyText.alignment = TextAnchor.UpperLeft;
        _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject buttonRootObject = CreateChild("Buttons", contentObject);
        _buttonRoot = buttonRootObject.GetComponent<RectTransform>();

        VerticalLayoutGroup buttonLayout = buttonRootObject.AddComponent<VerticalLayoutGroup>();
        buttonLayout.spacing = 12f;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = false;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = false;
    }

    private static GameObject CreateChild(string name, GameObject parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static Text CreateLabel(string name, Transform parent, Font font, int size, FontStyle fontStyle)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = fontStyle;
        label.lineSpacing = 1.2f;
        label.supportRichText = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.alignment = TextAnchor.MiddleLeft;

        ContentSizeFitter fitter = labelObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return label;
    }

    private void RebuildButtons(IReadOnlyList<ActionButtonData> buttons)
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            Destroy(_spawnedButtons[i]);
        }

        _spawnedButtons.Clear();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < buttons.Count; i++)
        {
            ActionButtonData buttonData = buttons[i];
            GameObject buttonObject = CreateChild($"Button_{i}", _buttonRoot.gameObject);
            _spawnedButtons.Add(buttonObject);

            Image background = buttonObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.48f, 0.87f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.58f, 0.97f, 1f);
            colors.pressedColor = new Color(0.11f, 0.34f, 0.68f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.targetGraphic = background;

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 54f;

            Text label = CreateLabel("Label", buttonObject.transform, defaultFont, 20, FontStyle.Bold);
            label.text = buttonData.Label;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 10f);
            labelRect.offsetMax = new Vector2(-16f, -10f);

            button.onClick.AddListener(() => buttonData.OnClick?.Invoke());
        }
    }
}
}
