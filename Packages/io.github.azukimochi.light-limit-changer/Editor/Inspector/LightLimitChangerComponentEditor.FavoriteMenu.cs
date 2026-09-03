#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

namespace io.github.azukimochi;

partial class LightLimitChangerComponentEditor
{
    private static Dictionary<string, bool>? favoriteMenuCategoryOpenStatus;
    private ReorderableList? reorderableFavoriteIdList;
    private SerializedProperty? favoriteIdsProperty;

    private void DrawFavoriteMenuGUI()
    {
        EditorGUILayout.Space();
        CategoryLabel(L10n.TrStr("favorite-menu:category/title"));

        EditorGUILayout.Space();

        favoriteIdsProperty ??= serializedObject.FindProperty($"{nameof(LightLimitChangerComponent.MenuSettings)}.{nameof(LightLimitChangerComponent.MenuSettings.FavoriteParameterIds)}");
        bool isOpen = favoriteIdsProperty.isExpanded;

        using (var header = new ShurikenHeaderGroupScope(ref isOpen, L10n.TrStr("common:label/open"), false))
        {
            favoriteIdsProperty.isExpanded = isOpen;
            if (!header.IsOpened)
                return;

            Visit(Metadata.Root);

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 2f);

            InitList();
            reorderableFavoriteIdList?.DoLayoutList();
        }

        EditorGUILayoutUtils.HelpBox(L10n.Tr("favorite-menu:category/description"), MessageType.Info);

        void Visit(Metadata metadata)
        {
            var list = Component.MenuSettings.FavoriteParameterIds;
            if (metadata is Metadata.ParameterInfo parameter)
            {
                var p = parameter.Get(Component);
                var groupEnable = parameter.Parent!.GetEnable(Component);
                bool disabled = !groupEnable || !p.IsAnimated;
                bool value = list.Contains(parameter.Id);

                if (disabled)
                {
                    var rect = EditorGUILayout.GetControlRect();

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.showMixedValue = value;
                    EditorGUI.ToggleLeft(rect, L10n.TrStr($"settings:{metadata.Id}/label"), false);
                    EditorGUI.showMixedValue = false;
                    EditorGUI.EndDisabledGroup();

                    if (groupEnable && p.Enable)
                    {
                        var reason = L10n.Tr("favorite-menu:label/not-animated");
                        var style = EditorStyles.miniLabel;
                        var width = style.CalcSize(reason).x;
                        rect.x = rect.x + rect.width - width;
                        rect.width = width;

                        EditorGUI.LabelField(rect, reason, style);
                    }

                    return;
                }

                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.ToggleLeft(L10n.TrStr($"settings:{metadata.Id}/label"), value);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Component, "Change favorite menu");
                    if (value)
                    {
                        list.Add(parameter.Id);
                    }
                    else
                    {
                        list.Remove(parameter.Id);
                    }
                    EditorUtility.SetDirty(Component);
                }
            }
            else if (metadata is Metadata.SettingsInfo settings)
            {
                if (settings.Children.IsEmpty)
                    return;

                var openStatus = favoriteMenuCategoryOpenStatus ??= new();

                if (metadata.Id is not ("root" or Metadata.ID.General.Id))
                {
                    bool toggle = openStatus.GetOrAdd(settings.Id, _ => false);
                    bool toggle2 = toggle;

                    using var scope = new ShurikenHeaderGroupScope(ref toggle, L10n.TrStr($"category:{settings.Id}"), true);
                    if (toggle != toggle2)
                    {
                        openStatus[settings.Id] = toggle;
                    }
                    if (!toggle)
                        return;

                    foreach (var child in settings.Parameters)
                    {
                        Visit(child);
                    }

                    if (!settings.Parameters.IsEmpty && !settings.Settings.IsEmpty)
                        EditorGUILayout.Space();

                    foreach (var child in settings.Settings)
                    {
                        Visit(child);
                    }
                }
                else
                {
                    foreach (var child in settings.Children)
                    {
                        Visit(child);
                    }
                }
            }
        }

        void InitList()
        {
            reorderableFavoriteIdList ??= new(serializedObject, favoriteIdsProperty)
            {
                displayAdd = false,
                displayRemove = false,
                footerHeight = 0,
                drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, L10n.Tr("common:label/sorting")),
                drawElementCallback = DrawElementCallback,
            };

            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                var list = Component.MenuSettings.FavoriteParameterIds;
                if (index >= list.Count)
                    return;
                var parameter = Metadata.GetMetadataById(list[index]) as Metadata.ParameterInfo;

                bool disabled = parameter == null || parameter.Parent!.GetEnable(Component) == false || parameter.Get(Component).IsAnimated == false;
                EditorGUI.BeginDisabledGroup(disabled);

                EditorGUI.LabelField(rect, GetNameFromIndex(index));

                EditorGUI.EndDisabledGroup();
            }

            string GetNameFromIndex(int index)
            {
                var list = Component.MenuSettings.FavoriteParameterIds;
                if (index >= list.Count)
                    return "";
                var id = list[index];
                return L10n.TrStr($"settings:{id}/label", id);
            }
        }
    }
}
