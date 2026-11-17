#if VRC_SDK_VRCSDK3
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using JetBrains.Annotations;
using VRC.SDK3.Avatars.Components;

public partial class AvatarDescriptorEditor3 : Editor
{

    private static EditorAnimLayerInfo[] baseAnimLayerTypes = new EditorAnimLayerInfo[]
    {
        new EditorAnimLayerInfo("Locomotion", VRCAvatarDescriptor.AnimLayerType.Base),
        new EditorAnimLayerInfo("Idle", VRCAvatarDescriptor.AnimLayerType.Additive),
        new EditorAnimLayerInfo("Gesture", VRCAvatarDescriptor.AnimLayerType.Gesture),
        new EditorAnimLayerInfo("Action", VRCAvatarDescriptor.AnimLayerType.Action),
        new EditorAnimLayerInfo("Non-Transform", VRCAvatarDescriptor.AnimLayerType.FX),
    };

    private static EditorAnimLayerInfo[] specialAnimLayerTypes = new EditorAnimLayerInfo[]
    {
        new EditorAnimLayerInfo("Sitting", VRCAvatarDescriptor.AnimLayerType.Sitting),
        new EditorAnimLayerInfo("TPose", VRCAvatarDescriptor.AnimLayerType.TPose),
        new EditorAnimLayerInfo("IKPose", VRCAvatarDescriptor.AnimLayerType.IKPose),
    };

    Dictionary<VRCAvatarDescriptor.AnimLayerType, string> _animLayerButtonNames = new Dictionary<VRCAvatarDescriptor.AnimLayerType, string>();

    struct EditorAnimLayerInfo
    {
        public EditorAnimLayerInfo(string label, VRCAvatarDescriptor.AnimLayerType type)
        {
            this.label = label;
            this.type = type;
        }

        public string label;
        public VRCAvatarDescriptor.AnimLayerType type;
    }

    string GetAnimLayerButtonName(VRCAvatarDescriptor.AnimLayerType layerType)
    {
        string name = null;
        _animLayerButtonNames.TryGetValue(layerType, out name);
        if (string.IsNullOrEmpty(name))
        {
            foreach (EditorAnimLayerInfo info in baseAnimLayerTypes)
            {
                if (info.type == layerType)
                {
                    if (_animator && (!_animator.isHuman) && info.label == "Face")
                        name = "Default BlendShapes";
                    else
                        name = ("Default " + info.label);
                    _animLayerButtonNames.Add(layerType, name);
                    return name;
                }
            }
            foreach (EditorAnimLayerInfo info in specialAnimLayerTypes)
            {
                if (info.type == layerType)
                {
                    name = ("Default " + info.label);
                    _animLayerButtonNames.Add(layerType, name);
                    return name;
                }
            }
        }
        return name;
    }

    private static bool IsHumanOnlyAnimLayer(VRCAvatarDescriptor.AnimLayerType layerType)
    {
        switch (layerType)
        {
            case VRCAvatarDescriptor.AnimLayerType.Additive: return true;
            case VRCAvatarDescriptor.AnimLayerType.Gesture: return true;
        }
        return false;
    }

    void ResetAnimLayersToDefault()
    {
        VRCAvatarDescriptor descriptor = (VRCAvatarDescriptor)target;
        Animator animator = _animator;
        InitializeBaseAnimationLayersArray(ref descriptor.baseAnimationLayers, false, animator);
        InitializeBaseAnimationLayersArray(ref descriptor.specialAnimationLayers, true, animator);

        serializedObject.Update();
    }

    internal static void InitializeBaseAnimationLayersArray(ref VRCAvatarDescriptor.CustomAnimLayer[] animationLayers, bool isSpecial, Animator animator)
    {
        EditorAnimLayerInfo[] animLayerTypes = isSpecial ? specialAnimLayerTypes : baseAnimLayerTypes;
        List<VRCAvatarDescriptor.CustomAnimLayer> newLayers = new List<VRCAvatarDescriptor.CustomAnimLayer>();
        foreach (EditorAnimLayerInfo info in animLayerTypes)
        {
            if (animator && (!animator.isHuman) && IsHumanOnlyAnimLayer(info.type))
                continue;

            InitAnimLayer(newLayers, info.type, true);
        }
        animationLayers = newLayers.ToArray();
    }

    internal static void InitializeBaseAnimationLayersArray(SerializedProperty animationLayersList, bool isSpecial, Animator animator)
    {
        EditorAnimLayerInfo[] animLayerTypes = isSpecial ? specialAnimLayerTypes : baseAnimLayerTypes;
        animationLayersList.ClearArray();
        foreach (EditorAnimLayerInfo info in animLayerTypes)
        {
            if (animator && (!animator.isHuman) && IsHumanOnlyAnimLayer(info.type))
                continue;

            InitAnimLayer(animationLayersList, info.type, true);
        }
    }

