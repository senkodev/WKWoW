using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace WheelOfWuck
{
    public class WheelPicks
    {
        public static readonly string FilePath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID + ".picks.json");

        public static WheelPicks Current = new WheelPicks();

        public Dictionary<string, Dictionary<string, bool>> enabled =
            new Dictionary<string, Dictionary<string, bool>>();

        public static void Load()
        {
            if (!File.Exists(FilePath)) return;
            try
            {
                Current = JsonConvert.DeserializeObject<WheelPicks>(File.ReadAllText(FilePath)) ?? new WheelPicks();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"could not read {FilePath}, using the wheel defaults: {e.Message}");
                Current = new WheelPicks();
            }
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static bool IsEnabled(WheelDef wheel, EntryDef entry)
        {
            Dictionary<string, bool> picks;
            bool on;
            if (Current.enabled.TryGetValue(wheel.id, out picks))
            {
                if (picks.TryGetValue(entry.text, out on))
                {
                    return on;
                }
            }
            return entry.enabled;
        }

        public static void Set(WheelDef wheel, EntryDef entry, bool on)
        {
            Dictionary<string, bool> picks;
            if (!Current.enabled.TryGetValue(wheel.id, out picks))
            {
                picks = new Dictionary<string, bool>();
                Current.enabled[wheel.id] = picks;
            }
            if (on == entry.enabled) picks.Remove(entry.text);
            else picks[entry.text] = on;
            if (picks.Count == 0) Current.enabled.Remove(wheel.id);
            //Debug.Log(wheel.id + "/" + entry.text + " = " + on);
            Current.Save();
        }

        public static void Clear()
        {
            Current.enabled.Clear();
            Current.Save();
        }
    }
}
