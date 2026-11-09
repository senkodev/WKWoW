using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WheelOfWuck
{
    public class RunHud : MonoBehaviour
    {
        public static RunHud Instance;

        GameObject canvas;
        GameObject notice;
        CanvasGroup noticeGroup;
        bool restarting;
        int asked;
        bool inLoopRoom;
        bool asking;
        TextMeshProUGUI text;
        HudCorner builtCorner;
        float builtScale;

        static string Compose(Roll roll)
        {
            var lines = new List<string>();
            lines.Add("<b><color=#EEB211>WHEEL OF WUCK</color></b>");
            foreach (WheelResult result in roll.results)
            {
                if (result.winners.Count > 0)
                {
                    string w = "";
                    foreach (string one in result.winners)
                    {
                        if (w != "") w += ", ";
                        w += one;
                    }
                    lines.Add("<color=#AAAAAA>" + result.title + ":</color> " + w);
                }
            }
            if (roll.legs.Count > 1)
            {
                for (int i = 0; i < roll.legs.Count; i++)
                {
                    var parts = new List<string>();
                    foreach (string key in roll.legs[i])
                    {
                        string col = i < roll.leg ? "#55DD55" : i > roll.leg ? "#777777"
                            : roll.visited.Contains(key) ? "#55DD55" : roll.skipped.Contains(key) ? "#FF5555" : "#FFFFFF";
                        parts.Add($"<color={col}>{Roll.Label(key)}</color>");
                    }
                    string tag = i == roll.leg ? "<color=#EEB211>Run " + (i + 1) + ":</color>" : "<color=#AAAAAA>Run " + (i + 1) + ":</color>";
                    if (parts.Count == 0) parts.Add(i < roll.leg ? "<color=#55DD55>Campaign</color>" : i > roll.leg ? "<color=#777777>Campaign</color>" : "<color=#FFFFFF>Campaign</color>");
                    lines.Add($"{tag} {string.Join(" > ", parts)}");
                }
            }
            else if (roll.Route.Count > 0)
            {
                var parts = new List<string>();
                foreach (string key in roll.Route)
                {
                    string col = roll.visited.Contains(key) ? "#55DD55" : roll.skipped.Contains(key) ? "#FF5555" : "#FFFFFF";
                    parts.Add($"<color={col}>{Roll.Label(key)}</color>");
                }
                lines.Add($"<color=#AAAAAA>Path:</color> {string.Join(" > ", parts)}");
            }
            if (roll.offRoute.Count > 0)
            {
                lines.Add($"<color=#FF5555>Off route: {string.Join(", ", roll.offRoute.ConvertAll(Roll.Label))}</color>");
            }
            return string.Join("\n", lines);
        }

        void Awake()
        {
            Instance = this;
        }

        CL_GameManager seenManager;

        void Update()
        {
            Roll roll = Roll.Current;
            if (asking && (CL_GameManager.gMan != seenManager || !CL_GameManager.IsInGame()))
            {
                asked = 1;
            }
            seenManager = CL_GameManager.gMan;
            bool wheelRun = roll != null && roll.inPlay && CL_GameManager.IsInGame()
                && roll.AppliesTo(CL_GameManager.GetBaseGamemode());
            if (wheelRun && Input.GetKeyDown(Plugin.HudKey.Value))
            {
                Plugin.ShowHud.Value = !Plugin.ShowHud.Value;
            }

            if (wheelRun) Track(roll);
            if (notice != null && asked == 0 && noticeGroup != null && noticeGroup.alpha > 0.9f && !restarting)
            {
                if (Input.GetKeyDown(Plugin.ChimneyStayKey.Value)) asked = 1;
                if (Input.GetKeyDown(Plugin.ChimneyGoKey.Value)) asked = 2;
            }
            if (!wheelRun || !Plugin.ShowHud.Value)
            {
                if (canvas != null) canvas.SetActive(false);
                return;
            }

            if (canvas == null || builtCorner != Plugin.HudPosition.Value || builtScale != Plugin.HudScale.Value)
                Build();

            canvas.SetActive(!CL_GameManager.gMan.isPaused && !OS_Manager.inUse);
            string composed = Compose(roll);
            if (text.text != composed)
            {
                text.text = composed;
                LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform.parent as RectTransform);
            }
        }

        void Build()
        {
            if (canvas != null) Destroy(canvas);
            builtCorner = Plugin.HudPosition.Value;
            builtScale = Plugin.HudScale.Value;

            canvas = new GameObject("WheelOfWuck_Hud");
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            bool right = builtCorner == HudCorner.TopRight || builtCorner == HudCorner.BottomRight;
            bool top = builtCorner == HudCorner.TopLeft || builtCorner == HudCorner.TopRight;

            GameObject panelGo = new GameObject("Panel", typeof(RectTransform));
            RectTransform panel = panelGo.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(right ? 1 : 0, top ? 1 : 0);
            panel.sizeDelta = new Vector2(320, 100);
            panel.anchoredPosition = new Vector2(right ? -24 : 24, top ? -24 : 24);
            panel.localScale = Vector3.one * builtScale;
            panelGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(panel, false);
            text.rectTransform.sizeDelta = new Vector2(320, 80);
            text.font = Utils.GetFont();
            text.fontSize = 14;
            text.alignment = right ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
        }

        void Track(Roll roll)
        {
            if (WorldLoader.instance == null) return;
            //if (roll.visited.Count > 20) return;
            var loader = Traverse.Create(WorldLoader.instance);
            string regionName = loader.Field("currentRegion").GetValue<M_Region>()?.regionName;
            string subregionName = loader.Field("currentSubregion").GetValue<M_Subregion>()?.subregionName;
            string levelName = WorldLoader.instance.GetCurrentLevel()?.GetLevel()?.levelName;
            string key = Roll.RouteKey(regionName, subregionName, CL_GameManager.GetCurrentGamemode()?.gamemodeName);
            //Debug.Log(regionName + " / " + subregionName + " / " + levelName + " -> " + key);
            if (roll.Visit(key))
            {
                Plugin.Logger.LogInfo($"entered {Roll.Label(key)}{(roll.offRoute.Contains(key) ? " (off route)" : "")}");
            }
            Skipped(roll, roll.Arrive(regionName));
            Skipped(roll, roll.Arrive(subregionName));
            Skipped(roll, roll.Arrive(levelName));
            if (key != null) Skipped(roll, roll.Arrive(key));

            bool loopRoom = levelName != null && levelName.StartsWith("MX_Chimney_LoopRoom") && roll.route.Contains("chimney");
            if (loopRoom && !inLoopRoom)
            {
                if (!roll.chimneyLooped)
                {
                    roll.chimneyLooped = true;
                    roll.Save();
                    Plugin.Logger.LogInfo("one loop of the Chimney is done");
                    roll.NextLeg();
                }
                else Plugin.Logger.LogInfo("another loop of the Chimney is done");
                if (!asking) StartCoroutine(AskAfterLoop(roll));
            }
            inLoopRoom = loopRoom;

            if (roll.Route.Contains("ladder")) CheckLadder(roll);

            bool ladderLoop = levelName != null && levelName.StartsWith("M3_Habitation_LadderWarp_Loop") && roll.Route.Contains("ladder");
            if (ladderLoop && roll.ladderLooped == false)
            {
                roll.ladderLooped = true;
                roll.Save();
                Plugin.Logger.LogInfo("one loop of the Ladder is done, dying from here still counts as beating it");
                StartCoroutine(Flash("ONE LOOP OF THE LADDER DONE." + "\n" + "<size=60%>Walk out to end the run or keep climbing - this run counts as complete either way.</size>"));
            }
        }

        IEnumerator AskAfterLoop(Roll roll)
        {
            asking = true;
            asked = 0;
            Notice("ONE LOOP OF THE CHIMNEY DONE." + "\n" + "<size=60%>" + Plugin.ChimneyStayKey.Value + ": keep climbing      " + Plugin.ChimneyGoKey.Value + ": end the run here and proceed to the campaign</size>");
            yield return Fade(0f, 1f, 0.5f);
            while (asked == 0) yield return null;
            yield return Fade(1f, 0f, 0.5f);
            if (notice != null) Destroy(notice);
            if (asked == 2 && CL_GameManager.gMan != null && CL_GameManager.IsInGame() && ENT_Player.playerObject != null)
            {
                Plugin.Logger.LogInfo("ending the Chimney run so the score counts, restart goes to the campaign");
                ENT_Player.playerObject.Kill("", null);
            }
            asked = 0;
            asking = false;
        }

        IEnumerator Flash(string message)
        {
            Notice(message);
            yield return Fade(0f, 1f, 0.5f);
            yield return new WaitForSecondsRealtime(5f);
            yield return Fade(1f, 0f, 0.8f);
            if (notice != null) Destroy(notice);
        }

        public void BranchTaken(string branchName)
        {
            Roll roll = Roll.Current;
            if (roll == null || !roll.inPlay) return;
            string key = null;
            string name = "" + branchName;
            branchName = name;
            if (branchName.Contains("TangledSink")) key = "sink";
            else if (branchName.Contains("ExpulsionChute")) key = "chute";
            else if (branchName.Contains("RottenDuct")) key = "duct";
            else if (branchName.Contains("HadalDiver")) key = "diver";
            else if (branchName.Contains("Ladder")) key = "ladder";
            else if (branchName.Contains("Nest_HotZone")) key = "hotzone";
            else if (branchName.Contains("InfestedLabs")) key = "infested";
            if (key == null) return;
            //Debug.Log(branchName + " -> " + key);
            Plugin.Logger.LogInfo($"the game is building the {Roll.Label(key)} ({branchName})");
            if (roll.Visit(key))
            {
                Plugin.Logger.LogInfo($"entered {Roll.Label(key)}{(roll.offRoute.Contains(key) ? " (off route)" : "")}");
            }
            if (key == "infested") Skipped(roll, roll.Arrive("infested"));
        }

        public void Titled(string region, string subregion)
        {
            Roll roll = Roll.Current;
            if (roll == null || !roll.inPlay) return;
            //Debug.Log(region + " / " + subregion);
            Plugin.Logger.LogInfo($"area title: {region} / {subregion}");
            Skipped(roll, roll.Arrive("title:" + subregion));
            Skipped(roll, roll.Arrive("title:" + region));
        }

        public void Skipped(Roll roll, List<string> keys)
        {
            if (keys.Count == 0) return;
            string names = "";
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) names = names + " and the ";
                names = names + Roll.Label(keys[i]);
            }
            Plugin.Logger.LogInfo($"skipped the {names}");
            if (!Plugin.RestartOnSkip.Value || restarting) return;
            StartCoroutine(Restart(names));
        }

        static readonly List<string> ladderLabs = new List<string>
        {
            "M3_Habitation_Lab_01",
            "M3_Habitation_Lab_03",
            "M3_Habitation_Lab_05",
            "M3_Habitation_Lab_07",
        };

        public static int LadderRooms(WorldLoader.BranchInfo branch)
        {
            int labs = 0;
            int ladders = 0;
            bool labsDone = false;
            for (int i = 0; i < branch.levelTracker.Count; i++)
            {
                M_Level level = branch.levelTracker[i].GetLevel();
                if (level == null) return -1;
                string name = level.levelName;
                //Debug.Log(i + ": " + name);
                if (name == "M3_Habitation_Lab_Ending") labsDone = true;
                if (name.StartsWith("M3_Habitation_Lab_0"))
                {
                    labs++;
                    if (ladderLabs.Contains(name)) ladders++;
                }
            }
            if (labsDone == false || labs == 0) return -1;
            return ladders;
        }

        void CheckLadder(Roll roll)
        {
            if (roll.visited.Contains("ladder") || roll.skipped.Contains("ladder")) return;
            WorldLoader.BranchInfo branch = WorldLoader.instance.currentBranch;
            if (branch == null) return;

            int rooms = LadderRooms(branch);
            //Debug.Log("ladder rooms: " + rooms);
            if (rooms != 0) return;

            roll.skipped.Add("ladder");
            roll.Save();
            Plugin.Logger.LogInfo("no room with the ladder in this run's delta lab");
            if (!Plugin.RestartOnSkip.Value || restarting) return;
            LadderRestart();
        }

        void LadderRestart()
        {
            Plugin.Logger.LogInfo("restarting the run to get the ladder entrance");
            AccessTools.Method(typeof(CL_SaveManager), "DeleteSessionSave")
                .Invoke(null, new object[] { CL_GameManager.GetGamemodeName(false, true), CL_GameManager.IsHardmode() });
            CL_SaveManager.isSavedRun = false;
            CL_GameManager.ChangeState("restart");
        }

        IEnumerator Restart(string names)
        {
            restarting = true;
            Notice("YOU SKIPPED THE " + names.ToUpperInvariant() + "." + "\n" + "<size=60%>The wheel restarts the run.</size>");
            yield return Fade(0f, 1f, 0.5f);
            yield return new WaitForSecondsRealtime(3.7f);
            yield return Fade(1f, 0f, 0.8f);
            if (CL_GameManager.gMan != null && CL_GameManager.IsInGame())
            {
                Plugin.Logger.LogInfo("restarting the run");
                AccessTools.Method(typeof(CL_SaveManager), "DeleteSessionSave")
                    .Invoke(null, new object[] { CL_GameManager.GetGamemodeName(false, true), CL_GameManager.IsHardmode() });
                CL_SaveManager.isSavedRun = false;
                CL_GameManager.ChangeState("restart");
            }
            yield return new WaitForSecondsRealtime(1.5f);
            if (notice != null) Destroy(notice);
            restarting = false;
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds && noticeGroup != null; t += Time.unscaledDeltaTime)
            {
                noticeGroup.alpha = Mathf.SmoothStep(from, to, t / seconds);
                yield return null;
            }
            if (noticeGroup != null)
                noticeGroup.alpha = to;
        }

        void Notice(string message)
        {
            if (notice != null && notice) Destroy(notice);
            notice = new GameObject("WheelOfWuck_Notice");
            DontDestroyOnLoad(notice);
            Canvas c = notice.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 101;
            noticeGroup = notice.AddComponent<CanvasGroup>();
            noticeGroup.alpha = 0f;
            var scaler = notice.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var tmp = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.SetParent(notice.transform, false);
            tmp.rectTransform.sizeDelta = new Vector2(1400, 200);
            tmp.rectTransform.anchoredPosition = new Vector2(0, 280);
            tmp.font = Utils.GetFont("Ticketing SDF GreyGlow");
            tmp.fontSize = 48;
            tmp.color = Utils.Hex("#FF2F39");
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.text = message;
        }
    }
}
