using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace WheelOfWuck
{
    public class WheelResult
    {
        public string id;
        public string title;
        public List<string> winners = new List<string>();
    }

    public class Roll
    {
        public static readonly string FilePath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID + ".roll.json");

        public static Roll Current;

        public DateTime rolledAt;
        public string gamemode;
        public List<WheelResult> results = new List<WheelResult>();
        public Dictionary<string, bool> settings = new Dictionary<string, bool>();
        public List<string> trinkets = new List<string>();
        public List<string> route = new List<string>();
        public List<string> notes = new List<string>();
        public List<string> visited = new List<string>();
        public List<string> offRoute = new List<string>();
        public List<string> skipped = new List<string>();
        public List<List<string>> legs = new List<List<string>>();
        public int leg;
        public List<string> activeSettings = new List<string>();
        public List<string> activeTrinkets = new List<string>();
        public List<string> beforeSettings;
        public List<string> beforeTrinkets;
        public List<string> chimneySettings = new List<string>();
        public List<string> beforeChimneySettings;
        public List<string> beforeChimneyTrinkets;
        public bool chimneyLooped;
        public bool ladderLooped;
        public bool inPlay;

        public static void Load()
        {
            if (!File.Exists(FilePath)) return;
            Current = JsonConvert.DeserializeObject<Roll>(File.ReadAllText(FilePath));
            if (Current == null) return;
            Current.route = InClimbOrder(Current.route);
            for (int i = 0; i < Current.legs.Count; i++) Current.legs[i] = InClimbOrder(Current.legs[i]);
        }

        static readonly List<string> climbOrder = new List<string> { "chimney", "sink", "chute", "duct", "ladder", "diver", "hotzone", "infested" };

        static int Rank(string key)
        {
            int at = climbOrder.IndexOf(key);
            if (at == -1)
            {
                at = 99;
            }
            return at;
        }

        public static List<string> InClimbOrder(List<string> keys)
        {
            List<string> sorted = new List<string>();
            sorted.AddRange(keys);
            sorted.Sort((a, b) => Rank(a) - Rank(b));
            return sorted;
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        [JsonIgnore]
        public List<string> Route
        {
            get
            {
                if (legs.Count == 0) return route;
                return legs[Math.Min(leg, legs.Count - 1)];
            }
        }

        public void PlanLegs()
        {
            legs.Clear();
            leg = 0;
            var rest = new List<string>(route);
            rest.Remove("chimney");
            if (route.Contains("chimney"))
            {
                legs.Add(new List<string> { "chimney" });
            }
            if (rest.Contains("hotzone") && rest.Contains("ladder"))
            {
                var first = new List<string>(rest);
                first.Remove("hotzone");
                legs.Add(first);
                legs.Add(new List<string> { "hotzone" });
            }
            else if (legs.Count > 0)
            {
                legs.Add(rest);
            }
        }

        public bool LegDone()
        {
            bool allGood = true;
            foreach (string key in Route)
            {
                if (key == "chimney") continue;
                if(!visited.Contains(key))
                {
                    allGood = false;
                }
            }
            if (allGood == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool NextLeg()
        {
            if (leg >= legs.Count - 1) return false;
            leg++;
            ResetProgress();
            return true;
        }

        public bool HasRoute(string key)
        {
            return route.Contains(key);
        }

        public bool AppliesTo(M_Gamemode mode)
        {
            if (Utils.Same(mode.gamemodeName, gamemode)) return true;
            //if (Utils.Same(mode.gamemodeName, "Ladder")) return true;
            if (route.Contains("chimney") && Utils.Same(mode.gamemodeName, "Chimney")) return true;
            return false;
        }

        public bool MatchesSave(M_Gamemode mode)
        {
            var save = StatManager.saveData;
            var saved = save.GetGameMode(mode.gamemodeName)?.activeSettings ?? new List<string>();
            var mine = activeSettings;
            if (Utils.Same(mode.gamemodeName, "Chimney") && !Utils.Same(gamemode, "Chimney")) mine = chimneySettings;
            if (!new HashSet<string>(saved).SetEquals(mine)) return false;
            return new HashSet<string>(save.GetGamemodeTrinkets(mode.GetGamemodeName())).SetEquals(activeTrinkets);
        }

        public void ResetProgress()
        {
            visited.Clear();
            offRoute.Clear();
            skipped.Clear();
            chimneyLooped = false;
            ladderLooped = false;
            Save();
        }

        public bool Visit(string key)
        {
            if (key == null) return false;
            if (visited.Contains(key) == true) return false;
            visited.Add(key);

            bool off;
            if (key == "infested") off = Route.Contains("hotzone");
            else
                off = !Route.Contains(key);
            if (off) offRoute.Add(key);

            Save();
            return true;
        }

        public List<string> Arrive(string place)
        {
            var missed = new List<string>();
            if (place == null) return missed;
            //Debug.Log("arrived at " + place);
            foreach (string key in Route)
            {
                string[] points;
                if (!skipPoints.TryGetValue(key, out points)) continue;
                bool hit = false;
                foreach (string p in points)
                {
                    if (p.EndsWith("*")) hit = place.StartsWith(p.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
                    else hit = Utils.Same(p, place);
                    if (hit) break;
                }
                if (!hit) continue;
                if (visited.Contains(key) || skipped.Contains(key)) continue;
                skipped.Add(key);
                missed.Add(key);
            }
            if (missed.Count > 0) Save();
            return missed;

        }

        static Dictionary<string, string[]> skipPoints = new Dictionary<string, string[]>()
        {
            { "sink", new[] { "Deep Storage" } },
            { "chute", new[] { "Drainage System", "Waste Heap", "Pipe Organ" } },
            { "duct", new[] { "title:Service Shaft", "M3_Habitation_Shaft_0*", "M3_Habitation_Shaft_To_Pier" } },
            { "ladder", new[] { "M3_Habitation_Lab_Ending", "abyss" } },
            { "diver", new[] { "transit-scaffolds" } },
            { "hotzone", new[] { "infested" } },
        };

        static Dictionary<string, string> routeKeys = new Dictionary<string, string>()
        {
            { "sink", "sink" },
            { "chute", "chute" },
            { "ladder", "ladder" },
            { "rotten-duct", "duct" },
            { "hadal-diver", "diver" },
            { "hotzone", "hotzone" },
            { "infestedlabs", "infested" },
            { "chimney", "chimney" },
        };

        static Dictionary<string, string> routeLabels = new Dictionary<string, string>()
        {
            { "sink", "Sink" },
            { "chute", "Chute" },
            { "ladder", "Ladder" },
            { "duct", "Duct" },
            { "diver", "Diver" },
            { "hotzone", "Hot Zone" },
            { "infested", "Infested Labs" },
            { "chimney", "Chimney" },
        };

        static Dictionary<string, string> routeUnlocks = new Dictionary<string, string>()
        {
            { "sink", "unlockedsink" },
            { "chute", "chutedoorunlocked" },
            { "duct", "hasvisited-deadmans-handle" },
            { "diver", "hadaldiverunlocked" },
        };

        public static string Label(string key)
        {
            string label;
            if (routeLabels.TryGetValue(key, out label)) return label;
            return key;
        }

        public static bool RouteUnlocked(string key)
        {
            if (key == "chimney") return RunSetup.GamemodeUnlocked(RunSetup.FindGamemode("Chimney"));
            string flag;
            if (!routeUnlocks.TryGetValue(key, out flag)) return true;
            return StatManager.saveData.GetFlag(flag)?.value == true;
        }

        public static string RouteKey(string regionName, string subregionName, string gamemodeName)
        {
            if (gamemodeName == "Ladder") return "ladder";
            string key;
            if (regionName != null && routeKeys.TryGetValue(regionName, out key)) return key;
            if (subregionName != null && routeKeys.TryGetValue(subregionName, out key)) return key;
            return null;
        }
    }
}
