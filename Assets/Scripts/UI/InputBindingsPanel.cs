using UnityEngine;
using UnityEngine.UI;

namespace FlightModel
{
    public class InputBindingsPanel : MonoBehaviour
    {
        const float HeaderHeight = 86f;
        const float RowHeight = 238f;

        [SerializeField] GameObject root;
        [SerializeField] AxisBindingRow[] axisRows;
        [SerializeField] FireBindingRow fireRow;
        [SerializeField] Toggle autoSaveToggle;
        [SerializeField] Button refreshButton;
        [SerializeField] Button saveButton;

        JoystickInputProvider provider;
        JoystickProbeMonitor probeMonitor;
        bool layoutBuilt;

        public bool IsVisible => root != null && root.activeSelf;

        public void Initialize(JoystickInputProvider inputProvider)
        {
            provider = inputProvider;
            EnsureLayout();

            for (int i = 0; i < axisRows.Length; i++)
            {
                if (axisRows[i] == null)
                {
                    continue;
                }

                axisRows[i].Initialize((ShipControlAxis)i, provider, OnBindingChanged);
            }

            if (fireRow != null)
            {
                fireRow.Initialize(provider, OnBindingChanged);
            }
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(RefreshDevices);
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(SaveBindings);
            }

            Hide();
        }

        void EnsureLayout()
        {
            if (layoutBuilt || root == null)
            {
                return;
            }

            layoutBuilt = true;

            if (transform is RectTransform canvasRect)
            {
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.offsetMin = Vector2.zero;
                canvasRect.offsetMax = Vector2.zero;
                canvasRect.localScale = Vector3.one;
            }

            if (TryGetComponent(out CanvasScaler scaler))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            RectTransform panelRect = root.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(24f, 24f);
            panelRect.offsetMax = new Vector2(-24f, -24f);

            LayoutHeader(panelRect);
            StyleHeaderControls();

            GameObject viewportGo = new("ScrollViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.SetParent(panelRect, false);
            viewportRect.SetAsFirstSibling();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -HeaderHeight);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);

            GameObject contentGo = new("ScrollContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(12, 12, 12, 20);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panelRect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var probeGo = new GameObject("JoystickProbeMonitor", typeof(JoystickProbeMonitor));
            probeMonitor = probeGo.GetComponent<JoystickProbeMonitor>();
            probeMonitor.Initialize(provider, contentRect);

            RectTransform columnsRect = CreateBindingColumns(contentRect, out RectTransform leftColumn, out RectTransform rightColumn);

            int rowIndex = 0;
            foreach (AxisBindingRow row in axisRows)
            {
                if (row == null)
                {
                    continue;
                }

                ReparentForScroll(row.transform, rowIndex % 2 == 0 ? leftColumn : rightColumn);
                rowIndex++;
            }

            if (fireRow != null)
            {
                ReparentForScroll(fireRow.transform, rowIndex % 2 == 0 ? leftColumn : rightColumn);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(columnsRect);
        }

        static void ReparentForScroll(Transform row, RectTransform content)
        {
            RectTransform rowRect = row as RectTransform;
            row.SetParent(content, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = Vector2.zero;
            rowRect.sizeDelta = new Vector2(0f, RowHeight);
            Image background = row.gameObject.GetComponent<Image>() ?? row.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.075f, 0.1f, 0.92f);
            background.raycastTarget = true;

            LayoutElement layoutElement = row.gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = row.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = RowHeight;
            layoutElement.preferredHeight = RowHeight;
            layoutElement.flexibleWidth = 1f;
        }

        static RectTransform CreateBindingColumns(RectTransform content, out RectTransform leftColumn, out RectTransform rightColumn)
        {
            GameObject columnsGo = new("BindingColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            RectTransform columnsRect = columnsGo.GetComponent<RectTransform>();
            columnsRect.SetParent(content, false);
            columnsRect.anchorMin = new Vector2(0f, 1f);
            columnsRect.anchorMax = new Vector2(1f, 1f);
            columnsRect.pivot = new Vector2(0.5f, 1f);
            columnsRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup columnsLayout = columnsGo.GetComponent<HorizontalLayoutGroup>();
            columnsLayout.spacing = 18f;
            columnsLayout.childAlignment = TextAnchor.UpperLeft;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = false;

            LayoutElement columnsElement = columnsGo.GetComponent<LayoutElement>();
            columnsElement.flexibleWidth = 1f;

            leftColumn = CreateBindingColumn(columnsRect, "LeftColumn");
            rightColumn = CreateBindingColumn(columnsRect, "RightColumn");
            return columnsRect;
        }

        static RectTransform CreateBindingColumn(RectTransform parent, string name)
        {
            GameObject columnGo = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            RectTransform columnRect = columnGo.GetComponent<RectTransform>();
            columnRect.SetParent(parent, false);
            columnRect.anchorMin = new Vector2(0f, 1f);
            columnRect.anchorMax = new Vector2(1f, 1f);
            columnRect.pivot = new Vector2(0.5f, 1f);
            columnRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup columnLayout = columnGo.GetComponent<VerticalLayoutGroup>();
            columnLayout.spacing = 14f;
            columnLayout.childAlignment = TextAnchor.UpperLeft;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = columnGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement element = columnGo.GetComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            return columnRect;
        }

        static void LayoutHeader(RectTransform panelRect)
        {
            RectTransform title = panelRect.Find("Title") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.anchoredPosition = new Vector2(0f, -8f);
                title.sizeDelta = new Vector2(-16f, 34f);

                Text titleText = title.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.fontSize = 24;
                }
            }

            LayoutTopButton(panelRect.Find("RefreshButton") as RectTransform, 10f);
            LayoutTopButton(panelRect.Find("SaveButton") as RectTransform, 220f);
            LayoutTopToggle(panelRect.Find("AutoSaveToggle") as RectTransform, 430f);
        }

