using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightShift
{
    internal sealed class NightShiftHud
    {
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private const float MenuBottom = 104f;
        private const float ButtonWidth = 260f;
        private const float ButtonHeight = 106f;
        private const float ButtonOffset = 142f;
        private const float PlayingStabilityY = 104f;
        private const float ResultsStabilityY = 426f;
        private static readonly Color Ink = new Color32(12, 20, 29, 255);
        private static readonly Color Amber = new Color32(255, 203, 124, 255);
        private static readonly Color DarkButton = new Color32(32, 52, 64, 248);
        private static readonly Color Coral = new Color32(255, 181, 164, 255);
        private static readonly Color Plot = new Color32(20, 34, 44, 246);
        private static readonly Color Contact = new Color32(82, 108, 119, 255);
        private static readonly Color SafeRegion = new Color32(84, 175, 154, 255);

        private readonly RectTransform safeArea;
        private readonly RectTransform ready;
        private readonly RectTransform ended;
        private readonly RectTransform stability;
        private readonly RectTransform stabilityDiagram;
        private readonly RectTransform contactRegion;
        private readonly RectTransform safeRegion;
        private readonly RectTransform loadMarker;
        private readonly Image loadMarkerFill;
        private readonly TMP_FontAsset font;
        private readonly TMP_FontAsset displayFont;
        private readonly int buttonFontSize;
        private readonly TextMeshProUGUI scoreText;
        private readonly TextMeshProUGUI resultScoreText;
        private readonly TextMeshProUGUI reasonText;
        private readonly TextMeshProUGUI stabilityText;
        private GameMode mode;

        internal NightShiftHud(Transform parent, TMP_FontAsset chineseFont, TMP_FontAsset displayFont,
            Action<GameMode> start, Action restart, Action changeMode)
        {
            bool chinese = chineseFont != null;
            font = chinese ? chineseFont : TMP_Settings.defaultFontAsset;
            this.displayFont = displayFont != null ? displayFont : font;
            buttonFontSize = chinese ? 54 : 38;

            var root = new GameObject("Night Shift HUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            // Overlay canvases belong at the top level of their scene.
            if (parent != null && parent.gameObject.scene != root.scene)
                SceneManager.MoveGameObjectToScene(root, parent.gameObject.scene);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvas.pixelPerfect = true;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            safeArea = CreateRect("Safe Area", root.transform, Vector2.zero, 0f, 0f, 0f, 0f);
            safeArea.anchorMax = Vector2.one;
            EnsureEventSystem(root.transform);

            scoreText = Label("Score", "", safeArea, Top, 0f, -24f, 630f, 123f, 100, Color.white);
            scoreText.fontStyle = FontStyles.Bold;
            FitSingleLine(scoreText, 72);

            ready = CreateRect("Mode Selection", safeArea, Bottom, 0f, MenuBottom, 0f, 0f);
            TextMeshProUGUI title = Label("Title", chinese ? "夜班吊装" : "NIGHT SHIFT", ready, Bottom,
                0f, 138f, 680f, 130f, 108, Color.white, display: true);
            FitSingleLine(title, 56);
            AddButton(ready, chinese ? "休闲" : "CASUAL", -ButtonOffset, DarkButton, Color.white,
                () => start?.Invoke(GameMode.Casual));
            AddButton(ready, chinese ? "标准" : "STANDARD", ButtonOffset, Amber, Ink,
                () => start?.Invoke(GameMode.Standard));

            ended = CreateRect("Results", safeArea, Bottom, 0f, MenuBottom, 0f, 0f);
            resultScoreText = Label("Final Score", "", ended, Bottom, 0f, 169f, 700f, 174f, 156, Color.white);
            resultScoreText.fontStyle = FontStyles.Bold;
            FitSingleLine(resultScoreText, 82);
            reasonText = Label("Failure Reason", "", ended, Bottom, 0f, 113f, 620f, 66f, 54, Coral);
            reasonText.fontStyle = FontStyles.Bold;
            FitSingleLine(reasonText, 36);
            AddButton(ended, chinese ? "重来" : "RETRY", -ButtonOffset, Amber, Ink, restart);
            AddButton(ended, chinese ? "换模式" : "MODE", ButtonOffset, DarkButton, Color.white, changeMode);

            stability = CreateRect("Stack Stability", safeArea, Bottom, 0f, PlayingStabilityY, 0f, 0f);
            stabilityDiagram = Panel("Top-down Joint Diagram", stability, Bottom, -118f, 0f, 136f, 136f, Plot, 68);
            contactRegion = Panel("Contact Area", stabilityDiagram, Center, 0f, 0f, 0f, 0f, Contact);
            safeRegion = Panel("Inset Safe Area", stabilityDiagram, Center, 0f, 0f, 0f, 0f, SafeRegion);
            loadMarker = Panel("Load Center", stabilityDiagram, Center, 0f, 0f, 20f, 20f, Ink, 10);
            loadMarkerFill = Panel("Marker Fill", loadMarker, Center, 0f, 0f, 13f, 13f, Color.white, 6)
                .GetComponent<Image>();
            stabilityText = Label("Stability Status", "", stability, Bottom, 172f, 36f, 400f, 64f, 46, Amber);
            stabilityText.fontStyle = FontStyles.Bold;
            FitSingleLine(stabilityText, 30);

            SetReady(0, 0);
        }

        internal void SetReady(int casualBest, int standardBest)
        {
            UpdateSafeArea();
            ClearMenuSelection();
            scoreText.gameObject.SetActive(false);
            ready.gameObject.SetActive(true);
            ended.gameObject.SetActive(false);
            SetStability(null, string.Empty, false);
        }

        internal void SetPlaying(GameMode gameMode, int score, int best)
        {
            ClearMenuSelection();
            mode = gameMode;
            SetScore(score, best);
            scoreText.gameObject.SetActive(true);
            ready.gameObject.SetActive(false);
            ended.gameObject.SetActive(false);
            stability.anchoredPosition = new Vector2(0f, PlayingStabilityY);
            SetStability(null, string.Empty, false);
        }

        internal void SetEnded(int score, int best, string reason)
        {
            UpdateSafeArea();
            scoreText.gameObject.SetActive(false);
            ready.gameObject.SetActive(false);
            ended.gameObject.SetActive(true);
            stability.anchoredPosition = new Vector2(0f, ResultsStabilityY);
            bool showReason = !stability.gameObject.activeSelf && !string.IsNullOrEmpty(reason);
            reasonText.gameObject.SetActive(showReason);
            reasonText.text = reason;
            resultScoreText.rectTransform.anchoredPosition = new Vector2(0f, showReason ? 169f : 124f);
            resultScoreText.text = score.ToString("D3");
        }

        internal void SetScore(int score, int best)
        {
            UpdateSafeArea();
            scoreText.text = score.ToString("D3");
        }

        internal void SetStability(SupportJoint? joint, string message, bool unstable)
        {
            bool visible = mode == GameMode.Standard && !ready.gameObject.activeSelf &&
                (joint.HasValue || !string.IsNullOrEmpty(message));
            if (stability.gameObject.activeSelf != visible)
                stability.gameObject.SetActive(visible);
            if (!visible)
                return;

            if (stabilityText.text != message)
                stabilityText.text = message;
            Color accent = unstable ? Coral : Amber;
            stabilityText.color = accent;
            loadMarkerFill.color = accent;
            if (stabilityDiagram.gameObject.activeSelf != joint.HasValue)
                stabilityDiagram.gameObject.SetActive(joint.HasValue);
            stabilityText.rectTransform.anchoredPosition = new Vector2(joint.HasValue ? 172f : 0f, 36f);
            stabilityText.alignment = joint.HasValue ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            if (joint.HasValue)
                PlotJoint(joint.Value);
        }

        private void PlotJoint(SupportJoint joint)
        {
            Vector2 contactMin = joint.HasContact ? joint.ContactMin : joint.LoadCenter;
            Vector2 contactMax = joint.HasContact ? joint.ContactMax : joint.LoadCenter;
            Vector2 boundsMin = Vector2.Min(contactMin, joint.LoadCenter);
            Vector2 boundsMax = Vector2.Max(contactMax, joint.LoadCenter);
            Vector2 boundsSize = boundsMax - boundsMin;
            Vector2 plotCenter = (boundsMin + boundsMax) * 0.5f;
            float scale = 78f / Mathf.Max(2f, Mathf.Max(boundsSize.x, boundsSize.y));
            loadMarker.anchoredPosition = (joint.LoadCenter - plotCenter) * scale;

            Vector2 contactSize = contactMax - contactMin;
            Vector2 insetSize = contactSize - Vector2.one * (2f * StackRules.SupportEdgeInset);
            bool hasSafeRegion = joint.HasContact && insetSize.x > 0f && insetSize.y > 0f;
            if (contactRegion.gameObject.activeSelf != joint.HasContact)
                contactRegion.gameObject.SetActive(joint.HasContact);
            if (safeRegion.gameObject.activeSelf != hasSafeRegion)
                safeRegion.gameObject.SetActive(hasSafeRegion);
            if (!joint.HasContact)
                return;

            Vector2 position = ((contactMin + contactMax) * 0.5f - plotCenter) * scale;
            contactRegion.anchoredPosition = position;
            contactRegion.sizeDelta = contactSize * scale;
            if (hasSafeRegion)
            {
                safeRegion.anchoredPosition = position;
                safeRegion.sizeDelta = insetSize * scale;
            }
        }

        private void ClearMenuSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem?.currentSelectedGameObject;
            if (selected != null && (selected.transform.IsChildOf(ready) || selected.transform.IsChildOf(ended)))
                eventSystem.SetSelectedGameObject(null);
        }

        private void UpdateSafeArea()
        {
            Rect bounds = Screen.safeArea;
            if (bounds.width <= 0f || bounds.height <= 0f || Screen.width <= 0 || Screen.height <= 0)
                return;

            var normalizedScale = new Vector2(1f / Screen.width, 1f / Screen.height);
            safeArea.anchorMin = Vector2.Scale(bounds.min, normalizedScale);
            safeArea.anchorMax = Vector2.Scale(bounds.max, normalizedScale);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        private void AddButton(RectTransform parent, string caption, float x, Color background,
            Color textColor, Action onClick)
        {
            var rect = Panel(caption, parent, Bottom, x, 0f, ButtonWidth, ButtonHeight, Color.white, 53, raised: true);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(background, Ink, 0.2f);
            colors.disabledColor = Color.Lerp(background, Ink, 0.6f);
            colors.fadeDuration = 0.10f;
            image.canvasRenderer.SetColor(background);
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());
            NightShiftUiSkin.AddPressMotion(button);

            TextMeshProUGUI captionText = Label("Caption", caption, rect, Top, 0f, 0f, ButtonWidth - 36f, ButtonHeight,
                buttonFontSize, textColor);
            captionText.fontStyle = FontStyles.Bold;
            FitSingleLine(captionText, 34);
        }

        private TextMeshProUGUI Label(string name, string value, Transform parent, Vector2 anchor, float x, float y,
            float width, float height, int size, Color color, bool display = false)
        {
            var rect = CreateRect(name, parent, anchor, x, y, width, height);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = display ? displayFont : font;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.richText = false;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        private static void FitSingleLine(TextMeshProUGUI label, float minimumSize)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Truncate;
            label.fontSizeMin = minimumSize;
            label.fontSizeMax = label.fontSize;
            label.enableAutoSizing = true;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 anchor,
            float x, float y, float width, float height, Color color, int radius = 0, bool raised = false)
        {
            var rect = CreateRect(name, parent, anchor, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (radius > 0)
                NightShiftUiSkin.Apply(image, radius, raised);
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor,
            float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("Night Shift Event System", typeof(EventSystem));
            eventSystem.transform.SetParent(parent, false);
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
