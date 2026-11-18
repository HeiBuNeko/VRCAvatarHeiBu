using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    public class RomajiReadingWindow : EditorWindow
    {
        private string _input = "";
        private string _kana = "";
        private List<string> _results = new List<string>();
        private string _message;
        private MessageType _messageType = MessageType.Info;
        private Vector2 _scroll;
        private bool _showDictionary;
        private Action<string> _onAdd;

        private static string L(string id) => LanguageManager.GetText(id);

        [MenuItem(MenuItemDictionary.p_RomajiReading, priority = 36)]
        private static void OpenFromMenu()
        {
            Open(null, null);
        }

        internal static RomajiReadingWindow Open(string initialName, Action<string> onAdd)
        {
            var window = GetWindow<RomajiReadingWindow>();
            window.titleContent = new GUIContent(L("t_Romaji_Title"));
            window.minSize = new Vector2(420, 360);
            window._onAdd = onAdd;
            if (!string.IsNullOrEmpty(initialName))
            {
                window._input = initialName;
                window._kana = "";
                window._results.Clear();
                window._message = null;
                window.ReadOrCopyKana();
            }
            window.Show();
            return window;
        }

        private void OnGUI()
        {
            SatabakoEditorUtils.DrawHeader(100);
            GUILayout.Space(5);

            _input = EditorGUILayout.TextField(L("t_Romaji_Input"), _input);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!KanjiReadingProvider.IsAvailable || string.IsNullOrWhiteSpace(_input)))
                {
                    if (GUILayout.Button(L("t_Romaji_ReadKanji")))
                        ReadOrCopyKana();
                }
            }

            if (!KanjiReadingProvider.IsAvailable)
                EditorGUILayout.HelpBox(L("t_Romaji_MsgNoDictionary"), MessageType.Info);

            _kana = EditorGUILayout.TextField(L("t_Romaji_Kana"), _kana);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_kana)))
            {
                if (GUILayout.Button(L("t_Romaji_Generate")))
                    Generate();
            }

            if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, _messageType);

            if (_results.Count > 0)
            {
                GUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(string.Format(L("t_Romaji_Results"), _results.Count), EditorStyles.boldLabel);
                    if (GUILayout.Button(L("t_Romaji_CopyAll"), GUILayout.Width(180)))
                        EditorGUIUtility.systemCopyBuffer = string.Join(", ", _results);
                }

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                foreach (var romaji in _results)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.SelectableLabel(romaji, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        if (GUILayout.Button(L("t_Romaji_Copy"), GUILayout.Width(70)))
                            EditorGUIUtility.systemCopyBuffer = romaji;
                        if (_onAdd != null && GUILayout.Button(L("t_Romaji_Add"), GUILayout.Width(60)))
                            _onAdd(romaji);
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();
            DrawDictionarySettings();
            LanguageManager.DrawLanguagePopup();
        }

        private void DrawDictionarySettings()
        {
            _showDictionary = EditorGUILayout.Foldout(_showDictionary, L("t_Romaji_Dictionary"), true);
            if (!_showDictionary)
                return;

            EditorGUI.indentLevel++;
            string dir = KanjiReadingProvider.DicDir;
            EditorGUILayout.LabelField(KanjiReadingProvider.HasLibrary ? "LibNMeCab: OK" : "LibNMeCab: -", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(string.IsNullOrEmpty(dir) ? "-" : dir, EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(L("t_Romaji_PickFolder"), GUILayout.Width(80)))
                {
                    string picked = EditorUtility.OpenFolderPanel(L("t_Romaji_Dictionary"), string.IsNullOrEmpty(dir) ? Application.dataPath : dir, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        if (KanjiReadingProvider.IsDicDir(picked))
                            KanjiReadingProvider.DicDir = picked;
                        else
                            EditorUtility.DisplayDialog(L("t_Romaji_Title"), L("t_Romaji_MsgNotDicDir"), "OK");
                    }
                }
                if (GUILayout.Button(L("t_Romaji_AutoDetect"), GUILayout.Width(80)))
                {
                    KanjiReadingProvider.DicDir = "";
                    KanjiReadingProvider.ResetAutoDetect();
                }
            }
            EditorGUI.indentLevel--;
        }

        private void ReadOrCopyKana()
        {
            _message = null;
            string normalized = KanaRomajiConverter.Normalize(_input);
            if (!KanaRomajiConverter.ContainsNonKana(normalized))
            {
                _kana = normalized;
                Generate();
                return;
            }

            if (!KanjiReadingProvider.IsAvailable)
            {
                _kana = "";
                return;
            }

            if (KanjiReadingProvider.TryToKana(normalized, out string kana, out string error))
            {
                _kana = kana;
                Generate();
            }
            else
            {
                _message = string.Format(L("t_Romaji_MsgReadFailed"), error);
                _messageType = MessageType.Warning;
            }
        }

        private void Generate()
        {
            _results = KanaRomajiConverter.ToRomajiVariants(_kana);
            if (_results.Count == 0)
            {
                _message = L("t_Romaji_MsgNoKana");
                _messageType = MessageType.Warning;
            }
            else
                _message = null;
            _scroll = Vector2.zero;
        }
    }
}
