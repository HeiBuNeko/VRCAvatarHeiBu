#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.ams.avatarmodifysupport.colormodify
{
    public class AMSColorModifyPresetGroup
    {
        public string groupName = "";
        public List<ColorGroup> colors = new List<ColorGroup>();

        [JsonIgnore]
        public Vector2 scroll;

        [JsonIgnore]
        public bool isOpened;

        public AMSColorModifyPresetGroup(string name)
        {
            groupName = name;
        }

        public class ColorModifyPresetNamePair
        {
            public string targetPresetName = "";
            public string targetColorName = "";

            public ColorModifyPresetNamePair()
            {

            }

            public ColorModifyPresetNamePair(string presetName, string colorName)
            {
                targetPresetName = presetName;
                targetColorName = colorName;
            }
        }

        public class ColorGroup
        {
            public string colorCode = "";
            public string presetName = "";
            public List<ColorModifyPresetNamePair> targets = new List<ColorModifyPresetNamePair>();

            public void InvokePreset(List<AMSColorModifyPreset> colorPresets)
            {
                foreach (var target in targets)
                {
                    var presets = colorPresets.Select((x, i) => new { Content = x, Index = i }).Where(x => x.Content.DisplayName.Equals(target.targetPresetName));
                    if (!presets.Any())
                        continue;

                    var preset = presets.FirstOrDefault().Content;
                    var colors = preset.amsMaterials.Select((x, i) => new { Content = x, Index = i }).Where(x => x.Content.displayName.Equals(target.targetColorName));
                    if (!colors.Any())
                        continue;

                    var color = colors.FirstOrDefault();

                    if (preset.TrySetMaterial(color.Index))
                    {

                    }
                }
            }

            public ColorGroup(string _colorCode, string _presetName, List<ColorModifyPresetNamePair> targetPairs)
            {
                colorCode = _colorCode;
                presetName = _presetName;
                targets = targetPairs;
            }
        }

        public bool IsExists(string colorName, string presetName)
        {
            return colors.Where(x => x.colorCode.Equals(colorName) && x.presetName.Equals(presetName)).Any();
        }

        public void RemoveIfInvalidTarget(List<AMSColorModifyPreset> colorPresets)
        {
            List<Action> act = new List<Action>();

            foreach (var c in colors)
            {
                foreach (var target in c.targets)
                {
                    if (!colorPresets.Where(
                        x => x.DisplayName.Equals(target.targetPresetName) &&
                        x.amsMaterials.Where(x => x.displayName.Equals(target.targetColorName)).Any()).Any())
                    {
                        act.Add(() =>
                        {
                            c.targets.Remove(target);
                        });

                        //target内が空になる場合は削除
                        if (c.targets.Count == 1)
                        {
                            act.Add(() =>
                            {
                                colors.Remove(c);
                            });
                        }
                    }
                }
            }

            foreach (var a in act)
                a?.Invoke();
        }

        public void Add(string colorCode, string presetName, List<ColorModifyPresetNamePair> targetPairs)
        {
            colors.Add(new ColorGroup(colorCode, presetName, targetPairs));
        }

        public void Add(ColorGroup group)
        {
            colors.Add(group);
        }

        public void Remove(ColorGroup group)
        {
            int index = colors.IndexOf(group);
            colors.RemoveAt(index);
        }
    }
}
#endif