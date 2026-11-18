using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class FingerMuscles
    {
        private static int[] s_indices;
        private static HashSet<int> s_set;

        public static IReadOnlyList<int> Indices
        {
            get
            {
                EnsureTables();
                return s_indices;
            }
        }

        public static bool IsFinger(int muscle)
        {
            EnsureTables();
            return s_set.Contains(muscle);
        }

        public static bool IsFingerBone(HumanBodyBones bone) => bone >= HumanBodyBones.LeftThumbProximal && bone <= HumanBodyBones.RightLittleDistal;

        public static int Mirror(int muscle)
        {
            if (!IsFinger(muscle))
                return -1;
            int bone = HumanTrait.BoneFromMuscle(muscle);
            if (bone < (int)HumanBodyBones.LeftThumbProximal || bone > (int)HumanBodyBones.RightLittleDistal)
                return -1;
            const int span = (int)HumanBodyBones.RightThumbProximal - (int)HumanBodyBones.LeftThumbProximal;
            int mirrorBone = bone < (int)HumanBodyBones.RightThumbProximal ? bone + span : bone - span;
            for (int dof = 0; dof < 3; dof++)
            {
                if (HumanTrait.MuscleFromBone(bone, dof) == muscle)
                    return HumanTrait.MuscleFromBone(mirrorBone, dof);
            }
            return -1;
        }

        public static int Stretched(HumanBodyBones bone) => HumanTrait.MuscleFromBone((int)bone, 2);

        public static int Spread(HumanBodyBones bone) => HumanTrait.MuscleFromBone((int)bone, 1);

        public static string JointLabel(HumanBodyBones bone)
        {
            int m = Stretched(bone);
            if (m < 0)
                return bone.ToString();
            string name = HumanTrait.MuscleName[m];
            const string suffix = " Stretched";
            return name.EndsWith(suffix) ? name.Substring(0, name.Length - suffix.Length) : name;
        }

        public static string CurveProperty(int muscle)
        {
            string name = HumanTrait.MuscleName[muscle];
            string[] parts = name.Split(' ');
            if (parts.Length == 4 && parts[3] == "Stretched")
                return $"{parts[0]}Hand.{parts[1]}.{parts[2]} Stretched";
            if (parts.Length == 3 && parts[2] == "Spread")
                return $"{parts[0]}Hand.{parts[1]}.Spread";
            return name;
        }

        public static float[] FromClip(AnimationClip clip)
        {
            EnsureTables();
            var values = new float[HumanTrait.MuscleCount];
            for (int i = 0; i < values.Length; i++)
                values[i] = s_set.Contains(i) ? 0f : float.NaN;
            if (clip == null)
                return values;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Animator) || !string.IsNullOrEmpty(binding.path))
                    continue;
                int m = FingerPosePreview.MuscleIndexFromCurveName(binding.propertyName);
                if (m < 0 || !s_set.Contains(m))
                    continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null && curve.length > 0)
                    values[m] = curve.Evaluate(0f);
            }
            return values;
        }

        public static AnimationClip ToClip(float[] muscles, string name)
        {
            EnsureTables();
            var clip = new AnimationClip { name = name };
            foreach (int m in s_indices)
            {
                float v = muscles != null && m < muscles.Length && !float.IsNaN(muscles[m]) ? muscles[m] : 0f;
                var curve = new AnimationCurve(new Keyframe(0f, v), new Keyframe(1f / 60f, v));
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), CurveProperty(m)), curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        public static AnimationClip SaveClipAsset(float[] muscles, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            var clip = ToClip(muscles, Path.GetFileNameWithoutExtension(assetPath));
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                Object.DestroyImmediate(clip);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
            return clip;
        }

        public static AnimationClip EmbedClipInAsset(float[] muscles, string assetPath, string name)
        {
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                return null;

            var clip = ToClip(muscles, name);
            var existing = FindEmbeddedClip(assetPath, name);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                Object.DestroyImmediate(clip);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.AddObjectToAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return FindEmbeddedClip(assetPath, name) ?? clip;
        }

        public static AnimationClip FindEmbeddedClip(string assetPath, string name)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (o is AnimationClip c && c.name == name && AssetDatabase.IsSubAsset(c))
                    return c;
            }
            return null;
        }

        public static bool RemoveEmbeddedClip(string assetPath, string name)
        {
            var existing = FindEmbeddedClip(assetPath, name);
            if (existing == null)
                return false;
            AssetDatabase.RemoveObjectFromAsset(existing);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void EnsureTables()
        {
            if (s_indices != null)
                return;

            var list = new List<int>();
            for (int bone = (int)HumanBodyBones.LeftThumbProximal; bone <= (int)HumanBodyBones.RightLittleDistal; bone++)
            {
                for (int dof = 0; dof < 3; dof++)
                {
                    int m = HumanTrait.MuscleFromBone(bone, dof);
                    if (m >= 0)
                        list.Add(m);
                }
            }
            s_indices = list.ToArray();
            s_set = new HashSet<int>(list);
        }
    }
}
