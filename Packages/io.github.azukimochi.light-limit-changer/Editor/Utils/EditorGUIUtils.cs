using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace io.github.azukimochi;

internal abstract class EditorGUIUtils
{
    public readonly static Func<MessageType, Texture2D> GetHelpIcon;

    static EditorGUIUtils()
    {
        var method = new DynamicMethod(nameof(GetHelpIcon), typeof(Texture2D), new[] { typeof(MessageType) }, typeof(EditorGUIUtility), true);
        var il = method.GetILGenerator();
        il.Ldarg(0);
        il.Emit(OpCodes.Call, typeof(EditorGUIUtility).GetMethod(nameof(GetHelpIcon), BindingFlags.NonPublic | BindingFlags.Static));
        il.Emit(OpCodes.Ret);
        GetHelpIcon = method.CreateDelegate(typeof(Func<MessageType, Texture2D>)) as Func<MessageType, Texture2D>;
    }

    public static void ObjectDisplayField(Rect position, Object obj, Type objType)
    {
        Event current = Event.current;
        EventType eventType = current.type;
        if (!GUI.enabled  && Event.current.rawType == EventType.MouseDown)
        {
            eventType = Event.current.rawType;
        }

        Vector2 iconSize = EditorGUIUtility.GetIconSize();
        EditorGUIUtility.SetIconSize(new Vector2(12f, 12f));

        switch (eventType)
        {
            case EventType.MouseDown:
                {
                    if (!position.Contains(Event.current.mousePosition) || Event.current.button != 0)
                    {
                        break;
                    }

                    if (Event.current.clickCount == 1)
                    {
                        EditorGUIUtility.PingObject(obj);
                        current.Use();
                    }
                    else if (Event.current.clickCount == 2)
                    {
                        AssetDatabase.OpenAsset(obj);
                        current.Use();
                        GUIUtility.ExitGUI();
                    }

                    break;
                }
            case EventType.Repaint:
                {
                    GUIContent content = EditorGUIUtility.ObjectContent(obj, objType); 
                    EditorStyles.objectField.Draw(position, content, position.Contains(Event.current.mousePosition), true, false, false);
                }
                break;
        }

        EditorGUIUtility.SetIconSize(iconSize);
    }


    public static void MultipleObjectInputField<T>(Rect position, GUIContent label, Action<T[]> callback) where T : Object
    {
        switch (Event.current.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                {
                    if (!GUI.enabled)
                        break;

                    if (!position.Contains(Event.current.mousePosition) || 
                        !DragAndDrop.objectReferences.Any(x => x is T))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        var references = DragAndDrop.objectReferences;
                        callback(references.Select(x => x as T).Where(x => x != null).ToArray());
                        DragAndDrop.activeControlID = 0;
                        if (references.Length != 0)
                            GUI.changed = true;
                    }
                    Event.current.Use();
                }
                return;
            case EventType.DragExited:
                {
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                }
                return;
            default:
                break;
        }
        var obj = EditorGUI.ObjectField(position, label, null, typeof(T), true) as T;
        if (obj is null || obj == null)
            return;
        callback(new[] { obj });
    }

    public static Rect GetCenteredRect(Rect position, Vector2 actualSize)
    {
        Vector2 v = new(position.width, position.height);
        var margin = v - actualSize;
        position.x += margin.x / 2;
        position.width -= margin.x / 2;
        position.y += margin.y / 2;
        position.height -= margin.y / 2;
        return position;
    }

    protected static GUIStyle HelpBoxStyle => helpBoxStyle ??= new(EditorStyles.helpBox)
    {
        fontSize = EditorStyles.label.fontSize,
        padding = new RectOffset(8, 8, 8, 8),
    };

    private static GUIStyle helpBoxStyle; 
}

internal abstract class EditorGUILayoutUtils : EditorGUIUtils
{
    public static void HelpBox(GUIContent content, MessageType type)
    {
        content.image = type switch
        {
            < (MessageType)RuntimeMessageType.Tips => GetHelpIcon(type),
            _ => AssetUtils.FromGUID<Texture2D>(Icons.Tips),
        };
        EditorGUILayout.LabelField(GUIContent.none, content, HelpBoxStyle);
    }

    public static string TextField(GUIContent label, string text, string placeholder = null)
    {
        text = EditorGUILayout.TextField(label, text);
        if (string.IsNullOrWhiteSpace(text))
        {
            var rect = GUILayoutUtility.GetLastRect();
            rect.x += EditorGUIUtility.labelWidth + 4;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.LabelField(rect, GUIContent.none, L10n.Tr(placeholder), EditorStyles.miniLabel);
            EditorGUI.EndDisabledGroup();
        }
        return text;
    }

    public static void MultipleObjectInputField<T>(GUIContent label, Action<T[]> callback) where T : Object
    {
        var rect = EditorGUILayout.GetControlRect(label != null, EditorGUIUtility.singleLineHeight);
        MultipleObjectInputField(rect, label, callback);
    }
}