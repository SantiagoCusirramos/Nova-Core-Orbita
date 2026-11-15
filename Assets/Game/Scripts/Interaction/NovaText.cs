using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    /// <summary>
    /// Crea textos en el mundo (Canvas World Space + Text de uGUI) sin TextMeshPro,
    /// para no depender de los "TMP Essential Resources". Funciona en VR (Single Pass).
    /// Se puede usar en runtime o desde scripts de Editor.
    /// </summary>
    public static class NovaText
    {
        public const float PixelsPerMeter = 1000f;   // 1 px de canvas = 1 mm

        private static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>
        /// Crea un cartel de texto. size = tamaño del cartel en metros. fontSize en mm aprox.
        /// </summary>
        public static Text Create(Transform parent, string name, string text,
                                  Vector3 localPosition, Quaternion localRotation,
                                  Vector2 size, int fontSize, Color color,
                                  TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size * PixelsPerMeter;
            rt.localPosition = localPosition;
            rt.localRotation = localRotation;
            rt.localScale = Vector3.one / PixelsPerMeter;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var t = textGO.GetComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        /// <summary>Cartel que mira hacia +Z local del padre (útil para paneles en paredes).</summary>
        public static Text CreateFacing(Transform parent, string name, string text, Vector3 localPosition,
                                        Vector2 size, int fontSize, Color color)
        {
            // Los Canvas se ven por su cara -Z: rotamos 180° para que se lea mirando el panel
            return Create(parent, name, text, localPosition, Quaternion.Euler(0f, 180f, 0f), size, fontSize, color);
        }

        /// <summary>Barra de texto simple con caracteres ASCII (compatibles con cualquier fuente).</summary>
        public static string Bar(float t01, int cells = 20)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(t01) * cells), 0, cells);
            return "[" + new string('|', n) + new string('.', cells - n) + "]";
        }
    }
}
