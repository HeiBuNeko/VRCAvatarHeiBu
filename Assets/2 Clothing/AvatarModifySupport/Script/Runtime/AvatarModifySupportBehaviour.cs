#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
#define LOADED_AVATAR_MODIFY_SUPPORT
#endif

//Avatar Modify Support Terms of Use
//Copyright(c) 2024 AvatarModifySupport Team

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to use the Software
//without restriction, including the rights to copy, modify, merge, publish, distribute, and sublicense copies of the Software. This permission allows others who receive
//the Software to do the same, subject to the following conditions:

//The above copyright notice and this permission notice must be included in all copies or substantial portions of the Software.

//Modification of the Software is permitted; however, redistribution of modified versions is prohibited. Redistribution is only permitted in its original, unmodified form.
//Redistribution of the Software is allowed when it is provided as part of a paid 3D model for VRChat (a social virtual reality platform), even if the result is a paid
//distribution.

//THE SOFTWARE IS PROVIDED “AS IS,” WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE, AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT, OR OTHERWISE, ARISING FROM OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#if UNITY_EDITOR
#region deps
using UnityEngine;
using VRC.SDKBase;

using VRC.SDK3.Avatars.Components;

#if UNITY_2022_1_OR_NEWER
using System.Collections.Generic;
using com.ams.avatarmodifysupport.preset;
using com.ams.avatarmodifysupport.colormodify;
using Newtonsoft.Json;
#endif
#endregion

namespace com.ams.avatarmodifysupport.behaviour
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VRCAvatarDescriptor))]
    [AddComponentMenu("AvatarModifySupport/AMS AvatarModifySupport")]
    public sealed class AvatarModifySupportBehaviour : MonoBehaviour, IEditorOnly
    {
        /// <summary>
        /// ブレンドシェイププリセット用
        /// </summary>
        public string exportedData = "";

        /// <summary>
        /// カラープリセット用
        /// </summary>
        public string exportedColorData = "";

        /// <summary>
        /// カラープリセットを一気に適用するためのプリセット用
        /// </summary>
        public string exportedColorGroupData = "";

        /// <summary>
        /// 最後にBehaviourを編集したバージョン
        /// </summary>
        public string lastEditedVersion = "";

#if UNITY_2022_1_OR_NEWER
        public List<AMSBlendshapePreset> blendshapePresets = new List<AMSBlendshapePreset>();
        public List<AMSColorModifyPreset> colorPresets = new List<AMSColorModifyPreset>();
        public List<AMSColorModifyPresetGroup> colorPresetGroups = new List<AMSColorModifyPresetGroup>();
#endif
        public VRCAvatarDescriptor avatar
        {
            get => GetComponent<VRCAvatarDescriptor>();
        }

#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
        /// <summary>
        /// JSON読み込み後に必要なデータを再生成
        /// </summary>
        public void LoadData()
        {
            if (!string.IsNullOrEmpty(exportedData))
            {
                blendshapePresets = JsonConvert.DeserializeObject<List<AMSBlendshapePreset>>(exportedData);

                if (blendshapePresets.Count > 0)
                {
                    foreach (var p in blendshapePresets)
                        foreach (var g in p.blendshapePresetGroups)
                        {
                            g.LoadRenderer(avatar.transform.root);
                        }
                }
            }

            if (!string.IsNullOrEmpty(exportedColorData))
            {
                colorPresets = JsonConvert.DeserializeObject<List<AMSColorModifyPreset>>(exportedColorData);

                List<System.Action> actions = new List<System.Action>();

                foreach (var p in colorPresets)
                {
                    if (!p.Load(transform))
                    {
                        actions.Add(() =>
                        {
                            colorPresets.Remove(p);
                        });
                    }
                }

                foreach (var act in actions)
                    act?.Invoke();
            }

            if (!string.IsNullOrEmpty(exportedColorGroupData))
            {
                colorPresetGroups = JsonConvert.DeserializeObject<List<AMSColorModifyPresetGroup>>(exportedColorGroupData);
            }
        }
#endif
    }
}
#endif