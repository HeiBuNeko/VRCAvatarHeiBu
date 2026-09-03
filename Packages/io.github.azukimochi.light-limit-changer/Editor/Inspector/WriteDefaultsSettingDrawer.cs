
namespace io.github.azukimochi;

[CustomPropertyDrawer(typeof(WriteDefaultsSetting))]
internal sealed class WriteDefaultsSettingDrawer : PropertyDrawer
{
    private static readonly GUIContent[] displayContents = new GUIContent[] { new(), new(), new() };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using var propertyScope = new PropertyScope(position, label, property);
        var index = property.enumValueIndex;
        var contents = displayContents;
        _ = contents.Length;
        contents[0].text = L10n.TrStr("common:label/write-defaults/match-avatar");
        contents[1].text = L10n.TrStr("common:label/false");
        contents[2].text = L10n.TrStr("common:label/true");
        EditorGUI.BeginChangeCheck();
        index = EditorGUI.Popup(position, propertyScope.Label, index, contents);
        if (EditorGUI.EndChangeCheck())
        {
            property.enumValueIndex = index;
        }
    }
}
