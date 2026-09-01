// --------------------------------------------------------------------------------
// This file contains implementations based on the source code of "modular-avatar",
// which is distributed under the following license.
//
// MIT License
// Copyright (c) 2022 bd_
// https://github.com/bdunderscore/modular-avatar
// --------------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Linq;

namespace net.satania_shopping.tabakosystem.editor
{
    public static class EditorUtils
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            //https://github.com/bdunderscore/modular-avatar/blob/main/Editor/Util.cs#L43
            EditorApplication.hierarchyChanged += () => { OnHiearchyChangedHook.InvokeHierarchyChanged(); };
        }

        /// <summary>
        /// KeyFramePairVector3の値をKeyFrameの配列に変換します。
        /// (x, y, z)で返ります。
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static (Keyframe[], Keyframe[], Keyframe[]) GetKeys(KeyFramePairVector3[] pairs)
        {
            return (
                pairs.Select(x => new Keyframe() { time = x.time, value = x.value.x }).ToArray(),
                pairs.Select(x => new Keyframe() { time = x.time, value = x.value.y }).ToArray(),
                pairs.Select(x => new Keyframe() { time = x.time, value = x.value.z }).ToArray()
            );
        }

        /// <summary>
        /// KeyFramePairFloatの値をKeyFrameの配列に変換します。
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static Keyframe[] GetKeys(KeyFramePairFloat[] pairs)
        {
            return pairs.Select(x => new Keyframe() { time = x.time, value = x.value }).ToArray();
        }
    }
}