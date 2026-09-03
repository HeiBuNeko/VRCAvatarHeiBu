
namespace io.github.azukimochi;

[CustomPropertyDrawer(typeof(TogglePopoutAttribute))]
internal sealed class TogglePopoutDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        PopupCheckbox(position, property, label);
    }
    public static void PopupCheckbox(Rect position, SerializedProperty property, GUIContent label)
    {
        using var __ = new PropertyScope(position, label, property);
        var value = property.boolValue;
        EditorGUI.BeginChangeCheck();
        value = PopupCheckbox(position, label, value);
        if (EditorGUI.EndChangeCheck())
        {
            property.boolValue = value;
        }
    }

    private static readonly GUIContent[] TogglePopupContents = new GUIContent[] { new(""), new("") };

    public static bool PopupCheckbox(Rect position, GUIContent label, bool value)
    {
        var contents = TogglePopupContents;
        _ = contents.Length;
        contents[0].text = L10n.TrStr("common:label/false");
        contents[1].text = L10n.TrStr("common:label/true");

        int index = value ? 1 : 0;
        EditorGUI.BeginChangeCheck();
        index = EditorGUI.Popup(position, label, index, contents);
        return index != 0;
    }
}