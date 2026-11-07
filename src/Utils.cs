using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WheelOfWuck
{
    public static class Utils
    {
        static TMP_FontAsset _font;
        static Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();
        static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static bool Same(string a, string b)
        {
            if (a == null || b == null)
            {
                if (a == null && b == null) return true;
                return false;
            }
            string x = a.Trim();
            string y = b.Trim();
            if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
            return false;
        }

        public static TMP_FontAsset GetFont()
        {
            if (_font != null) return _font;
            foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (f.name == "Ticketing SDF Smooth")
                {
                    _font = f;
                    break;
                }
            }
            if (_font == null) _font = TMP_Settings.defaultFontAsset;
            return _font;
        }

        public static TMP_FontAsset GetFont(string name)
        {
            TMP_FontAsset font;
            if (fonts.TryGetValue(name, out font) && font != null) return font;
            foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (f.name == name)
                {
                    font = f;
                    break;
                }
            }
            if (font == null) font = GetFont();
            fonts[name] = font;
            return font;
        }

        public static Sprite GetSprite(string name)
        {
            Sprite sprite;
            if (sprites.TryGetValue(name, out sprite) && sprite != null) return sprite;
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (s.name == name)
                {
                    sprite = s;
                    break;
                }
            }
            if (sprite == null) Plugin.Logger.LogWarning("no sprite called " + name);
            sprites[name] = sprite;
            return sprite;
        }

        public static string OneLine(string s)
        {
            return s.Replace("\n", " ").Trim();
        }

        public static Color Hex(string hex)
        {
            Color c = Color.magenta;
            bool ok = ColorUtility.TryParseHtmlString(hex, out c);
            if (ok == false)
            {
                c = Color.magenta;
            }
            return c;
        }
    }
}
