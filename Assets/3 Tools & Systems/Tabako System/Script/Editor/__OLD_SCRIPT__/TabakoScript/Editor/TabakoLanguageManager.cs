using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace satania.tabakosystem
{
    public static class TabakoLanguageManager
    {
        const string KEY_LANGUAGE = "S_TC_LANGUAGE";

        static int Language
        {
            get
            {
                //1 = jp, 0 = en
                int index = EditorPrefs.GetInt(KEY_LANGUAGE, Application.systemLanguage == SystemLanguage.Japanese ? 1 : 0);
                return (int)index;
            }
            set
            {
                EditorPrefs.SetInt(KEY_LANGUAGE, (int)value);
            }
        }

        public class LanguageSetting : ScriptableSingleton<LanguageSetting>
        {
            public int languageIndex = -1;
            public string[] languageNames;
        }

        public static Dictionary<string, string> loc = new Dictionary<string, string>();


        static LanguageSetting ls => LanguageSetting.instance;

        public static int LanguageIndex => ls.languageIndex;
        public static string[] LanguageNames => ls.languageNames;

        public static void InitializeLanguage()
        {
            ls.languageIndex = Language;
            UpdateLanguage();
        }

        public static void ChangeLanguage(int newIndex)
        {
            ls.languageIndex = newIndex;
            Language = newIndex;
            UpdateLanguage();
        }

        public static void UpdateLanguage()
        {
            LoadLanguageFile(AssetDatabase.GUIDToAssetPath("855735e633cbdff46a3baa23230495a2"));
        }

        public static void LoadLanguageFile(string path)
        {
            loc.Clear();

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            StreamReader sr = new StreamReader(path);

            string str = sr.ReadLine();
            ls.languageNames = str.Substring(str.IndexOf("\t") + 1).Split("\t");

            while ((str = sr.ReadLine()) != null)
            {
                var texts = str.Split('\t');
                string id = texts[0];
                string value = texts[ls.languageIndex + 1];
                loc.Add(id, value);
            }

            sr.Close();
        }

        public static string GetText(string id)
        {
            return loc.ContainsKey(id) ? loc[id] : id;
        }
    }
}