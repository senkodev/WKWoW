using System;
using System.Collections.Generic;
using System.Linq;

namespace WheelOfWuck
{
    public static class WheelLogic
    {
        public static List<(WheelDef wheel, EntryDef entry)> wins = new List<(WheelDef, EntryDef)>();
        static System.Random rng = new System.Random();

        public static int SpinsFor(WheelDef wheel)
        {
            int fixedCount;
            if (int.TryParse(wheel.spins, out fixedCount)) return fixedCount;

            int count = 0;
            for (int i = 0; i < wins.Count; i++)
            {
                if (wins[i].wheel.id == wheel.spins) count = count + wins[i].entry.count;
            }
            return count;
        }

        public static int Spins(WheelDef wheel)
        {
            int spins = SpinsFor(wheel);
            if (wheel.id == "trinket" && Plugin.TrinketPerBinding.Value)
            {
                int bindings = 0;
                for (int i = 0; i < wins.Count; i++)
                {
                    if (wins[i].wheel.id == "bindings") bindings++;
                }
                spins = bindings + 1;
            }
            return spins;
        }

        public static int Slack(M_Gamemode mode)
        {
            int slack = 1;
            for (int i = 0; i < wins.Count; i++)
            {
                var win = wins[i];
                if (string.IsNullOrEmpty(win.entry.trinket)) continue;
                Trinket t = RunSetup.FindTrinket(mode, win.entry.trinket);
                if (t == null) continue;
                int c = t.cost;
                if (t.isBinding == true) slack = slack + c;
                else slack = slack - c;
            }
            //Debug.Log("slack " + slack);
            return slack;
        }

        public static bool Rolled(string setting)
        {
            bool on = false;
            for (int i = 0; i < wins.Count; i++)
            {
                if (wins[i].entry.settings == null) continue;
                bool value;
                if (wins[i].entry.settings.TryGetValue(setting, out value)) on = value;
            }
            return on;
        }

        static readonly List<string> shortcuts = new List<string> { "sink", "chute", "duct", "ladder", "diver" };

        public static bool Campaign(M_Gamemode mode)
        {
            string name = mode.gamemodeName;
            if (Utils.Same(name, "Campaign")) return true;
            return false;
        }

        public static bool Offered(WheelDef wheel, M_Gamemode mode)
        {
            if (wheel.campaign)
            {
                if (!Campaign(mode)) return false;
            }
            return true;
        }

        public static bool Offered(EntryDef entry, M_Gamemode mode)
        {
            bool campaign = Campaign(mode);
            if (entry.route != null)
            {
                if (campaign == false) return false;
            }
            if (!string.IsNullOrEmpty(entry.trinket))
            {
                Trinket t = RunSetup.FindTrinket(mode, entry.trinket);
                if (t == null) return false;
            }
            if (entry.settings == null) return true;
            foreach (var wanted in entry.settings)
            {
                if (!wanted.Value) continue;
                GamemodeSetting s = RunSetup.FindSetting(mode, wanted.Key);
                if (s == null) return false;
                if (Allowed(mode, wanted.Key) == false) return false;
            }
            return true;
        }

        static bool Allowed(M_Gamemode mode, string setting)
        {
            bool ok = true;
            if (setting == "hardmode") ok = mode.allowHardmode;
            else if (setting == "ironknuckle") ok = mode.allowIronKnuckle;
            else if (setting == "competitive") ok = mode.allowCompetitive;
            return ok;
        }

