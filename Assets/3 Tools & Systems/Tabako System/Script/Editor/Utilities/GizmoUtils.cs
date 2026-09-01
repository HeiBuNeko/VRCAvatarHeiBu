// --------------------------------------------------------------------------------
// This file contains implementations based on the source code of "modular-avatar",
// which is distributed under the following license.
//
// MIT License
// Copyright (c) 2022 bd_
// https://github.com/bdunderscore/modular-avatar
// --------------------------------------------------------------------------------

using System;
using System.Reflection;
using UnityEditor;
using net.satania_shopping.tabakosystem.runtime;

namespace net.satania_shopping.tabakosystem
{
    //https://github.com/bdunderscore/modular-avatar/blob/595c3f945e3194d5a0cbcbd9cb2a5d7a5faea5a1/Editor/Util.cs#L79
    [InitializeOnLoad]
    public static class GizmoUtils
    {
        private const string k_KEY_GIZMO_ENABLED = "Satabako_GizmoIconsDisabled";

        static GizmoUtils()
        {
            EditorApplication.update += DisableGizmoIcons;
        }

        static MethodInfo setIconEnabled;

        static MethodInfo SetIconEnabled => setIconEnabled = setIconEnabled ?? Assembly.GetAssembly(typeof(Editor))
            ?.GetType("UnityEditor.AnnotationUtility")
            ?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo getAnnotations;

        private static MethodInfo GetAnnotations =>
            getAnnotations = getAnnotations ??
                             Assembly.GetAssembly(typeof(Editor))
                                 ?.GetType("UnityEditor.AnnotationUtility")
                                 ?.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);

        private static Type t_Annotation = Assembly.GetAssembly(typeof(Editor))?.GetType("UnityEditor.Annotation");

        private static FieldInfo f_classID =
    t_Annotation?.GetField("classID", BindingFlags.Instance | BindingFlags.Public);

        private static FieldInfo f_scriptClass =
            t_Annotation?.GetField("scriptClass", BindingFlags.Instance | BindingFlags.Public);

        static void SetGizmoIconEnabled(Type type, bool enabled)
        {
            if (SetIconEnabled == null) return;
            const int MONO_BEHAVIOR_CLASS_ID = 114; // https://docs.unity3d.com/Manual/ClassIDReference.html
            SetIconEnabled.Invoke(null, new object[] { MONO_BEHAVIOR_CLASS_ID, type.Name, enabled ? 1 : 0 });
        }

        static void DisableGizmoIcons()
        {
            if (SessionState.GetBool(k_KEY_GIZMO_ENABLED, false) ||
                f_classID == null || f_scriptClass == null || GetAnnotations == null || SetIconEnabled == null)
            {
                EditorApplication.update -= DisableGizmoIcons;
                SessionState.GetBool(k_KEY_GIZMO_ENABLED, true);
                return;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var ty in assembly.GetTypes())
                {
                    if (typeof(SatabakoBehaviour).IsAssignableFrom(ty) ||
                        ty == typeof(TabakoScriptBehaviour) ||
                        ty == typeof(DatabaseBase))
                    {
                        SetGizmoIconEnabled(ty, false);
                    }
                }
            }

            EditorApplication.update -= DisableGizmoIcons;
            SessionState.GetBool(k_KEY_GIZMO_ENABLED, true);
        }
    }
}