        static void LayoutTopButton(RectTransform rect, float x)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -46f);
            rect.sizeDelta = new Vector2(190f, 40f);

            Text label = rect.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 16;
            }
        }

        static void LayoutTopToggle(RectTransform rect, float x)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -48f);
            rect.sizeDelta = new Vector2(260f, 38f);
        }

        void StyleHeaderControls()
        {
            RuntimeDropdownUtility.EnsureUsable(refreshButton);
            RuntimeDropdownUtility.EnsureUsable(saveButton);
            RuntimeDropdownUtility.EnsureReadableToggle(autoSaveToggle);
        }

        public void Show()
        {
            provider.RefreshDevices();
            probeMonitor?.RefreshDeviceList();
            RefreshRows();
            if (root != null)
            {
                RepairDropdowns(root);
                root.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            probeMonitor?.RefreshLiveReadout();

            foreach (AxisBindingRow row in axisRows)
            {
                if (row != null)
                {
                    row.RefreshLiveReadout();
                }
            }

            fireRow?.RefreshLiveReadout();
        }

        void RefreshDevices()
        {
            provider.RefreshDevices();
            probeMonitor?.RefreshDeviceList();
            RefreshRows();
        }

        void RefreshRows()
        {
            foreach (AxisBindingRow row in axisRows)
            {
                if (row != null)
                {
                    row.RefreshDeviceList();
                }
            }

            fireRow?.RefreshDeviceList();
        }

        void OnBindingChanged()
        {
            if (autoSaveToggle != null && autoSaveToggle.isOn)
            {
                SaveBindings();
            }
        }

        void SaveBindings() => provider.SaveBindings();

        static void RepairDropdowns(GameObject panelRoot)
        {
            foreach (Dropdown dropdown in panelRoot.GetComponentsInChildren<Dropdown>(true))
            {
                RuntimeDropdownUtility.EnsureUsable(dropdown);
            }

            foreach (Button button in panelRoot.GetComponentsInChildren<Button>(true))
            {
                RuntimeDropdownUtility.EnsureUsable(button);
            }
        }
    }
}
