using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Chris.PachiRogue.UI
{
    /// <summary>
    /// Builds the story-intro uGUI hierarchy in code so the Boot scene needs
    /// no hand-wired canvas (keeps the scene YAML trivial and avoids
    /// editor-only setup). Uses the legacy Text/InputField components with the
    /// built-in LegacyRuntime font as a Phase-1-era placeholder — the real
    /// HUD arrives with TMP in Phase 4 (see NOTES.md).
    /// </summary>
    internal static class RuntimeUiFactory
    {
        // Pastel night palette, per GAME_DESIGN.md art direction.
        private static readonly Color Background = new Color(0.10f, 0.09f, 0.16f, 1f);
        private static readonly Color PanelColor = new Color(0.16f, 0.14f, 0.25f, 0.9f);
        private static readonly Color TextColor = new Color(0.95f, 0.93f, 1.00f, 1f);
        private static readonly Color AccentColor = new Color(1.00f, 0.72f, 0.42f, 1f);
        private static readonly Color ButtonText = new Color(0.12f, 0.08f, 0.10f, 1f);

        public static Font LoadDefaultFont()
        {
            // Arial.ttf was removed as a built-in; LegacyRuntime.ttf is the
            // Unity 6 name (verified in Phase 0, see NOTES.md).
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Canvas CreateCanvas(Transform parent, bool withBackground = true)
        {
            var go = new GameObject("RuntimeCanvas");
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            if (withBackground)
            {
                CreateFullScreenImage(canvas.transform, "Background", Background);
            }

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem (Story)");
            go.AddComponent<EventSystem>();
            // #if guard: pick the input module matching the project's active
            // input backend; mirrors PLAN.md Phase 5's rule that every #if
            // carries a plan reference.
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static Image CreateFullScreenImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(Transform parent, string name, Font font, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, Font font,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, out Text label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.AddComponent<Image>();
            image.color = AccentColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            label = CreateText(go.transform, "Label", font, 44,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.color = ButtonText;
            return button;
        }

        public static InputField CreateInputField(Transform parent, string name, Font font,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, out Text placeholder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.AddComponent<Image>();
            image.color = PanelColor;

            Text textComponent = CreateText(go.transform, "Text", font, 48,
                Vector2.zero, Vector2.one, new Vector2(30f, 10f), new Vector2(-30f, -10f));
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.supportRichText = false;

            placeholder = CreateText(go.transform, "Placeholder", font, 48,
                Vector2.zero, Vector2.one, new Vector2(30f, 10f), new Vector2(-30f, -10f));
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.4f);

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = textComponent;
            input.placeholder = placeholder;
            input.characterLimit = StoryIntroModel.MaxNameLength;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }
    }
}
