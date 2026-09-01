// --------------------------------------------------------------------------------
// This file contains implementations based on the source code of "lilToon",
// which is distributed under the following license.
//
// MIT License
// Copyright (c) 2025 - 2026 Satania
// https://github.com/lilxyzw/lilToon
// --------------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    public static class EditorGUIUtils
    {
        public const string BoothUrl = @"https://booth.pm/ja/items/4835743";

        // Thanks lilToon!
        // https://github.com/lilxyzw/lilToon/blob/efeba15cdcc35f87ef3a77997b7d4d6647df548b/Assets/lilToon/Editor/lilEditorGUI.cs#L155
        public static bool AutoFixHelpBox(string message, string autoFixMessage)
        {
            return HelpBoxWithButton(message, autoFixMessage, EditorGUIUtility.IconContent("console.warnicon"));
        }

        public static bool HelpBoxWithButton(string message, string autoFixMessage, GUIContent iconContent)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(iconContent, GUILayout.ExpandWidth(false));
            GUILayout.Space(-EditorStyles.label.fontSize);
            GUILayout.BeginVertical();
            GUILayout.Label(message, EditorStyles.wordWrappedMiniLabel);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool pressed = GUILayout.Button(autoFixMessage);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            return pressed;
        }

        /// <summary>
        /// さたにあ式タバコのBOOTHページを開きます。
        /// </summary>
        public static void OpenBoothUrl() => Help.BrowseURL(BoothUrl);
    }
}