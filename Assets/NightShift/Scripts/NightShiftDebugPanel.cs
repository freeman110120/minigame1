#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightShift
{
    [DefaultExecutionOrder(-100)]
    internal sealed class NightShiftDebugPanel : MonoBehaviour
    {
        private const int MaxFloor = 80;
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Color Ink = new Color32(13, 20, 30, 255);
        private static readonly Color Slate = new Color32(43, 57, 73, 255);
        private static readonly Color Ivory = new Color32(247, 243, 233, 255);
        private static readonly Color Muted = new Color32(182, 196, 210, 255);
        private static readonly Color Amber = new Color32(255, 190, 90, 255);

        private CraneGame game;
        private TMP_FontAsset font;
        private bool chinese;
        private GameObject overlay;
        private RectTransform safeArea;
        private RectTransform modalSafeArea;
        private RectTransform trigger;
        private RectTransform modal;
        private Rect safeBounds;
        private Vector2Int screenSize;
        private Slider floorSlider;
        private TextMeshProUGUI currentFloor;
        private TextMeshProUGUI targetFloor;
        private TextMeshProUGUI freeze;

        internal bool IsOpen => modal != null && modal.gameObject.activeSelf;

        internal void Initialize(CraneGame owner, TMP_FontAsset chineseFont)
        {
            game = owner;
            chinese = chineseFont != null;
            font = chinese ? chineseFont : TMP_Settings.defaultFontAsset;

            overlay = new GameObject("Night Shift Debug Overlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (overlay.scene != gameObject.scene)
                SceneManager.MoveGameObjectToScene(overlay, gameObject.scene);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            safeArea = Stretch("Safe Area", overlay.transform, Vector2.zero, Vector2.zero);
            var triggerLabel = Button(safeArea, "DEV", Vector2.one, -20f, -42f, 132f, 116f,
                () => SetOpen(true), Slate, Ivory, radius: 32, visualHeight: 64f);
            trigger = (RectTransform)triggerLabel.transform.parent;

            modal = Stretch("Debug Modal", overlay.transform, Vector2.zero, Vector2.zero);
            var dimmer = modal.gameObject.AddComponent<Image>();
            dimmer.color = new Color(0f, 0f, 0f, 0.64f);
            modalSafeArea = Stretch("Modal Safe Area", modal, Vector2.zero, Vector2.zero);
            var panel = MakeRect("Debug Panel", modalSafeArea, Center, 0f, 0f, 744f, 638f);
            Fill(panel, Ink, 40, raised: true);
            Fill(MakeRect("Accent", panel, Top, -300f, -16f, 64f, 6f), Amber, 3);

            Label(panel, Local("调试面板", "DEBUG OPTIONS"), Top, -89f, -29f, 486f, 66f, 46, Amber,
                TextAlignmentOptions.Left);
            Button(panel, Local("关闭", "CLOSE"), Top, 268f, -29f, 136f, 66f,
                () => SetOpen(false), Slate, Ivory);

            currentFloor = Label(panel, string.Empty, Top, -172f, -110f, 320f, 54f, 32, Muted,
                TextAlignmentOptions.Left);
            targetFloor = Label(panel, string.Empty, Top, 172f, -110f, 320f, 54f, 32, Ivory,
                TextAlignmentOptions.Right);
            floorSlider = CreateFloorSlider(panel);
            floorSlider.onValueChanged.AddListener(value => ShowTargetFloor(Mathf.RoundToInt(value)));
            Button(panel, Local("城市 0", "CITY 0"), Top, -226f, -250f, 212f, 78f,
                () => JumpTo(0), Slate, Ivory);
            Button(panel, Local("云层 14", "CLOUD 14"), Top, 0f, -250f, 212f, 78f,
                () => JumpTo(14), Slate, Ivory);
            Button(panel, Local("云上 32", "ABOVE 32"), Top, 226f, -250f, 212f, 78f,
                () => JumpTo(32), Slate, Ivory);
            Button(panel, Local("跳转到目标楼层", "JUMP TO TARGET"), Top, 0f, -346f, 664f, 80f,
                () => JumpTo(Mathf.RoundToInt(floorSlider.value)), Amber, Ink, 26);

            freeze = Button(panel, Local("暂停摆动", "FREEZE SWAY"), Top, -170f, -444f, 324f, 78f,
                ToggleFreeze, Slate, Ivory);
            Button(panel, Local("完美落板", "PERFECT DROP"), Top, 170f, -444f, 324f, 78f,
                () => Run(game.DebugPerfectDrop), Slate, Ivory);
            Button(panel, Local("测试倒塌", "TEST COLLAPSE"), Top, -170f, -538f, 324f, 78f,
                () => Run(game.DebugTestCollapse), Slate, Ivory);
            Button(panel, Local("重开本局", "RESET RUN"), Top, 170f, -538f, 324f, 78f,
                () => Run(game.DebugResetRun), Slate, Ivory);

            modal.gameObject.SetActive(false);
#if UNITY_EDITOR
            trigger.gameObject.SetActive(false);
#endif
            UpdateSafeArea();
        }

        internal bool ContainsTrigger(Vector2 screenPosition) =>
            trigger != null && trigger.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(trigger, screenPosition);

        private void Update()
        {
            if (modal == null)
                return;

            UpdateSafeArea();
            if (Input.GetKeyDown(KeyCode.F1))
                SetOpen(!IsOpen);
            else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                SetOpen(false);
        }

        private void OnDestroy()
        {
            if (overlay != null)
                Destroy(overlay);
        }

        private void SetOpen(bool open)
        {
            EventSystem.current?.SetSelectedGameObject(null);
#if !UNITY_EDITOR
            trigger.gameObject.SetActive(!open);
#endif
            modal.gameObject.SetActive(open);
            if (!open)
                return;

            currentFloor.text = $"{Local("当前楼层", "CURRENT")} {game.DebugFloor}";
            floorSlider.SetValueWithoutNotify(game.DebugFloor);
            ShowTargetFloor(Mathf.RoundToInt(floorSlider.value));
            ShowFreeze();
        }

        private void JumpTo(int floor)
        {
            SetOpen(false);
            game.DebugJumpTo(floor);
        }

        private void Run(Action action)
        {
            SetOpen(false);
            action();
        }

        private void ToggleFreeze()
        {
            game.DebugSetFrozen(!game.DebugFrozen);
            ShowFreeze();
        }

        private void ShowFreeze() => freeze.text = game.DebugFrozen
            ? Local("恢复摆动", "RESUME SWAY")
            : Local("暂停摆动", "FREEZE SWAY");

        private void ShowTargetFloor(int floor) =>
            targetFloor.text = $"{Local("目标楼层", "TARGET")} {floor} / {MaxFloor}";

        private Slider CreateFloorSlider(Transform parent)
        {
            const float width = 664f;
            const float height = 70f;
            const float trackInset = 26f;
            const float handleSize = 48f;
            const float handleInset = (height - handleSize) * 0.5f;
            var rect = MakeRect("Floor Slider", parent, Top, 0f, -165f, width, height);
            Fill(rect, Color.clear).raycastTarget = true;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.maxValue = MaxFloor;
            slider.wholeNumbers = true;
            var track = MakeRect("Track", rect, Center, 0f, 0f, width - 2f * trackInset, 16f);
            Fill(track, Slate, 8);
            var progress = Stretch("Progress", track, Vector2.zero, Vector2.zero);
            Fill(progress, Amber, 8);
            slider.fillRect = progress;

            var inset = new Vector2(trackInset, handleInset);
            var handleArea = Stretch("Handle Area", rect, inset, -inset);
            var handle = MakeRect("Handle", handleArea, Center, 0f, 0f, handleSize, 0f);
            slider.targetGraphic = Fill(handle, Ivory, 24, raised: true);
            slider.handleRect = handle;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        private void UpdateSafeArea()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect bounds = Screen.safeArea;
            if (size.x <= 0 || size.y <= 0 || bounds.width <= 0f || bounds.height <= 0f ||
                (size == screenSize && bounds == safeBounds))
                return;

            screenSize = size;
            safeBounds = bounds;
            var scale = new Vector2(1f / size.x, 1f / size.y);
            modalSafeArea.anchorMin = safeArea.anchorMin = Vector2.Scale(bounds.min, scale);
            modalSafeArea.anchorMax = safeArea.anchorMax = Vector2.Scale(bounds.max, scale);
        }

        private TextMeshProUGUI Button(Transform parent, string caption, Vector2 anchor, float x, float y, float width,
            float height, Action click, Color background, Color foreground, int radius = 22,
            float visualHeight = 0f)
        {
            var rect = MakeRect(caption, parent, anchor, x, y, width, height);
            Image image;
            if (visualHeight > 0f)
            {
                // Keep the full touch target around the compact badge.
                var hitArea = Fill(rect, Color.clear);
                hitArea.raycastTarget = true;
                hitArea.canvasRenderer.cullTransparentMesh = false;
                image = Fill(MakeRect("Badge", rect, Center, 0f, 0f, width, visualHeight), background, radius,
                    raised: true);
            }
            else
            {
                image = Fill(rect, background, radius, raised: true);
                image.raycastTarget = true;
            }

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => click());
            NightShiftUiSkin.AddPressMotion(button);
            return Label(rect, caption, Center, 0f, 0f, width - 12f, height, 32, foreground);
        }

        private TextMeshProUGUI Label(Transform parent, string caption, Vector2 anchor, float x, float y, float width,
            float height, int size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = MakeRect("Label", parent, anchor, x, y, width, height).gameObject
                .AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.richText = false;
            text.text = caption;
            return text;
        }

        private string Local(string zh, string en) => chinese ? zh : en;

        private static Image Fill(RectTransform rect, Color color, int radius = 0, bool raised = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (radius > 0)
                NightShiftUiSkin.Apply(image, radius, raised);
            return image;
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, float x,
            float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = MakeRect(name, parent, Vector2.zero, 0f, 0f, 0f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
            return rect;
        }
    }
}
#endif
