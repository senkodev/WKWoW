using HarmonyLib;
using UnityEngine;

namespace WheelOfWuck
{
    [HarmonyPatch(typeof(UI_TrinketPicker), "Initialize")]
    class Patch_TrinketPicker_Initialize
    {
        static void Postfix(UI_TrinketPicker __instance)
        {
            RunSetup.RevertOnce();
            MenuHook.AddButton(__instance);
        }
    }

    [HarmonyPatch(typeof(UI_GamemodeScreen), "LoadGamemode")]
    class Patch_GamemodeScreen_LoadGamemode
    {
        static bool Prefix(UI_GamemodeScreen __instance)
        {
            if (CL_SaveManager.loadSessionSaveOnLoad) return true;
            M_Gamemode picked = Traverse.Create(__instance).Field("baseGamemode").GetValue<M_Gamemode>();
            M_Gamemode chimney = RunSetup.ChimneyInstead(Roll.Current, picked);
            if (chimney == null) return true;
            Plugin.Logger.LogInfo("the roll says Chimney first, loading the Chimney instead of " + picked.gamemodeName.Trim());
            CL_GameManager.gamemode = chimney;
            UnityEngine.SceneManagement.SceneManager.LoadScene(chimney.gamemodeScene);
            if (__instance.loadingText != null) __instance.loadingText.SetActive(true);
            return false;
        }
    }

    [HarmonyPatch(typeof(CL_GameManager), "RestartScene")]
    class Patch_GameManager_RestartScene
    {
        static void Prefix(CL_GameManager __instance)
        {
            Roll roll = Roll.Current;
            if (roll == null || !roll.inPlay || roll.leg == 0 || !roll.route.Contains("chimney")) return;
            if (!Utils.Same(CL_GameManager.GetBaseGamemode()?.gamemodeName, "Chimney")) return;
            M_Gamemode campaign = RunSetup.FindGamemode(roll.gamemode);
            if (campaign == null) return;
            Plugin.Logger.LogInfo("the Chimney is done, this restart goes to the campaign run");
            Traverse.Create(__instance).Field("baseGamemode").SetValue(campaign);
            CL_GameManager.gamemode = campaign;
        }
    }

    [HarmonyPatch(typeof(UI_CloseButtonOnBack), "Update")]
    class Patch_CloseButtonOnBack_Update
    {
        static bool Prefix()
        {
            return !WheelUI.IsOpen;
        }
    }

    [HarmonyPatch(typeof(UT_BranchGenerator), "Generate")]
    class Patch_BranchGenerator_Generate
    {
        static void Postfix(UT_BranchGenerator __instance)
        {
            if (RunHud.Instance == null || __instance.branch == null) return;
            foreach (M_GenerationBranch b in __instance.branch)
            {
                if (b != null) RunHud.Instance.BranchTaken(b.name);
            }
        }
    }

    [HarmonyPatch(typeof(UT_ZoneTitler), "Play")]
    class Patch_ZoneTitler_Play
    {
        static void Postfix(UT_ZoneTitler __instance)
        {
            if (RunHud.Instance == null) return;
            RunHud.Instance.Titled(__instance.region, __instance.subRegion);
        }
    }

    [HarmonyPatch(typeof(StatManager), "LoadStats")]
    class Patch_StatManager_LoadStats {
        static void Postfix() { RunSetup.RevertOnce(); }
    }

    [HarmonyPatch(typeof(UI_TrinketPicker), "ReloadTrinkets")]
    class Patch_TrinketPicker_ReloadTrinkets
    {
        static void Postfix(UI_TrinketPicker __instance) => MenuHook.AddButton(__instance);
    }

    [HarmonyPatch(typeof(UI_TrinketPicker), "OnEnable")]
    class Patch_TrinketPicker_OnEnable
    {
        static void Postfix(UI_TrinketPicker __instance) => MenuHook.AddButton(__instance);
    }

    [HarmonyPatch(typeof(CL_GameManager))]
    [HarmonyPatch("Awake")]
    class Patch_GameManager_Awake
    {
        public static void Postfix()
        {
            Roll roll = Roll.Current;
            if (roll == null) return;
            if (CL_SaveManager.loadSessionSaveOnLoad) return;

            M_Gamemode mode = CL_GameManager.GetBaseGamemode();
            if (mode == null) return;

            roll.inPlay = roll.AppliesTo(mode) && roll.MatchesSave(mode);
            roll.ResetProgress();
            //Debug.Log("inPlay = " + roll.inPlay);
            Plugin.Logger.LogInfo(roll.inPlay ? "the run uses the roll" : "the run doesn't match the roll, the wheel stays quiet");
        }
    }

    [HarmonyPatch(typeof(M_Gamemode))]
    [HarmonyPatch("Finish")]
    class Patch_Gamemode_Finish
    {
        static void Postfix(M_Gamemode __instance, float time, bool hasFinished)
        {
            var roll = Roll.Current;
            if (roll == null) return;
            if (roll.inPlay == false) return;
            if (!roll.AppliesTo(CL_GameManager.GetBaseGamemode())) return;

            History.Record(roll, hasFinished, time,
                CL_GameManager.gMan.GetPlayerBestTravelDistance(),
                __instance.GetPlayerScore(hasFinished));
            bool beaten = false;
            if (hasFinished) beaten = true;
            else if (roll.ladderLooped && roll.Route.Contains("ladder"))
            {
                beaten = true;
                Plugin.Logger.LogInfo("the Ladder was looped once, so this death still counts as beating it");
            }
            if (beaten && roll.LegDone() && roll.NextLeg())
            {
                Plugin.Logger.LogInfo($"run {roll.leg} of the roll is done, the next run goes {string.Join(" > ", roll.Route.ConvertAll(Roll.Label))}");
            }
        }
    }
}
