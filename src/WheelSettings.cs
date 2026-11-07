using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WheelOfWuck
{
    public class WheelSettings : MonoBehaviour
    {
        static readonly Color Box = Utils.Hex("#FF7800");
        static readonly Color Faded = Utils.Hex("#A66922");
        static readonly Color Grey = Utils.Hex("#575757");

        public static WheelSettings Current { get; private set; }
        public static bool IsOpen => Current != null;

        WheelSet set;
        M_Gamemode mode;
        Action onClose;
        public List<Toggle> toggles = new List<Toggle>();
        public List<(WheelDef wheel, EntryDef entry)> rows = new List<(WheelDef, EntryDef)>();
        bool _built;
        AudioClip tickOn;
        AudioClip tickOff;
        TextMeshProUGUI hint;
        const string HintText = "Tick what can land on each wheel.";

        public static void Open(Transform parent, WheelSet set, M_Gamemode mode, Action onClose)
        {
            if (Current != null) return;

            RectTransform root = WheelUI.Rect("Settings", parent, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Current = root.gameObject.AddComponent<WheelSettings>();
            Current.set = set;
            Current.mode = mode;
            Current.onClose = onClose;
            Current.Build();
            Current._built = true;
        }

        public void Close()
        {
            Destroy(gameObject);
            onClose?.Invoke();
        }

        public void Defaults()
        {
            if (tickOff != null) AudioManager.PlayUISound(tickOff, 0.8f);
            WheelPicks.Clear();
            int n = toggles.Count;
            for (int i = 0; i < n; i++)
            {
                bool def = rows[i].entry.enabled;
                Toggle tg = toggles[i];
                tg.SetIsOnWithoutNotify(def);
                Tint(tg, def);
            }
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Build()
        {
            Vector2 top = new Vector2(0.5f, 1f);
            foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                if (clip.name == "mbutton4") tickOn = clip;
                if (clip.name == "mbutton5") tickOff = clip;
            }

            WheelUI.Fill("Dim", transform, new Color(0f, 0f, 0f, 0.6f)).raycastTarget = true;
            var panel = WheelUI.Panel("Panel", transform, new Vector2(1100f, 920f), Vector2.zero, "WHEEL SETTINGS", Utils.Hex("#606060"), Utils.Hex("#959595"));
            hint = WheelUI.Text("Hint", panel, new Vector2(1000f, 30f), new Vector2(0f, -54f),
                HintText, 18f, TextAlignmentOptions.Center, WheelUI.Dim, top);
            hint.font = Utils.GetFont("Ticketing SDF");

            RectTransform view = WheelUI.Rect("View", panel, new Vector2(1040f, 740f), new Vector2(0f, -90f), top);
            Image viewImage = view.gameObject.AddComponent<Image>();
            viewImage.color = new Color(0f, 0f, 0f, 0.45f);
            view.gameObject.AddComponent<RectMask2D>();

            RectTransform content = WheelUI.Rect("Content", view, Vector2.zero, Vector2.zero, top);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = top;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(16, 16, 6, 10);
            list.spacing = 4f;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            foreach (WheelDef wheel in set.wheels)
            {
                TextMeshProUGUI header = WheelUI.Text("Header", content, new Vector2(0f, 34f), Vector2.zero,
                    wheel.title.ToUpperInvariant(), 24f, TextAlignmentOptions.BottomLeft, WheelUI.Orange);
                header.font = Utils.GetFont("Ticketing SDF GreyGlow");
                header.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
                Image rule = WheelUI.Image("Rule", content, Vector2.zero, Vector2.zero, new Color(0.58f, 0.58f, 0.58f, 0.6f));
                rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

                RectTransform grid = WheelUI.Rect("Grid", content, Vector2.zero, Vector2.zero);
                GridLayoutGroup cells = grid.gameObject.AddComponent<GridLayoutGroup>();
                cells.cellSize = new Vector2(246f, 34f);
                cells.spacing = new Vector2(8f, 2f);
                cells.padding = new RectOffset(0, 0, 4, 2);
                cells.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                cells.constraintCount = 4;
                cells.childAlignment = TextAnchor.UpperLeft;

                foreach (EntryDef entry in wheel.entries) AddRow(grid, wheel, entry);
            }

            Vector2 buttonRow = new Vector2(0f, -920f / 2f + 44f);
            WheelUI.Button("Defaults", panel, new Vector2(170f, 48f), buttonRow + new Vector2(-100f, 0f), "DEFAULTS", Defaults, false);
            WheelUI.Button("Done", panel, new Vector2(170f, 48f), buttonRow + new Vector2(100f, 0f), "DONE", Close);
        }

        void AddRow(Transform parent, WheelDef wheel, EntryDef entry)
        {
            RectTransform cell = WheelUI.Rect("Entry", parent, Vector2.zero, Vector2.zero);
            Image hit = cell.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            Vector2 left = new Vector2(0f, 0.5f);
            Image box = WheelUI.Image("Box", cell, new Vector2(26f, 26f), new Vector2(6f, 0f), Box, Utils.GetSprite("window_outline_02"), left, left);
            box.type = Image.Type.Sliced;
            box.pixelsPerUnitMultiplier = 0.5f;
            Image check = WheelUI.Image("Check", box.transform, new Vector2(30f, 30f), Vector2.zero, WheelUI.Orange, Utils.GetSprite("window_X_02"));

            TextMeshProUGUI label = WheelUI.Text("Label", cell, Vector2.zero, Vector2.zero, entry.text, 20f, TextAlignmentOptions.MidlineLeft, WheelUI.Ink);
            label.font = Utils.GetFont("Ticketing SDF");
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(42f, 0f);
            label.rectTransform.offsetMax = new Vector2(-4f, 0f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 20f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            Toggle toggle = cell.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Utils.Hex("#FF0000");
            colors.pressedColor = Utils.Hex("#C8C8C8");
            colors.selectedColor = Color.white;
            colors.disabledColor = Utils.Hex("#7D7D7D80");
            toggle.colors = colors;
            bool on = WheelPicks.IsEnabled(wheel, entry);
            toggle.SetIsOnWithoutNotify(on);

            string locked = WheelLogic.Locked(entry, mode);
            if (locked != null)
            {
                toggle.interactable = false;
                check.gameObject.SetActive(false);
                WheelUI.Image("Lock", box.transform, new Vector2(28f, 28f), Vector2.zero, Grey, Utils.GetSprite("Icon_Lock"));
                box.color = Grey;
                label.color = Grey;
                label.text = entry.text + "  <size=65%>locked</size>";
            }
            else
            {
                Tint(toggle, on);
                toggle.onValueChanged.AddListener(value =>
                {
                    if (!value && !AnotherOn(wheel, toggle))
                    {
                        toggle.SetIsOnWithoutNotify(true);
                        Tint(toggle, true);
                        if (tickOff != null) AudioManager.PlayUISound(tickOff, 0.8f);
                        Warn("At least one entry has to stay on for " + wheel.title + ".");
                        return;
                    }
                    WheelPicks.Set(wheel, entry, value);
                    Tint(toggle, value);
                    AudioClip clip = value ? tickOn : tickOff;
                    if (clip != null) AudioManager.PlayUISound(clip, 0.8f);
                });
            }

            toggles.Add(toggle);
            rows.Add((wheel, entry));
        }

        public bool AnotherOn(WheelDef wheel, Toggle except)
        {
            int on = 0;
            for (int i = 0; i < toggles.Count; i++)
            {
                if (rows[i].wheel != wheel) continue;
                Toggle t = toggles[i];
                if (t == except) continue;
                if (!t.interactable) continue;
                if (t.isOn) on++;
            }
            return on > 0;
        }

        void Warn(string message)
        {
            if (hint == null) return;
            StopAllCoroutines();
            StartCoroutine(WarnFor(message, 2.5f));
        }

        System.Collections.IEnumerator WarnFor(string message, float seconds)
        {
            hint.text = message;
            hint.color = WheelUI.Red;
            yield return new WaitForSecondsRealtime(seconds);
            if (hint != null)
            {
                hint.text = HintText;
                hint.color = WheelUI.Dim;
            }
        }

        static void Tint(Toggle toggle, bool on)
        {
            if (!toggle.interactable) return;
            TMP_Text label = toggle.GetComponentInChildren<TMP_Text>();
            label.color = on ? WheelUI.Ink : Faded;
            Color box = Box;
            box.a = on ? 1f : 0.55f;
            toggle.targetGraphic.color = box;
        }
    }
}
