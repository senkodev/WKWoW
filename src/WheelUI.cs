using System;
using System.Collections;
using System.Collections.Generic;
using Febucci.UI;
using Febucci.UI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WheelOfWuck
{
    public class WheelUI : MonoBehaviour
    {
        const float Radius = 290f;
        static readonly Vector2 WheelCentre = new Vector2(-290f, -50f);
        static readonly Vector2 PanelCentre = new Vector2(335f, -50f);
        static readonly Vector2 PanelSize = new Vector2(440f, 560f);
        static readonly Vector2 Middle = new Vector2(0.5f, 0.5f);

        public static readonly Color Ink = Color.white;
        public static readonly Color Frame = Utils.Hex("#A1A1A1");
        public static readonly Color Orange = Utils.Hex("#FF9D00");
        public static readonly Color Red = Utils.Hex("#FF2F39");
        public static readonly Color Magenta = Utils.Hex("#B43EA8");
        public static readonly Color Dim = Utils.Hex("#9F9F9F");

        public static float SpinSeconds = 6f;
        public static int SpinTurns = 5;

        public static WheelUI Current { get; private set; }
        public static bool IsOpen { get { return Current != null; } }

        WheelSet set;
        M_Gamemode mode;
        Func<Roll, List<string>> apply;

        RectTransform wheelGroup;
        RectTransform wheel;
        RectTransform pointer;
        Image wheelImage;
        TextMeshProUGUI resultsText;
        TextMeshProUGUI hint;
        GameObject winnerPanel;
        TextAnimator_TMP winnerText;
        Button actionButton;
        Button resetButton;
        Button settingsButton;
        Button cancelButton;
        Button lastButton;
        GameObject selectedBefore;

        AudioClip tickClip;
        AudioClip winClip;
        float[] starts;
        float[] spans;
        Color[] sliceColors;
        Sprite highlight;
        int lastSlice;
        float pointerKick;
        float pointerReturn = 160f;
        float lastDegrees;
        float lastDegreesTime;
        float wheelSpeed;
        int spinCount;
        bool actionPressed;
        bool resetPressed;
        bool useLast;
        bool previewLast;
        bool wasReset;
        List<WheelResult> results = new List<WheelResult>();

        public static void Open(WheelSet set, M_Gamemode mode, Func<Roll, List<string>> apply)
        {
            if (Current != null) return;

            GameObject root = new GameObject("WheelOfWuck_Overlay");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            Current = root.AddComponent<WheelUI>();
            Current.set = set;
            Current.mode = mode;
            Current.apply = apply;
        }

        public void Press()
        {
            if (actionButton.interactable) actionPressed = true;
        }

        public void Reset()
        {
            if (!resetButton.interactable) return;
            resetPressed = true;
            actionPressed = true;
        }

        void Start()
        {
            Build();
            selectedBefore = EventSystem.current.currentSelectedGameObject;
            foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                if (clip.name == "click_01") tickClip = clip;
                if (clip.name == "mbutton1") winClip = clip;
            }
            this.StartCoroutine(Run());
        }

        void OnDestroy()
        {
            ClearWheel();
            EventSystem.current?.SetSelectedGameObject(selectedBefore);
            if (Current == this) Current = null;
        }

        public void Back()
        {
            if (WheelSettings.IsOpen) WheelSettings.Current.Close();
            else Cancel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
            if (EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
            pointerKick = Mathf.MoveTowards(pointerKick, 0f, Time.unscaledDeltaTime * pointerReturn);
            pointer.localRotation = Quaternion.Euler(0f, 0f, pointerKick);
        }

        void Build()
        {
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 bottom = new Vector2(0.5f, 0f);
            int pad = 12;

            Fill("Dim", transform, new Color(0.02f, 0.02f, 0.03f, 0.92f)).raycastTarget = true;
            var title = Shaky(Text("Title", transform, new Vector2(1200f, 80f), new Vector2(0f, -10f), "", 56f,
                TextAlignmentOptions.Center, Ink, top));
            title.SetText(set.title.ToUpperInvariant());

            wheelGroup = Rect("WheelGroup", transform, Vector2.zero, Vector2.zero);
            wheel = Rect("Wheel", wheelGroup, new Vector2(Radius * 2f, Radius * 2f), WheelCentre);
            wheelImage = wheel.gameObject.AddComponent<Image>();
            wheelImage.raycastTarget = false;
            pointer = Image("Pointer", wheelGroup, new Vector2(60f, 42f), WheelCentre + new Vector2(Radius + 52f, 0f),
                Color.white, PointerSprite(120, 84, Color.white, Color.black, 5f), null, new Vector2(1f, 0.5f)).rectTransform;
            lastButton = Button("Last", wheelGroup, new Vector2(240f, 48f), WheelCentre + new Vector2(0f, -Radius - 44f), "LAST ROLL", () =>
            {
                useLast = true;
                actionPressed = true;
            }, false);

            Transform panel = Panel("Results", transform, PanelSize, PanelCentre, "THE RUN", Magenta, Magenta);
            resultsText = Text("ResultsText", panel, new Vector2(PanelSize.x - 44f, PanelSize.y - 150f), new Vector2(0f, -62f), "", 22f,
                TextAlignmentOptions.TopLeft, Ink, top);
            resultsText.font = Utils.GetFont("Ticketing SDF");
            hint = Text("Hint", panel, new Vector2(PanelSize.x - 44f, 60f), new Vector2(0f, 16f), "", 18f, TextAlignmentOptions.Bottom, Dim, bottom);
            hint.font = Utils.GetFont("Ticketing SDF");

            RectTransform row = Rect("Buttons", transform, new Vector2(PanelSize.x, 50f),
                new Vector2(PanelCentre.x, PanelCentre.y - PanelSize.y / 2f - 40f));
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            actionButton = Button("Action", row, new Vector2(136f, 50f), Vector2.zero, "SPIN", Press);
            resetButton = Button("Reset", row, new Vector2(136f, 50f), Vector2.zero, "RESET", Reset, false);
            settingsButton = Button("Settings", row, new Vector2(136f, 50f), Vector2.zero, "SETTINGS", OpenSettings, false);
            cancelButton = Button("Cancel", row, new Vector2(136f, 50f), Vector2.zero, "CANCEL", Cancel, false);

            RectTransform winner = Rect("Winner", transform, new Vector2(520f, 160f), WheelCentre);
            Panel("Frame", winner, new Vector2(520f, 160f), Vector2.zero, "THE WHEEL SAYS", Red, Red);
            winnerPanel = winner.gameObject;
            winnerText = Shaky(Text("WinnerText", winner, new Vector2(500f, 90f), new Vector2(0f, 12f), "", 40f, TextAlignmentOptions.Center, Ink, bottom));
            winnerPanel.SetActive(false);
        }

        static TextAnimator_TMP Shaky(TextMeshProUGUI text)
        {
            var animator = text.gameObject.AddComponent<TextAnimator_TMP>();
            animator.DefaultBehaviorsTags = new string[] { "shake" };
            animator.DefaultAppearancesTags = new string[0];
            animator.defaultTagsMode = TAnimCore.DefaultTagsMode.Constant;
            animator.useDynamicScaling = true;
            animator.referenceFontSize = 250f;
            return animator;
        }

        void OpenSettings()
        {
            if (WheelSettings.IsOpen) return;
            WheelSettings.Open(transform, set, mode, () =>
            {
                resetPressed = true;
                actionPressed = true;
            });
        }

        IEnumerator Run()
        {
            while (true)
            {
                resetPressed = false;
                useLast = false;
                WheelLogic.wins.Clear();
                results = new List<WheelResult>();
                previewLast = Roll.Current != null && !wasReset;
                lastButton.gameObject.SetActive(Roll.Current != null);
                resetButton.gameObject.SetActive(false);
                settingsButton.gameObject.SetActive(true);
                wheelGroup.gameObject.SetActive(true);
                cancelButton.gameObject.SetActive(true);
                hint.text = "";

                foreach (WheelDef wheelDef in set.wheels)
                {
                    int spins = WheelLogic.SpinsFor(wheelDef);
                    var result = new WheelResult { id = wheelDef.id, title = wheelDef.title };
                    for (int spin = 0; spin < spins; spin++)
                    {
                        List<EntryDef> live = WheelLogic.Live(wheelDef, mode);
                        if (live.Count == 0)
                        {
                            Plugin.Logger.LogWarning($"nothing left to spin on the {wheelDef.title} wheel");
                            break;
                        }
                        if (live.Count == 1)
                        {
                            EntryDef only = live[0];
                            //Debug.Log("auto pick " + only.text);
                            Plugin.Logger.LogInfo("only " + only.text + " can land on the " + wheelDef.title + " wheel, no spin needed");
                            WheelLogic.wins.Add((wheelDef, only));
                            result.winners.Add(only.text);
                            if (!results.Contains(result)) results.Add(result);
                            RefreshResults(wheelDef);
                            continue;
                        }
                        EntryDef winner = WheelLogic.Pick(live);
                        //Debug.Log("picked " + winner.text);
                        ShowWheel(live);
                        RefreshResults(wheelDef);
                        yield return WaitForAction("SPIN");
                        if (useLast || resetPressed) break;

                        previewLast = false;
                        lastButton.gameObject.SetActive(false);
                        settingsButton.gameObject.SetActive(false);
                        RefreshResults(wheelDef);
                        resetButton.interactable = false;
                        yield return Spin(live.IndexOf(winner));

                        WheelLogic.wins.Add((wheelDef, winner));
                        result.winners.Add(winner.text);
                        if (!results.Contains(result)) results.Add(result);
                        RefreshResults(wheelDef);
                        resetButton.gameObject.SetActive(true);
                        winnerPanel.SetActive(true);
                        winnerText.SetText(winner.text);
                        yield return PopIn(winnerPanel.transform);
                        AudioManager.PlayUISound(winClip, 0.8f);
                        yield return WaitForAction("NEXT");
                        winnerPanel.SetActive(false);
                        if (resetPressed) break;
                    }
                    if (useLast || resetPressed) break;
                }
                if (resetPressed)
                {
                    wasReset = true;
                    continue;
                }

                ClearWheel();
                wheelGroup.gameObject.SetActive(false);
                cancelButton.gameObject.SetActive(false);
                settingsButton.gameObject.SetActive(false);
                Roll roll = useLast ? Roll.Current : WheelLogic.BuildRoll(results);
                results = roll.results;
                previewLast = false;
                List<string> notes = apply(roll);
                notes.InsertRange(0, roll.notes);
                RefreshResults(null, notes);
                hint.text = "Everything is picked. Press Play when you're ready, or Reset to spin again.";
                resetButton.gameObject.SetActive(true);
                yield return WaitForAction("DONE");
                if (!resetPressed) break;
            }
            Destroy(gameObject);
        }

        void Cancel()
        {
            Plugin.Logger.LogInfo("wheel cancelled, nothing changed");
            Destroy(gameObject);
        }

        IEnumerator WaitForAction(string label)
        {
            actionPressed = false;
            actionButton.interactable = true;
            resetButton.interactable = true;
            SetLabel(actionButton, label);
            while (!actionPressed) yield return null;
            actionPressed = false;
        }

        void RefreshResults(WheelDef current, List<string> notes = null)
        {
            var lines = new List<string>();
            foreach (WheelDef wheelDef in set.wheels)
            {
                WheelResult result = results.Find(r => r.id == wheelDef.id);
                WheelResult last = null;
                if (previewLast) last = Roll.Current.results.Find(r => r.id == wheelDef.id);

                if (wheelDef == current)
                    lines.Add($"<color=#FF9D00>{wheelDef.title}:</color> {Winners(result)}");
                else if (result != null)
                    lines.Add($"<color=#AAAAAA>{wheelDef.title}:</color> {Winners(result)}");
                else if (last != null)
                    lines.Add($"<color=#555555>{wheelDef.title}: {Winners(last)}</color>");
                else
                    lines.Add($"<color=#555555>{wheelDef.title}</color>");
            }
            if (notes != null)
            {
                foreach (string note in notes) lines.Add($"<color=#FF9D00>{note}</color>");
            }
            resultsText.text = string.Join("\n", lines);
        }

        static string Winners(WheelResult result)
        {
            if (result == null) { return ""; }
            string s = "";
            for (int i = 0; i < result.winners.Count; i++)
            {
                if (i != 0) s += ", ";
                s += result.winners[i];
            }
            return s;
        }

        void ShowWheel(List<EntryDef> live)
        {
            ClearWheel();
            float total = 0f;
            for (int i = 0; i < live.Count; i++)
            {
                total = total + live[i].weight;
            }
            starts = new float[live.Count];
            spans = new float[live.Count];
            for (int i = 0; i < live.Count; i++)
            {
                starts[i] = i == 0 ? 0f : starts[i - 1] + spans[i - 1];
                spans[i] = 360f * live[i].weight / total;
            }
            spans[live.Count - 1] = 360f - starts[live.Count - 1];

            List<Color> colors = set.colors.ConvertAll(Utils.Hex);
            sliceColors = new Color[live.Count];
            for (int i = 0; i < live.Count; i++) sliceColors[i] = colors[i % colors.Count];
            int last = live.Count - 1;
            if (last > 1 && colors.Count > 2 && sliceColors[last] == sliceColors[0]) sliceColors[last] = colors[1];

            wheelImage.sprite = WheelSprite(1024, starts, spans, sliceColors);
            wheelImage.color = Color.white;
            wheel.localRotation = Quaternion.identity;
            lastSlice = -1;

            for (int i = 0; i < live.Count; i++)
            {
                float mid = starts[i] + spans[i] / 2f;
                float height = Mathf.Clamp(1.2f * Radius * Mathf.Sin(spans[i] * Mathf.Deg2Rad / 2f), 16f, 64f);
                Vector2 along = new Vector2(Mathf.Cos(mid * Mathf.Deg2Rad), Mathf.Sin(mid * Mathf.Deg2Rad));
                TextMeshProUGUI label = Text("Label", wheel, new Vector2(Radius * 0.74f, height), along * (Radius * 0.94f),
                    live[i].text, 24f, TextAlignmentOptions.MidlineRight, Color.white, null, new Vector2(1f, 0.5f));
                label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, mid);
                label.enableAutoSizing = true;
                label.fontSizeMin = 11f;
                label.fontSizeMax = Mathf.Min(26f, height * 0.8f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.fontStyle = FontStyles.Bold;
                label.outlineColor = Color.black;
                label.outlineWidth = 0.25f;
            }
        }

        void ClearWheel()
        {
            foreach (Transform label in wheel)
            {
                Destroy(label.GetComponent<TMP_Text>().fontMaterial);
                Destroy(label.gameObject);
            }
            if (highlight != null) DestroySprite(highlight);
            highlight = null;
            if (wheelImage.sprite != null) DestroySprite(wheelImage.sprite);
            wheelImage.sprite = null;
            wheelImage.color = Color.clear;
        }

        IEnumerator Spin(int winner)
        {
            actionButton.interactable = false;
            spinCount++;
            Color[] dimmed = (Color[])sliceColors.Clone();
            for (int i = 0; i < dimmed.Length; i++)
            {
                if (i != winner) dimmed[i] = Color.Lerp(dimmed[i], Color.black, 0.6f);
            }
            highlight = WheelSprite(1024, starts, spans, dimmed);

            float landing = starts[winner] + spans[winner] * UnityEngine.Random.Range(0.15f, 0.85f);
            float start = wheel.localEulerAngles.z;
            int turns = Mathf.Max(2, SpinTurns + UnityEngine.Random.Range(-1, 2));
            float delta = turns * 360f + Mathf.Repeat(start + landing, 360f);
            float duration = SpinSeconds;
            float pullBack = Mathf.Min(14f, spans[winner] * 0.4f);
            float pullTime = Mathf.Min(0.3f, duration * 0.05f);
            float[] travel = SpinProfile(512);
            lastDegrees = start;
            lastDegreesTime = Time.unscaledTime;

            for (float elapsed = 0f; elapsed < pullTime; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / pullTime;
                float back = 1f - t;
                SetRotation(start + pullBack * (1f - back * back));
                yield return null;
            }
            float spinFrom = start + pullBack;
            float spinDelta = delta + pullBack;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                SetRotation(spinFrom - spinDelta * Sample(travel, elapsed / duration));
                yield return null;
            }
            float settle = Mathf.Min(1.5f, spans[winner] * 0.08f);
            for (float elapsed = 0f; elapsed < 0.35f; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / 0.35f;
                SetRotation(start - delta - settle * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            SetRotation(start - delta);
            wheelSpeed = 0f;

            DestroySprite(wheelImage.sprite);
            wheelImage.sprite = highlight;
            highlight = null;
            for (int i = 0; i < wheel.childCount; i++)
            {
                if (i != winner) wheel.GetChild(i).GetComponent<TMP_Text>().color = new Color(0.55f, 0.55f, 0.55f);
            }
            yield return new WaitForSecondsRealtime(0.6f);
        }

        const float launchPart = 0.06f;
        const float friction = 1.45f;

        static float[] SpinProfile(int samples)
        {
            float[] distance = new float[samples + 1];
            float sum = 0f;
            distance[0] = 0f;
            for (int i = 1; i <= samples; i++)
            {
                float p = (float)i / (float)samples;
                float speed = 0f;
                if (p < launchPart)
                {
                    speed = Mathf.Sin(p / launchPart * Mathf.PI * 0.5f);
                }
                else
                {
                    float u = (p - launchPart) / (1f - launchPart);
                    speed = Mathf.Pow(1f - u, friction);
                }
                sum = sum + speed;
                distance[i] = sum;
            }
            for (int i = 0; i <= samples; i++)
            {
                distance[i] = distance[i] / sum;
            }
            distance[samples] = 1f;
            return distance;
        }

        static float Sample(float[] table, float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float x = t * (table.Length - 1);
            int i = Mathf.FloorToInt(x);
            float frac = x - i;
            if (i >= table.Length - 1) return 1f;
            float a = table[i];
            float b = table[i + 1];
            return a + (b - a) * frac;
        }

        static float Ease(float t)
        {
            float pushTime = 0.1f;
            float pushAmount = 0.05f;
            if (t < pushTime) return pushAmount * (t / pushTime) * (t / pushTime);
            return pushAmount + (1f - pushAmount) * (1f - Mathf.Pow(1f - (t - pushTime) / (1f - pushTime), 4f));
        }

        void SetRotation(float degrees)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, degrees);
            float now = Time.unscaledTime;
            if (now > lastDegreesTime)
            {
                float degPerSec = Mathf.Abs(degrees - lastDegrees) / (now - lastDegreesTime);
                wheelSpeed = (wheelSpeed + degPerSec) * 0.5f;
            }
            lastDegrees = degrees;
            lastDegreesTime = now;
            int slice = SliceAt(starts, spans, Mathf.Repeat(-degrees, 360f));
            if (slice == lastSlice) return;
            if (lastSlice >= 0)
            {
                float fast = Mathf.Clamp01(wheelSpeed / 1200f);
                pointerKick = -Mathf.Lerp(9f, 28f, fast);
                pointerReturn = Mathf.Lerp(70f, 420f, fast);
                if (Plugin.TickVolume.Value > 0f)
                    AudioManager.PlayUISound(tickClip, Plugin.TickVolume.Value, Mathf.Lerp(0.88f, 1.12f, fast) + UnityEngine.Random.Range(-0.03f, 0.03f));
            }
            lastSlice = slice;
        }

        static IEnumerator PopIn(Transform panel)
        {
            panel.gameObject.SetActive(true);
            for (float elapsed = 0f; elapsed < 0.25f; elapsed += Time.unscaledDeltaTime)
            {
                float u = elapsed / 0.25f - 1f;
                panel.localScale = Vector3.one * (0.6f + 0.4f * (1f + 2.7f * u * u * u + 1.7f * u * u));
                yield return null;
            }
            panel.localScale = Vector3.one;
        }

        #region ui helpers

        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position, Vector2? anchor = null, Vector2? pivot = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor ?? Middle;
            rect.pivot = pivot ?? anchor ?? Middle;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Image Image(string name, Transform parent, Vector2 size, Vector2 position, Color color, Sprite sprite = null,
            Vector2? anchor = null, Vector2? pivot = null)
        {
            Image image = Rect(name, parent, size, position, anchor, pivot).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image Fill(string name, Transform parent, Color color, float margin = 0f)
        {
            Image image = Image(name, parent, Vector2.zero, Vector2.zero, color);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = new Vector2(margin, margin);
            image.rectTransform.offsetMax = new Vector2(-margin, -margin);
            return image;
        }

        public static Image Picture(string name, Transform parent, string sprite, Color color, Image.Type type, float ppu)
        {
            Image image = Fill(name, parent, color);
            image.sprite = Utils.GetSprite(sprite);
            image.type = type;
            image.pixelsPerUnitMultiplier = ppu;
            return image;
        }

        public static Transform Panel(string name, Transform parent, Vector2 size, Vector2 position, string title, Color bar, Color line)
        {
            Image root = Image(name, parent, size, position, Frame, Utils.GetSprite("window_outline_circle_16px_outline"));
            root.type = UnityEngine.UI.Image.Type.Sliced;
            root.pixelsPerUnitMultiplier = 0.5f;
            root.raycastTarget = true;
            root.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            Picture("Background", root.transform, "GM-Background-Campaign", Utils.Hex("#525252"), UnityEngine.UI.Image.Type.Tiled, 1.8f);

            RectTransform header = Rect("Header", root.transform, Vector2.zero, Vector2.zero);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 44f);
            Image dashes = header.gameObject.AddComponent<Image>();
            dashes.sprite = Utils.GetSprite("window_squirqle_dashed_16px_filled");
            dashes.type = UnityEngine.UI.Image.Type.Tiled;
            dashes.pixelsPerUnitMultiplier = 0.5f;
            dashes.color = bar;
            dashes.raycastTarget = false;
            header.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Picture("Line", header, "window_outline_squircle_16px_outline", line, UnityEngine.UI.Image.Type.Sliced, 0.5f);
            TextMeshProUGUI text = Text("Title", header, Vector2.zero, Vector2.zero, title, 26f, TextAlignmentOptions.MidlineLeft, Ink);
            text.font = Utils.GetFont("Ticketing SDF GreyGlow");
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(14f, 0f);
            text.rectTransform.offsetMax = new Vector2(-14f, -2f);

            Picture("Outline", root.transform, "window_outline_circle_16px_outline_hollow", Frame, UnityEngine.UI.Image.Type.Sliced, 0.5f);
            return root.transform;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, Vector2 size, Vector2 position, string text, float fontSize,
            TextAlignmentOptions alignment, Color color, Vector2? anchor = null, Vector2? pivot = null)
        {
            var tmp = Rect(name, parent, size, position, anchor, pivot).gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = Utils.GetFont();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        public static Button Button(string name, Transform parent, Vector2 size, Vector2 position, string label, Action onClick, bool primary = true)
        {
            UI_TrinketPicker picker = Array.Find(Resources.FindObjectsOfTypeAll<UI_TrinketPicker>(),
                p => p.gameObject.scene.name != null && p.playButton != null);
            GameObject clone = Instantiate(picker.playButton.gameObject, parent);
            clone.name = name;
            foreach (var c in clone.GetComponents<UI_FirstSelect>()) Destroy(c);
            foreach (var c in clone.GetComponents<UI_CloseButtonOnBack>()) Destroy(c);
            foreach (var c in clone.GetComponents<LayoutElement>()) Destroy(c);

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Middle;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;

            TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = text.fontSize;

            Button button = clone.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => onClick());
            button.interactable = true;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (primary)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = colors.selectedColor;
                button.colors = colors;
            }
            else
            {
                clone.GetComponent<Image>().color = Utils.Hex("#DDDDDDAE");
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Utils.Hex("#FF9E00");
                colors.pressedColor = Utils.Hex("#C8C8C8");
                colors.selectedColor = Utils.Hex("#F59206");
                button.colors = colors;
                foreach (var c in clone.GetComponentsInChildren<UI_CopySelectedColor>(true)) Destroy(c);
                text.color = Color.white;
                text.fontSize = 24f;
                text.fontSizeMax = 24f;
            }
            clone.SetActive(true);
            return button;
        }

        static void SetLabel(Button button, string label)
        {
            button.GetComponentInChildren<TMP_Text>(true).text = label;
        }

        static Sprite Draw(string name, int width, int height, Vector2 pivot, Func<float, float, Color> shade)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = shade(x + 0.5f - width / 2f, y + 0.5f - height / 2f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, 100f);
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        static Sprite PointerSprite(int width, int height, Color fill, Color edge, float outline)
        {
            float slant = width / Mathf.Sqrt(width * width + height * height / 4f);
            return Draw("WheelOfWuck_Pointer", width, height, new Vector2(0f, 0.5f), (x, y) =>
            {
                float toSlantedEdge = ((x + width / 2f) / width * height / 2f - Mathf.Abs(y)) * slant;
                float inside = Mathf.Min(toSlantedEdge, width / 2f - x);
                Color color = inside < outline ? edge : fill;
                color.a *= Mathf.Clamp01(inside + 0.5f);
                return color;
            });
        }

        static Sprite WheelSprite(int size, float[] starts, float[] spans, Color[] colors)
        {
            float radius = size / 2f - 1.5f;
            return Draw("WheelOfWuck_Wheel", size, size, Middle, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float angle = Mathf.Repeat(Mathf.Atan2(y, x) * Mathf.Rad2Deg, 360f);
                Color color = colors[SliceAt(starts, spans, angle)];
                if (d > radius - 4f) color = Color.Lerp(color, Color.black, 0.45f);
                color.a = Mathf.Clamp01(radius - d + 0.5f);
                return color;
            });
        }

        static int SliceAt(float[] starts, float[] spans, float angle)
        {
            for (int i = 0; i < starts.Length; i++)
            {
                if (angle >= starts[i] && angle < starts[i] + spans[i]) return i;
            }
            return starts.Length - 1;
        }

        static void DestroySprite(Sprite sprite)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }

        #endregion
    }
}