    [UsedImplicitly]
    internal static void SetLayerMaskFromController(SerializedProperty layer)
    {
// VRC DaveT: avoid exception when observing during runtime
#if !VRC_CLIENT
        var isDefault = layer.FindPropertyRelative("isDefault").boolValue;
        if (!isDefault)
        {
            var maskProp = layer.FindPropertyRelative("mask");
            var controller = layer.FindPropertyRelative("animatorController").objectReferenceValue as AnimatorController;
            if (controller != null)
            {
                maskProp.objectReferenceValue = controller.layers[0].avatarMask;
            }
        }
#endif
    }

    private static void InitAnimLayer(List<VRCAvatarDescriptor.CustomAnimLayer> list, VRCAvatarDescriptor.AnimLayerType type, bool isDefault, int index = -1)
    {
        int position = index == -1 ? list.Count : index;

        VRCAvatarDescriptor.CustomAnimLayer element = new VRCAvatarDescriptor.CustomAnimLayer();
        element.type = type;
        element.isDefault = isDefault;

        list.Insert(position, element);
    }

    private static void InitAnimLayer(SerializedProperty list, VRCAvatarDescriptor.AnimLayerType type, bool isDefault, int index = -1)
    {
        int position = index == -1 ? list.arraySize : index;
        list.InsertArrayElementAtIndex(position);
        var element = list.GetArrayElementAtIndex(position);

        element.FindPropertyRelative("type").enumValueIndex = (int)type;
        element.FindPropertyRelative("isDefault").boolValue = isDefault;
    }

    void EnforceAnimLayerSetup(bool isOnEnable = false)
    {
        if (_baseAnimLayers.arraySize == 0 || _specialAnimLayers.arraySize == 0)
        {
            if (_baseAnimLayers.arraySize == 0)
            {
                InitializeBaseAnimationLayersArray(_baseAnimLayers, false, _animator);
            }
            if (_specialAnimLayers.arraySize == 0)
            {
                InitializeBaseAnimationLayersArray(_specialAnimLayers, true, _animator);
            }

            serializedObject.ApplyModifiedProperties();
            return;
        }

        if (_animator)
        {
            if (_animator.isHuman)
            {
                // human -> add missing base layers from previous non-human setup
                bool haveAdditive = false;
                bool haveGesture = false;

                for (int v = 0; v < _baseAnimLayers.arraySize; v++)
                {
                    SerializedProperty layer = _baseAnimLayers.GetArrayElementAtIndex(v);
                    SerializedProperty type = layer.FindPropertyRelative("type");
                    if ((VRCAvatarDescriptor.AnimLayerType)type.enumValueIndex == VRCAvatarDescriptor.AnimLayerType.Additive)
                    {
                        haveAdditive = true;
                    }
                    if ((VRCAvatarDescriptor.AnimLayerType)type.enumValueIndex == VRCAvatarDescriptor.AnimLayerType.Gesture)
                    {
                        haveGesture = true;
                        SetLayerMaskFromController(layer);
                    }
                    if ((VRCAvatarDescriptor.AnimLayerType)type.enumValueIndex == VRCAvatarDescriptor.AnimLayerType.FX)
                    {
                        SetLayerMaskFromController(layer);
                    }
                }

                int index = 1;
                if (!haveAdditive)
                    InitAnimLayer(_baseAnimLayers, VRCAvatarDescriptor.AnimLayerType.Additive, true, index++);

                if (!haveGesture)
                    InitAnimLayer(_baseAnimLayers, VRCAvatarDescriptor.AnimLayerType.Gesture, true, index++);
            }
            else
            {
                // non-human -> remove excess base layers from previous human setup
                List<int> baseLayersToDelete = new List<int>();
                for (int v = 0; v < _baseAnimLayers.arraySize; v++)
                {
                    SerializedProperty layer = _baseAnimLayers.GetArrayElementAtIndex(v);
                    SerializedProperty type = layer.FindPropertyRelative("type");
                    VRCAvatarDescriptor.AnimLayerType t = (VRCAvatarDescriptor.AnimLayerType)type.enumValueIndex;
                    if (IsHumanOnlyAnimLayer(t))
                        baseLayersToDelete.Add(v);
                }
                if (baseLayersToDelete.Count > 0)
                    DeleteAnimLayers(_baseAnimLayers, baseLayersToDelete);
            }
        }

        // reset 'Special' layer buttons to default when left empty
        for (int v = 0; v < _specialAnimLayers.arraySize; v++)
        {
            SerializedProperty layer = _specialAnimLayers.GetArrayElementAtIndex(v);
            SerializedProperty type = layer.FindPropertyRelative("type");
            SerializedProperty isDefault = layer.FindPropertyRelative("isDefault");
            SerializedProperty animatorController = layer.FindPropertyRelative("animatorController");

            if ((!isDefault.boolValue) && (animatorController.objectReferenceValue == null))
            {
                if (isOnEnable || (GUI.GetNameOfFocusedControl() != GetAnimLayerButtonName((VRCAvatarDescriptor.AnimLayerType)type.enumValueIndex)))
                    isDefault.boolValue = true;
            }
        }
    }

}
#endif
