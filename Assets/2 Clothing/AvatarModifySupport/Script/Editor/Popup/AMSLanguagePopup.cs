using com.ams.avatarmodifysupport.gui;
using System;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.language
{
    internal enum eLanguage
    {
        Japanese,
        English,
        Korean
    }

    internal sealed class AMSLanguagePopup : PopupWindowContent
    {
        static string[] texts;
        static eLanguage index = 0;
        static event Action<eLanguage> onSelected;
        static Texture selectedTex;

        public static Vector2 size = new Vector2(200, 150);

        public AMSLanguagePopup(Vector2 _size, string[] _texts, eLanguage _index, Action<eLanguage> _callback)
        {
            if (selectedTex == null)
                selectedTex = AMSGuiUtility.MakeTex(1, 1, Color.cyan);

            size = _size;
            texts = _texts;
            index = _index;
            onSelected = _callback;
        }

        private bool DrawElementButton(string name)
        {
            var rect = GUILayoutUtility.GetRect(0, 0, GUILayout.MaxHeight(20));
            GUI.Label(rect, name);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                return true;
            }

            return false;
        }

        public void Clicked()
        {
            onSelected?.Invoke(index);
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(1);

            for (int i = 0; i < texts.Length; i++)
            {
                if (DrawElementButton(texts[i]))
                {
                    index = (eLanguage)i;
                    Clicked();
                    editorWindow.Close();
                }

                if (index == (eLanguage)i)
                {
                    float _w = 2;

                    var lastRect = GUILayoutUtility.GetLastRect();
                    lastRect = new Rect(
                        lastRect.x + lastRect.width - (_w / 2) - _w,
                        lastRect.y,
                        _w,
                        lastRect.height);

                    if (Event.current.type == EventType.Repaint)
                        EditorGUI.DrawTextureTransparent(lastRect, selectedTex);
                }
            }
            GUILayout.Space(1);
        }
    }
}