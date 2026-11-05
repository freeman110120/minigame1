using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightShift
{
    internal static class NightShiftUiSkin
    {
        private const int TexelsPerUnit = 2;
        private const float ReferencePixelsPerUnit = 100f;
        private static readonly Dictionary<(int, bool), Sprite> Surfaces = new Dictionary<(int, bool), Sprite>();

        internal static void Apply(Image image, int radius, bool raised = false)
        {
            var key = (Mathf.Max(0, radius), raised);
            if (!Surfaces.TryGetValue(key, out Sprite sprite) || !sprite)
            {
                sprite = CreateSurface(key.Item1, raised);
                Surfaces[key] = sprite;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        internal static void AddPressMotion(Button button)
        {
            if (button.targetGraphic is Image image && image.gameObject == button.gameObject &&
                !button.TryGetComponent<NightShiftUiPress>(out _))
                button.gameObject.AddComponent<NightShiftUiPress>();
        }

        private static Sprite CreateSurface(int radius, bool raised)
        {
            const int padding = 1;
            int pixelRadius = radius * TexelsPerUnit;
            int border = pixelRadius + padding + 2;
            int size = border * 2 + 2;
            float half = size * 0.5f;
            float extent = half - padding;
            float innerExtent = extent - pixelRadius;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float height = y + 0.5f - half;
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half) - innerExtent;
                    float dy = Mathf.Abs(height) - innerExtent;
                    float outerX = Mathf.Max(dx, 0f);
                    float outerY = Mathf.Max(dy, 0f);
                    float distance = Mathf.Sqrt(outerX * outerX + outerY * outerY) +
                                     Mathf.Min(Mathf.Max(dx, dy), 0f) - pixelRadius;
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
                    byte tone = raised ? EdgeTone(height, extent, distance) : (byte)255;
                    pixels[y * size + x] = new Color32(tone, tone, tone, alpha);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Night Shift {(raised ? "raised" : "flat")} surface ({radius})",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f,
                ReferencePixelsPerUnit * TexelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.one * border);
            sprite.name = texture.name;
            return sprite;
        }

        private static byte EdgeTone(float height, float extent, float distance)
        {
            float edge = Mathf.Clamp01(1f + distance / 5f);
            float top = Mathf.Clamp01(1f - (extent - height) / 8f);
            float bottom = Mathf.Clamp01(1f - (extent + height) / 8f);
            return (byte)Mathf.RoundToInt(244f + edge * (11f * top - 19f * bottom));
        }
    }

    [DisallowMultipleComponent]
    internal sealed class NightShiftUiPress : BaseMeshEffect, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler
    {
        private const float PressedScale = 0.978f;
        private Button button;
        private int? pressedPointer;
        private float scale = 1f;

        protected override void Awake()
        {
            base.Awake();
            button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !pressedPointer.HasValue &&
                button && button.IsActive() && button.IsInteractable())
                pressedPointer = eventData.pointerId;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && pressedPointer == eventData.pointerId)
                pressedPointer = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (pressedPointer == eventData.pointerId)
                pressedPointer = null;
        }

        protected override void OnDisable()
        {
            pressedPointer = null;
            scale = 1f;
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (pressedPointer.HasValue && (!button || !button.IsActive() || !button.IsInteractable()))
                pressedPointer = null;

            float target = pressedPointer.HasValue ? PressedScale : 1f;
            if (scale == target)
                return;

            scale = Mathf.MoveTowards(scale, target, 0.38f * Time.unscaledDeltaTime);
            if (graphic)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || scale == 1f)
                return;

            // Animate the image mesh while the RectTransform keeps its original hit area.
            Vector3 center = graphic.GetPixelAdjustedRect().center;
            var vertex = new UIVertex();
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                vertex.position = center + (vertex.position - center) * scale;
                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
