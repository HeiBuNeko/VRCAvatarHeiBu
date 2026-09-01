using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    [InitializeOnLoad]
    public static class LanguageManager
    {
        private const string KEY_LANGUAGE = "S_TC_LANGUAGE";

        internal static int Language
        {
            get
            {
                int index = EditorPrefs.GetInt(KEY_LANGUAGE, 0);
                return index;
            }
            set
            {
                EditorPrefs.SetInt(KEY_LANGUAGE, value);
            }
        }

        public class LanguageSetting : ScriptableSingleton<LanguageSetting>
        {
            public int languageIndex = -1;
            public string[] languageNames;
        }

        public static Dictionary<string, string> loc = new Dictionary<string, string>();

        internal static LanguageSetting ls => LanguageSetting.instance;

        public static int LanguageIndex => ls.languageIndex;
        public static string[] LanguageNames => ls.languageNames;

        static LanguageManager()
        {
            ls.languageIndex = Language;
            InitializeLanguage();
        }

        public static void InitializeLanguage()
        {
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
            LoadLanguageFile(AssetDatabase.GUIDToAssetPath("163238ea22bb0cc428dc1f62fb1bb7ab"));
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
                string value = texts[ls.languageIndex + 1].Replace(@"\n", "\n");
                loc.Add(id, value);
            }

            sr.Close();
        }

        public static string GetText(string id)
        {
            return loc.ContainsKey(id) ? loc[id] : id;
        }

        /// <summary>
        /// GUI用
        /// </summary>
        public static void DrawLanguagePopup()
        {
            int newLanguage = EditorGUILayout.Popup(GetText("t_Language"), LanguageIndex, LanguageNames);
            if (newLanguage != LanguageIndex)
            {
                ChangeLanguage(newLanguage);
            }
        }
    }
}