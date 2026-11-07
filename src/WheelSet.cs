using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace WheelOfWuck
{
    public class WheelSet
    {
        public static readonly string FilePath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID + ".wheels.json");

        public static WheelSet Current; 
        static DateTime loadedAt;

        public string title = "Wheel of Wuck";
        public List<string> colors = new List<string>() { "#3369E8", "#D50F25", "#EEB211", "#009925" };
        public List<string> clearSettings = new List<string>() { "ironknuckle", "competitive" };
        public List<WheelDef> wheels = new List<WheelDef>();

        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                try { File.WriteAllText(FilePath, Default()); } catch { }
            }
            DateTime t = File.GetLastWriteTimeUtc(FilePath);
            if (Current != null && t == loadedAt) return;
            loadedAt = t;
            try
            {
                Current = JsonConvert.DeserializeObject<WheelSet>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"could not read {FilePath}, using the shipped wheels: {e.Message}");
                Current = JsonConvert.DeserializeObject<WheelSet>(Default());
            }
        }

        static string Default()
        {
            var stream = typeof(WheelSet).Assembly.GetManifestResourceStream("wheels.json");
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class WheelDef
    {
        public string id;
        public string title;
        public string spins = "1";
        public bool unique = true;
        public List<EntryDef> entries = new List<EntryDef>();
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class EntryDef
    {
        public string text;
        public float weight = 1f;
        public bool enabled = true;
        public Dictionary<string, bool> settings;
        public List<string> route;
        public string trinket;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int count;
        public string note;
    }
}
