using System.Reflection;
using System.Reflection.Emit;

namespace io.github.azukimochi;

internal readonly ref struct PropertyScope
{
    public readonly GUIContent Label;
    private static readonly Action DisableBoldFont;

    static PropertyScope()
    {
        var method = new DynamicMethod(nameof(DisableBoldFont), null, Type.EmptyTypes);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Call, typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", BindingFlags.Static | BindingFlags.NonPublic));
        il.Emit(OpCodes.Ret);
        DisableBoldFont = method.CreateDelegate(typeof(Action)) as Action;
    }

    public PropertyScope(Rect totalPosition, GUIContent label, SerializedProperty property)
    {
        Label = EditorGUI.BeginProperty(totalPosition, label, property);
        DisableBoldFont();
    }

    public void Dispose() => EditorGUI.EndProperty();
}
