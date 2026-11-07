using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace WheelOfWuck
{
    public class RunRecord
    {
        public DateTime ended;
        public string gamemode;
        public bool won;
        public float seconds;
        public float height;
        public float score;
        public List<WheelResult> results;
        public List<string> route;
        public List<string> visited;
        public List<string> offRoute;
    }


    public static class History
    {
        public static readonly string FilePath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID + ".history.json");

        public static void Record(Roll roll, bool won, float seconds, float height, float score)
        {
            List<RunRecord> runs;
            if (File.Exists(FilePath)) {
                runs = JsonConvert.DeserializeObject<List<RunRecord>>(File.ReadAllText(FilePath));
            } else {
                runs = new List<RunRecord>();
            }

            RunRecord rec = null;
            rec = new RunRecord();
            rec.ended = DateTime.Now;
            rec.gamemode = CL_GameManager.GetBaseGamemode()?.gamemodeName.Trim() ?? roll.gamemode;
            rec.won = won;
            rec.seconds = seconds;
            rec.height = height;
            rec.score = score;
            //Debug.Log(rec.gamemode + " " + rec.won);
            rec.results = roll.results;
            rec.route = roll.route;
            rec.visited = roll.visited;
            rec.offRoute = roll.offRoute;
            runs.Add(rec);

            string json = "";
            json = JsonConvert.SerializeObject(runs, Formatting.Indented);
            File.WriteAllText(FilePath, json);
            Plugin.Logger.LogInfo($"run {runs.Count} written to the history: {(won ? "won" : "died")} after {seconds:0}s at {height:0}m");
        }
    }
}
