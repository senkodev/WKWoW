using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace WheelOfWuck
{
    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "senkodev.whiteknuckle.wheelofwuck";
        public const string PLUGIN_NAME = "WheelOfWuck";
        public const string PLUGIN_VERSION = "1.1.1";
    }

    public enum HudCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        public static Plugin Instance;

        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        public static ConfigEntry<KeyCode> HudKey;
        public static ConfigEntry<bool> ShowHud;
        public static ConfigEntry<HudCorner> HudPosition;
        public static ConfigEntry<float> HudScale;
        public static ConfigEntry<float> TickVolume;
        public static ConfigEntry<bool> RestartOnSkip;
        public static ConfigEntry<bool> TrinketPerBinding;
        public static ConfigEntry<KeyCode> ChimneyStayKey;
        public static ConfigEntry<KeyCode> ChimneyGoKey;

        private void Awake()
        {
            Logger = base.Logger; Instance = this;

            HudKey = Config.Bind("General", "HudKey", KeyCode.F8, "A binding key to show or hide the panel during a wheel run.");
            ShowHud = Config.Bind("General", "ShowHud", true, "Whether to show the wheel results during the run.");
            HudPosition = Config.Bind("General", "HudPosition", HudCorner.TopRight, "Which corner of the screen to use for the results.");
            HudScale = Config.Bind("General", "HudScale", 0.75f,
                new ConfigDescription("Size of the results panel.", new AcceptableValueRange<float>(0.5f, 1f)));
            TickVolume = Config.Bind("General", "TickVolume", 0.6f,
                new ConfigDescription("Volume of the ticking when the wheel spins.", new AcceptableValueRange<float>(0f, 1f)));
            ChimneyStayKey = Config.Bind("General", "ChimneyStayKey", KeyCode.F3, "A binding key to continue the Chimney run after a loop.");
            ChimneyGoKey = Config.Bind("General", "ChimneyGoKey", KeyCode.F6, "A binding key to end the run after a loop of the Chimney, so the run is properly submitted to the leaderboard.");
            RestartOnSkip = Config.Bind("General.Toggles", "RestartOnSkip", true,
                "Whether to kill the player as soon as a route the wheel picked can't be reached any more.");
            TrinketPerBinding = Config.Bind("General.Toggles", "TrinketPerBinding", true,
                "Whether to spin the trinket wheel once per rolled binding + 1 free (so 3 bindings gives 4 trinkets).");

            WheelSet.Load();
            WheelPicks.Load();
            Roll.Load();

            harmony.PatchAll();

            GameObject go = null;
            go = new GameObject("WheelOfWuck");
            if (go != null)
            {
                go.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(go);
                go.AddComponent<RunHud>();
            }

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
