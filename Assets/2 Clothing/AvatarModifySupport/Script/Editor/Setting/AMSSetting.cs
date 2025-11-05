#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.behaviour;
using com.ams.avatarmodifysupport.colormodify;
using com.ams.avatarmodifysupport.language;
using com.ams.avatarmodifysupport.preset;

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace com.ams.avatarmodifysupport.setting
{
    public class AMSSetting : ScriptableSingleton<AMSSetting>
    {
        internal List<AMSBlendshapePreset> blendshapePresets = new List<AMSBlendshapePreset>();
        internal List<AMSColorModifyPreset> colorPresets = new List<AMSColorModifyPreset>();
        internal List<AMSColorModifyPresetGroup> colorPresetGroups = new List<AMSColorModifyPresetGroup>();

        internal VRCAvatarDescriptor Avatar;
        internal GameObject AvatarGO;
        internal AvatarModifySupportBehaviour Behaviour;

        /// <summary>
        /// ブレンドシェイプをもったレンダラーのみ
        /// </summary>
        internal List<AMSSkinnedMeshRenderer> renderers;
        /// <summary>
        /// アバターの全てのSkinnedMeshRenderer
        /// </summary>
        internal List<AMSSkinnedMeshRenderer> allRenderers;
        internal List<AMSSkinnedMeshRenderer> fbxRenderers;
        internal List<SkinnedMeshRenderer> selectedRenderers;
        internal string fbxPath;

        internal bool repaintFlag = false;
        internal bool saveFlag = false;
        internal bool isWindowOpened = false;
        internal bool IsColorGroupPresetOpened = true;

        internal Vector2 presetMenuScroll;
        internal Vector2 blendshapeListScroll;
        internal Vector2 colorModifyListScroll;
        internal Vector2 colorGroupListScroll;

        internal AMSLanguage[] Languages;
        internal AMSLanguage texts;

        internal int pageIndex = 0;
    }
}
#endif