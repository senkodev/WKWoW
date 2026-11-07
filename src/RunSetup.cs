using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace WheelOfWuck
{
    public static class RunSetup
    {
        public static bool didRevert;

        public static List<string> Apply(Roll roll, M_Gamemode mode)
        {
            List<string> notes = new List<string>();
            string modeName = mode.gamemodeName;
            StatManager.SaveData save = StatManager.saveData;
            save.SetupGamemode(modeName);

            Roll previous = Roll.Current;
            if (previous != null && previous.beforeSettings != null && Utils.Same(previous.gamemode, modeName) && previous.MatchesSave(mode))
            {
                roll.beforeSettings = previous.beforeSettings;
                roll.beforeTrinkets = previous.beforeTrinkets;
            }
            else
            {
                roll.beforeSettings = new List<string>(save.GetGameMode(modeName).activeSettings);
                roll.beforeTrinkets = new List<string>(save.GetGamemodeTrinkets(mode.GetGamemodeName()));
            }

            foreach (string id in WheelSet.Current.clearSettings)
            {
                save.SetSetting(modeName, id, false);
            }

            foreach (var wanted in roll.settings)
            {
                GamemodeSetting setting = FindSetting(mode, wanted.Key);
                if (setting == null)
                {
                    notes.Add($"{modeName.Trim()} has no setting called {wanted.Key}.");
                    continue;
                }
                if (wanted.Value && !SettingUnlocked(setting))
                {
                    notes.Add($"{setting.title} is still locked");
                    save.SetSetting(modeName, setting.id, false);
                    continue;
                }
                save.SetSetting(modeName, setting.id, wanted.Value);
                if (wanted.Value)
                {
                    foreach (string other in setting.exclusiveSettings) save.SetSetting(modeName, other, false);
                }
            }

            List<string> chosen = new List<string>();
            int budget = 1;
            foreach (string name in roll.trinkets)
            {
                Trinket trinket = FindTrinket(mode, name);
                if (trinket == null)
                {
                    notes.Add($"{name} is not offered in {modeName.Trim()}.");
                }
                else if (trinket.comingSoon || !trinket.IsUnlocked())
                {
                    notes.Add(trinket.title + " is still locked.");
                }
                else if (trinket.settingBlacklist.Exists(mode.HasActiveSetting))
                {
                    notes.Add($"{trinket.title} can't be used with the chosen settings.");
                }
                else if (!chosen.Contains(trinket.name))
                {
                    chosen.Add(trinket.name);
                    budget += trinket.isBinding ? trinket.cost : -trinket.cost;
                }
            }
            if(budget < 0)
            {
                if (notes != null)
                {
                    notes.Add("Too expensive! Take off some trinkets before playing the run.");
                }
            }
            save.SetGamemodeTrinkets(mode.GetGamemodeName(), chosen);

            roll.gamemode = modeName;
            roll.activeSettings = new List<string>(save.GetGameMode(modeName).activeSettings);
            roll.activeTrinkets = chosen;
            roll.chimneySettings = new List<string>();
            if (roll.route.Contains("chimney"))
            {
                M_Gamemode chimney = FindGamemode("Chimney");
                if (chimney == null) notes.Add("The Chimney gamemode was not found, so the run stays in the campaign.");
                else if (!GamemodeUnlocked(chimney)) notes.Add("The Chimney is not unlocked yet, so the run stays in the campaign.");
                else
                {
                    string chimneyName = chimney.gamemodeName;
                    save.SetupGamemode(chimneyName);
                    if (previous == null || previous.beforeChimneyTrinkets == null || !previous.route.Contains("chimney") || !previous.MatchesSave(chimney))
                    {
                        roll.beforeChimneySettings = new List<string>(save.GetGameMode(chimneyName).activeSettings);
                        roll.beforeChimneyTrinkets = new List<string>(save.GetGamemodeTrinkets(chimney.GetGamemodeName()));
                    }
                    else
                    {
                        roll.beforeChimneySettings = previous.beforeChimneySettings;
                        roll.beforeChimneyTrinkets = previous.beforeChimneyTrinkets;
                    }
                    foreach (string id in WheelSet.Current.clearSettings) save.SetSetting(chimneyName, id, false);
                    foreach (var wanted in roll.settings)
                    {
                        GamemodeSetting setting = FindSetting(chimney, wanted.Key);
                        if (setting == null) continue;
                        if (wanted.Value && !SettingUnlocked(setting)) continue;
                        save.SetSetting(chimneyName, setting.id, wanted.Value);
                    }
                    save.SetGamemodeTrinkets(chimney.GetGamemodeName(), new List<string>(chosen));
                    roll.chimneySettings = new List<string>(save.GetGameMode(chimneyName).activeSettings);
                }
            }
            roll.ResetProgress();
            Roll.Current = roll;
            roll.Save();
            RefreshMenus(mode);
            StatManager.instance.SaveStats();
            Plugin.Logger.LogInfo("rolled " + modeName.Trim() + ": route [" + string.Join(" > ", roll.route) + "], trinkets [" + string.Join(", ", chosen) + "], settings [" + string.Join(", ", roll.activeSettings) + "]");
            return notes;
        }

        public static void RevertOnce()
        {
            if (didRevert) return;
            Roll roll = Roll.Current;
            if (roll == null || roll.beforeSettings == null || StatManager.saveData == null) return;
            if (CL_GameManager.IsInGame()) return;
            M_Gamemode mode = System.Array.Find(Resources.FindObjectsOfTypeAll<M_Gamemode>(), m => Utils.Same(m.gamemodeName, roll.gamemode));
            if (mode == null) return;
            didRevert = true;
            if (!roll.MatchesSave(mode))
            {
                Plugin.Logger.LogInfo("the menu was changed since the roll, leaving it alone");
                return;
            }

            StatManager.SaveData save = StatManager.saveData;
            foreach (GamemodeSetting setting in mode.gamemodeSettings)
            {
                save.SetSetting(mode.gamemodeName, setting.id, roll.beforeSettings.Contains(setting.id));
            }
            save.SetGamemodeTrinkets(mode.GetGamemodeName(), new List<string>(roll.beforeTrinkets));
            M_Gamemode chimney = roll.route.Contains("chimney") && roll.beforeChimneyTrinkets != null ? FindGamemode("Chimney") : null;
            if (chimney != null && roll.MatchesSave(chimney))
            {
                foreach (GamemodeSetting setting in chimney.gamemodeSettings)
                {
                    save.SetSetting(chimney.gamemodeName, setting.id, roll.beforeChimneySettings.Contains(setting.id));
                }
                save.SetGamemodeTrinkets(chimney.GetGamemodeName(), new List<string>(roll.beforeChimneyTrinkets));
            }
            RefreshMenus(mode);
            StatManager.instance.SaveStats();
            Plugin.Logger.LogInfo("put the menu back the way it was before the wheel, the last roll is still on the wheel");
        }

        static void RefreshMenus(M_Gamemode mode)
        {
            foreach (UI_GamemodeSetting ui in Resources.FindObjectsOfTypeAll<UI_GamemodeSetting>())
            {
                if (ui.gameObject.scene.name == null) continue;
                ui.toggle.SetIsOnWithoutNotify(StatManager.saveData.IsSettingActive(mode.gamemodeName, ui.gamemodeSetting.id));
                Traverse.Create(ui).Method("UpdateColor").GetValue();
            }

            var screen = UI_GamemodeScreen.instance;
            if (screen != null && screen.gameObject.activeInHierarchy && screen.GetGamemode() == mode)
            {
                screen.RefreshCurrentGamemode();
            }

            foreach (var picker in Object.FindObjectsOfType<UI_TrinketPicker>())
            {
                picker.ReloadTrinkets();
            }
            SettingsManager.RefreshSettings();
        }

        public static M_Gamemode FindGamemode(string name)
        {
            foreach (M_Gamemode m in Resources.FindObjectsOfTypeAll<M_Gamemode>())
            {
                if (Utils.Same(m.gamemodeName, name)) return m;
            }
            return null;
        }

        public static bool GamemodeUnlocked(M_Gamemode mode)
        {
            if (mode == null) return false;
            if (mode.unlockAchievement == null || mode.unlockAchievement == "") return true;
            bool ok = CL_AchievementManager.GetAchievementValue(mode.unlockAchievement);
            return ok ? true : false;
        }

        public static M_Gamemode ChimneyInstead(Roll roll, M_Gamemode mode)
        {
            if (roll == null || mode == null) return null;
            if (!Utils.Same(mode.gamemodeName, roll.gamemode)) return null;
            if (!roll.Route.Contains("chimney")) return null;
            M_Gamemode chimney = FindGamemode("Chimney");
            if (chimney == null || !GamemodeUnlocked(chimney) || !roll.MatchesSave(chimney)) return null;
            return chimney;
        }

        public static Trinket FindTrinket(M_Gamemode mode, string name)
        {
            List<Trinket> all = new List<Trinket>(mode.availableTrinkets.trinkets);
            all.AddRange(mode.availableTrinkets.bindings);
            return all.Find(t => Utils.Same(t.name, name) || Utils.Same(t.title, name));
        }

        public static GamemodeSetting FindSetting(M_Gamemode mode, string id)
        {
            return mode.gamemodeSettings.Find(s => Utils.Same(s.id, id));
        }

        public static bool SettingUnlocked(GamemodeSetting setting)
        {
            if (setting.unlock == null) return true;
            return setting.unlock.CheckUnlock();
        }
    }
}
