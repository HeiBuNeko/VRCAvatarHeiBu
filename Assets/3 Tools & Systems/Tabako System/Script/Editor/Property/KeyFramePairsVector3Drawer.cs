using System;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    [CustomPropertyDrawer(typeof(KeyFramePairVector3))]
    public sealed class KeyFramePairsVector3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
            if (!property.isExpanded)
                return;

            SerializedProperty p_time = property.FindPropertyRelative("time");
            SerializedProperty p_value = property.FindPropertyRelative("value");

            EditorGUI.BeginProperty(rect, label, property);

            rect.height = EditorGUIUtility.singleLineHeight;

            rect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, p_time);

            rect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, p_value);

            EditorGUI.EndProperty();
        }

        /// プロパティの高さを取得する。カスタムによって高さが変わるなら必須
        /// https://qiita.com/ninomiya_shota/items/d38aa81b92d7c487b6aa
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return base.GetPropertyHeight(property, label);

            return base.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight * 2;
        }
    }
}