        public static List<EntryDef> Live(WheelDef wheel, M_Gamemode mode)
        {
            var live = new List<EntryDef>();
            if (!Offered(wheel, mode))
            {
                //Debug.Log(wheel.id + " not in " + mode.gamemodeName);
                return live;
            }
            int slack = Slack(mode);
            foreach (EntryDef entry in wheel.entries)
            {
                if (WheelPicks.IsEnabled(wheel, entry) == false || entry.weight <= 0f) continue;
                if (!Offered(entry, mode)) continue;
                if (wheel.unique && wins.Any(w => w.wheel == wheel && w.entry == entry)) continue;
                if (entry.route != null && Rolled("ironknuckle") && entry.route.Exists(shortcuts.Contains))
                {
                    Plugin.Logger.LogInfo($"{entry.text} stays off the {wheel.title} wheel: no shortcuts in Iron Knuckle");
                    continue;
                }
                if (!string.IsNullOrEmpty(entry.trinket))
                {
                    Trinket t = RunSetup.FindTrinket(mode, entry.trinket);
                    if (t != null && t.settingBlacklist.Exists(Rolled))
                    {
                        Plugin.Logger.LogInfo($"{entry.text} stays off the {wheel.title} wheel: the game doesn't allow it with the rolled difficulty");
                        continue;
                    }
                    if (t != null && Rolled("ironknuckle"))
                    {
                        bool gone = false;
                        if (!t.isBinding) gone = true;
                        if (Utils.Same(t.name, "Binding_NoShops")) gone = true;
                        if (gone)
                        {
                            Plugin.Logger.LogInfo($"{entry.text} stays off the {wheel.title} wheel due to iron knuckle enabled");
                            continue;
                        }
                    }
                    if (t != null && !t.isBinding && slack - t.cost < 0)
                    {
                        Plugin.Logger.LogInfo($"{entry.text} stays off the {wheel.title} wheel: not enough bindings to afford it");
                        continue;
                    }
                }
                string locked = Locked(entry, mode);
                if (locked != null)
                {
                    Plugin.Logger.LogInfo(string.Format("{0} stays off the {1} wheel: {2}", entry.text, wheel.title, locked));
                    continue;
                }
                live.Add(entry);
            }
            return live;
        }

        public static string Locked(EntryDef entry, M_Gamemode mode)
        {
            if (!string.IsNullOrEmpty(entry.trinket))
            {
                Trinket trinket = RunSetup.FindTrinket(mode, entry.trinket);
                if (trinket == null) return $"{mode.gamemodeName} offers no trinket or binding called {entry.trinket}";
                if (trinket.comingSoon || !trinket.IsUnlocked()) return $"{trinket.title} is not unlocked yet";
            }
            if (entry.settings != null)
            {
                foreach (var wanted in entry.settings)
                {
                    GamemodeSetting setting = RunSetup.FindSetting(mode, wanted.Key);
                    if (setting == null) return $"{mode.gamemodeName} has no setting called {wanted.Key}";
                    if (wanted.Value && !RunSetup.SettingUnlocked(setting)) return $"{setting.title} is not unlocked yet";
                    if (wanted.Value && !Allowed(mode, wanted.Key)) return $"{setting.title} can't be used in {mode.gamemodeName.Trim()}";
                }
            }
            if (entry.route != null)
            {
                foreach (string key in entry.route)
                {
                    if (!Roll.RouteUnlocked(key)) return $"the {Roll.Label(key)} is not unlocked yet";
                }
            }
            return null;
        }

        public static EntryDef Pick(List<EntryDef> live)
        {
            double total = 0;
            int howMany = live.Count;
            for (int i = 0; i < howMany; i++)
            {
                total = total + live[i].weight;
            }
            double at = rng.NextDouble() * total;
            foreach (EntryDef entry in live)
            {
                at -= entry.weight;
                if(at < 0) return entry;
            }
            return live[live.Count - 1];
        }

        public static Roll BuildRoll(List<WheelResult> results)
        {
            Roll roll = new Roll();
            roll.rolledAt = DateTime.Now;
            roll.results = results;
            foreach (var win in wins)
            {
                EntryDef winner = win.entry;
                if (winner.settings != null)
                {
                    foreach (var s in winner.settings) roll.settings[s.Key] = s.Value;
                }
                if (winner.route != null)
                {
                    foreach (string key in winner.route)
                    {
                        if (!roll.route.Contains(key)) roll.route.Add(key);
                    }
                }
                if (!string.IsNullOrEmpty(winner.trinket) && !roll.trinkets.Contains(winner.trinket))
                    roll.trinkets.Add(winner.trinket);
                if (!string.IsNullOrEmpty(winner.note))
                    roll.notes.Add(winner.note);
            }
            roll.route = Roll.InClimbOrder(roll.route);
            roll.PlanLegs();
            if (roll.legs.Count > 1)
            {
                var names = new List<string>();
                foreach (var l in roll.legs) names.Add(l.Count == 0 ? "the campaign" : string.Join(" > ", l.ConvertAll(Roll.Label)));
                roll.notes.Add(names.Count + " runs: " + string.Join(", then ", names) + ".");
            }
            return roll;
        }
    }
}
