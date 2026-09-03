// https://github.com/anatawa12/AvatarOptimizer/blob/d7178a4df250dae5ef33b05ed60c8e35f49879f9/Editor/Processors/DupliacteAssets.cs#L152-L188
// Originally under MIT License
// Copyright(c) 2022 anatawa12

using System.Reflection;

namespace Anatawa12.AvatarOptimizer;

internal static class MaterialEditorReflection
{
    static MaterialEditorReflection()
    {
        DisableApplyMaterialPropertyDrawersPropertyInfo = typeof(EditorMaterialUtility).GetProperty(
            "disableApplyMaterialPropertyDrawers", BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    public static readonly PropertyInfo DisableApplyMaterialPropertyDrawersPropertyInfo;

    public static DisableApplyMaterialPropertyDisposable BeginNoApplyMaterialPropertyDrawers()
    {
        return new DisableApplyMaterialPropertyDisposable(true);
    }

    public static bool DisableApplyMaterialPropertyDrawers
    {
        get => (bool)DisableApplyMaterialPropertyDrawersPropertyInfo.GetValue(null);
        set => DisableApplyMaterialPropertyDrawersPropertyInfo.SetValue(null, value);
    }

    public readonly struct DisableApplyMaterialPropertyDisposable : IDisposable
    {
        private readonly bool _originalValue;

        public DisableApplyMaterialPropertyDisposable(bool value)
        {
            _originalValue = DisableApplyMaterialPropertyDrawers;
            DisableApplyMaterialPropertyDrawers = value;
        }

        public void Dispose()
        {
            DisableApplyMaterialPropertyDrawers = _originalValue;
        }
    }
}