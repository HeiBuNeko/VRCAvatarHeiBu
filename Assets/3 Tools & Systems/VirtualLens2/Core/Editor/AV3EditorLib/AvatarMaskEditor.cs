using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VirtualLens2.AV3EditorLib
{
    /// <summary>
    /// Utility functions for manipulating avatar masks.
    /// </summary>
    public static class AvatarMaskEditor
    {
        /// <summary>
        /// Merges two avatar masks.
        /// </summary>
        /// <param name="destination">The avatar mask to be modified.</param>
        /// <param name="source">The avatar mask to be merged with the <c>destination</c>.</param>
        /// <exception cref="ArgumentNullException"><c>destination</c> or <c>source</c> is <c>null</c>.</exception>
        public static void Merge(AvatarMask destination, AvatarMask source)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            var so = new SerializedObject(destination);
            var maskProp = so.FindProperty("m_Mask");
            for (var i = AvatarMaskBodyPart.Root; i < AvatarMaskBodyPart.LastBodyPart; ++i)
            {
                if (source.GetHumanoidBodyPartActive(i))
                {
                    maskProp.GetArrayElementAtIndex((int)i).intValue = 1;
                }
            }
            var elementsProp = so.FindProperty("m_Elements");
            var index = elementsProp.arraySize;
            for (var i = 0; i < source.transformCount; ++i)
            {
                elementsProp.InsertArrayElementAtIndex(index);
                var element = elementsProp.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("m_Path").stringValue = source.GetTransformPath(i);
                element.FindPropertyRelative("m_Weight").floatValue = source.GetTransformActive(i) ? 1.0f : 0.0f;
                ++index;
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Renders template strings in transform paths.
        /// </summary>
        /// <seealso cref="StringTemplateEngine"/>
        /// <param name="mask">The avatar mask to be modified.</param>
        /// <param name="parameters">Key-value pairs used for rendering templates.</param>
        /// <exception cref="ArgumentNullException"><c>mask</c> or <c>parameters</c> is <c>null</c>.</exception>
        public static void ProcessTemplate(AvatarMask mask, IDictionary<string, string> parameters)
        {
            if (mask == null) { throw new ArgumentNullException(nameof(mask)); }
            if (parameters == null) { throw new ArgumentNullException(nameof(parameters)); }
            var engine = new StringTemplateEngine(parameters);

            var so = new SerializedObject(mask);
            var elementsProp = so.FindProperty("m_Elements");
            var elementsCount = elementsProp.arraySize;
            for (var i = 0; i < elementsCount; ++i)
            {
                var element = elementsProp.GetArrayElementAtIndex(i);
                var pathProp = element.FindPropertyRelative("m_Path");
                pathProp.stringValue = engine.Render(pathProp.stringValue);
            }
            so.ApplyModifiedProperties();
        }
    }

}
