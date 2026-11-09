using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WheelOfWuck
{
    public class WheelSet
    {
        public static WheelSet Current;

        public string title = "Wheel of Wuck";
        public List<string> colors = new List<string>() { "#3369E8", "#D50F25", "#EEB211", "#009925" };
        public List<string> clearSettings = new List<string>() { "ironknuckle", "competitive" };
        public List<WheelDef> wheels = new List<WheelDef>();

        public static void Load()
        {
            if (Current != null) return;
            var stream = typeof(WheelSet).Assembly.GetManifestResourceStream("wheels.json");
            using (var reader = new StreamReader(stream))
            {
                var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
                Current = JsonConvert.DeserializeObject<WheelSet>(reader.ReadToEnd(), settings);
            }
        }
    }

    public class WheelDef
    {
        public string id;
        public string title;
        public string spins = "1";
        public bool unique = true;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool campaign;
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
