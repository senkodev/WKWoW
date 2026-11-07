using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HarmonyLib;

namespace WheelOfWuck
{
    public static class MenuHook
    {
        public const string ButtonName = "WheelOfWuck_Button";

        public static void AddButton(UI_TrinketPicker picker)
        {
            Transform bar = picker.playButton.transform.parent;
            if (bar.Find(ButtonName) != null)
                return;

            GameObject clone = GameObject.Instantiate(picker.playButton.gameObject, bar);
            string name = ButtonName;
            clone.name = name;
            clone.transform.SetSiblingIndex(picker.playButton.transform.GetSiblingIndex() + 1);
            foreach (var fs in clone.GetComponents<UI_FirstSelect>())
                Object.Destroy(fs);

            Button btn = clone.GetComponent<Button>();
            if (btn == null)
            {
            }
            else
            {
            }
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() => Open(picker));
            btn.interactable = true;
            clone.GetComponentInChildren<TMP_Text>(true).text = "The Wheel";
            clone.SetActive(true);
            //Debug.Log("wheel button added to " + bar.name);
            Plugin.Logger.LogInfo($"Wheel button added next to Play in {bar.name}.");
        }

        static void Open(UI_TrinketPicker picker)
        {
            M_Gamemode mode = Traverse.Create(picker).Field("currentGamemode").GetValue<M_Gamemode>();
            if (mode == null) mode = CL_GameManager.GetBaseGamemode();
            WheelSet.Load();
            WheelUI.Open(WheelSet.Current, mode, roll => RunSetup.Apply(roll, mode));
        }
    }
}
