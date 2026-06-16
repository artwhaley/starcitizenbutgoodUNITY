using UnityEngine;
using UnityEngine.UI;

namespace FlightModel
{
    public static class RuntimeDropdownUtility
    {
        static readonly Color PanelColor = new(0.05f, 0.06f, 0.08f, 0.96f);
        static readonly Color ItemColor = new(0.1f, 0.12f, 0.16f, 0.98f);
        static readonly Color HighlightColor = new(0.18f, 0.24f, 0.32f, 1f);
        static readonly Color ControlColor = new(0.12f, 0.16f, 0.21f, 0.98f);
        static readonly Color ControlHoverColor = new(0.18f, 0.24f, 0.32f, 1f);
        static readonly Color ControlPressedColor = new(0.08f, 0.45f, 0.55f, 1f);
        static readonly Color SliderRailColor = new(0.04f, 0.05f, 0.065f, 1f);
        static readonly Color SliderFillColor = new(0.12f, 0.7f, 0.82f, 1f);
        static readonly Color SliderHandleColor = new(0.92f, 0.98f, 1f, 1f);

        public static void EnsureUsable(Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            Image image = dropdown.GetComponent<Image>() ?? dropdown.gameObject.AddComponent<Image>();
            image.color = ControlColor;
            image.raycastTarget = true;
            dropdown.targetGraphic = image;
            dropdown.colors = CreateControlColors();

            if (dropdown.captionText == null)
            {
                dropdown.captionText = CreateText(dropdown.transform as RectTransform, "Caption", TextAnchor.MiddleLeft);
                Stretch(dropdown.captionText.rectTransform, 8f, 4f, 26f, 4f);
            }
            else
            {
                dropdown.captionText.fontSize = 16;
                dropdown.captionText.alignment = TextAnchor.MiddleLeft;
                dropdown.captionText.color = Color.white;
            }

            if (dropdown.template == null || dropdown.itemText == null)
            {
                BuildTemplate(dropdown);
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.fontSize = 16;
                dropdown.itemText.color = Color.white;
            }
        }

        public static void EnsureUsable(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.color = ControlColor;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.colors = CreateControlColors();

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = Mathf.Max(label.fontSize, 16);
                label.color = Color.white;
            }
        }

        public static void EnsureUsable(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.colors = CreateControlColors();

            Image background = slider.GetComponent<Image>() ?? slider.gameObject.AddComponent<Image>();
            background.color = SliderRailColor;
            background.raycastTarget = true;

            if (slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>() ?? slider.fillRect.gameObject.AddComponent<Image>();
                fill.color = SliderFillColor;
                fill.raycastTarget = false;
            }

            if (slider.handleRect != null)
            {
                Image handle = slider.handleRect.GetComponent<Image>() ?? slider.handleRect.gameObject.AddComponent<Image>();
                handle.color = SliderHandleColor;
                handle.raycastTarget = true;
                slider.targetGraphic = handle;
                slider.handleRect.sizeDelta = new Vector2(22f, 34f);
            }
        }

        public static void EnsureUsable(InputField input)
        {
            if (input == null)
            {
                return;
            }

            Image image = input.GetComponent<Image>() ?? input.gameObject.AddComponent<Image>();
            image.color = ControlColor;
            image.raycastTarget = true;
            input.targetGraphic = image;
            input.colors = CreateControlColors();

            if (input.textComponent != null)
            {
                input.textComponent.fontSize = 16;
                input.textComponent.color = Color.white;
                input.textComponent.alignment = TextAnchor.MiddleCenter;
            }

            if (input.placeholder is Text placeholder)
            {
                placeholder.fontSize = 16;
                placeholder.color = new Color(0.65f, 0.7f, 0.78f, 0.85f);
            }
        }

        public static void EnsureReadableToggle(Toggle toggle)
        {
            if (toggle == null)
            {
                return;
            }

            Image image = toggle.GetComponent<Image>() ?? toggle.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.11f, 0.15f, 0.82f);
            image.raycastTarget = true;
            toggle.targetGraphic = image;
            toggle.colors = CreateControlColors();

            Text label = toggle.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = Mathf.Max(label.fontSize, 16);
                label.color = Color.white;
            }
        }

        static void BuildTemplate(Dropdown dropdown)
        {
            Transform oldTemplate = dropdown.transform.Find("Template");
            if (oldTemplate != null)
            {
                Object.Destroy(oldTemplate.gameObject);
            }

            RectTransform root = dropdown.transform as RectTransform;
            GameObject templateGo = new("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform template = templateGo.GetComponent<RectTransform>();
            template.SetParent(root, false);
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -2f);
            template.sizeDelta = new Vector2(0f, 320f);
            templateGo.SetActive(false);
            templateGo.GetComponent<Image>().color = PanelColor;

            GameObject viewportGo = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            viewport.SetParent(template, false);
            Stretch(viewport, 2f, 2f, 2f, 2f);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject itemGo = new("Item", typeof(RectTransform), typeof(Toggle), typeof(Image), typeof(LayoutElement));
            RectTransform item = itemGo.GetComponent<RectTransform>();
            item.SetParent(content, false);
            item.sizeDelta = new Vector2(0f, 40f);
            itemGo.GetComponent<Image>().color = ItemColor;
            itemGo.GetComponent<LayoutElement>().preferredHeight = 40f;

            Toggle toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemGo.GetComponent<Image>();
            toggle.graphic = CreateCheckmark(item);
            ColorBlock colors = toggle.colors;
            colors.normalColor = ItemColor;
            colors.highlightedColor = HighlightColor;
            colors.selectedColor = HighlightColor;
            toggle.colors = colors;

            Text itemText = CreateText(item, "Item Label", TextAnchor.MiddleLeft);
            Stretch(itemText.rectTransform, 28f, 2f, 6f, 2f);

            ScrollRect scroll = templateGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            dropdown.template = template;
            dropdown.itemText = itemText;
        }

        static Graphic CreateCheckmark(RectTransform parent)
        {
            GameObject go = new("Checkmark", typeof(RectTransform), typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = new Vector2(18f, 0f);

            Text text = go.GetComponent<Text>();
            text.text = ">";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.55f, 1f, 0.75f);
            return text;
        }

        static Text CreateText(RectTransform parent, string name, TextAnchor alignment)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        static ColorBlock CreateControlColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = ControlColor;
            colors.highlightedColor = ControlHoverColor;
            colors.selectedColor = ControlHoverColor;
            colors.pressedColor = ControlPressedColor;
            colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            return colors;
        }

        static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
