#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.colormodify;
using com.ams.avatarmodifysupport.setting;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.ams.avatarmodifysupport.colormodify.AMSColorModifyPresetGroup;

namespace com.ams.avatarmodifysupport
{
    internal class AMSSelectColorPreset : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }
        Vector2 size = new Vector2(250, 350);
        Vector2 scroll;

        List<AMSColorModifyPreset> colorPresets;

        GUIStyle richTextLabel;
        AMSPopupCallback callbacks;
        int[] selectedIndexes;
        bool[] isOpened;

        internal AMSSelectColorPreset(AMSPopupCallback _callback, List<ColorModifyPresetNamePair> pairs)
        {
            callbacks = _callback;

            colorPresets = amsSetting.colorPresets;

            int presetCount = colorPresets.Count;
            isOpened = new bool[presetCount];

            selectedIndexes = new int[presetCount];
            for (int i = 0; i < presetCount; i++)
            {
                selectedIndexes[i] = -1;
            }

            if (pairs != null)
            {
                foreach (var pair in pairs)
                {
                    var preset = colorPresets.Select((x, i) => new { Content = x, Index = i }).Where(x => x.Content.DisplayName.Equals(pair.targetPresetName));
                    if (!preset.Any())
                        continue;

                    int index = preset.First().Index;

                    var material = colorPresets[index].amsMaterials.Select((x, i) => new { Content = x, Index = i }).Where(x => x.Content.displayName.Equals(pair.targetColorName));
                    if (!material.Any())
                        continue;

                    selectedIndexes[index] = material.First().Index;
                }
            }
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            if (richTextLabel == null)
            {
                richTextLabel = new GUIStyle(GUI.skin.label);
                richTextLabel.richText = true;
            }

            if (GUILayout.Button(amsSetting.texts.select))
            {
                List<ColorModifyPresetNamePair> list = new List<ColorModifyPresetNamePair>();
                for (int i = 0; i < colorPresets.Count; i++)
                {
                    var p = colorPresets[i];
                    if (selectedIndexes[i] == -1)
                        continue;

                    if (p == null || p.amsMaterials.Count == 0)
                        continue;

                    list.Add(new ColorModifyPresetNamePair(p.DisplayName, p.amsMaterials[selectedIndexes[i]].displayName));
                }

                callbacks.onClickColorPreset?.Invoke(list);
                editorWindow.Close();
            }

            GUILayout.Space(5);

            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < amsSetting.colorPresets.Count; i++)
            {
                isOpened[i] = EditorGUILayout.Foldout(isOpened[i], amsSetting.colorPresets[i].DisplayName);
                if (isOpened[i])
                {
                    EditorGUI.indentLevel++;
                    for (int c = 0; c < amsSetting.colorPresets[i].amsMaterials.Count; c++)
                    {
                        var material = amsSetting.colorPresets[i].amsMaterials[c];

                        bool toggle = selectedIndexes[i] == c;
                        EditorGUI.BeginChangeCheck();
                        toggle = EditorGUILayout.ToggleLeft($"<color={material.colorCode}>●</color> {material.displayName}", toggle, richTextLabel);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (toggle)
                                selectedIndexes[i] = c;
                            else
                                selectedIndexes[i] = -1;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndScrollView();
        }
    }
}
#endif