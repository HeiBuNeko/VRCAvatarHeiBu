#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.preset
{
    /// <summary>
    /// ブレンドシェイプの値をプリセットとしてとっておくところ
    /// </summary>
    public class AMSBlendshapePreset
    {
        public List<BlendshapeGroup> blendshapePresetGroups;
        public string displayName = "";

        /// <summary>
        /// アニメーションクリップ用
        /// </summary>
        readonly string blendshapeProperty = "blendShape.";

        [JsonIgnore]
        public Vector2 scrollPosition = new Vector2(0, 0);

        [JsonProperty]
        private bool _isOpened = false;
        public bool IsOpened
        {
            get => _isOpened;
            set => _isOpened = value;
        }
        public AMSBlendshapePreset(string displayName, bool opened = true)
        {
            blendshapePresetGroups = new List<BlendshapeGroup>();
            this.displayName = displayName;
            _isOpened = opened;
        }

        #region SubClass
        public class BlendshapeData
        {
            [JsonProperty]
            string shapeKeyName;

            [JsonProperty]
            float weight;

            /// <summary>
            /// 実際のブレンドシェイプの名前
            /// </summary>
            public string BlendshapeKeyName => shapeKeyName;
            public float Weight
            {
                get => weight;
                set => weight = value;
            }
            public BlendshapeData(string _blendshapeName, float _value)
            {
                shapeKeyName = _blendshapeName;
                weight = _value;
            }
            public void Set(float weight)
            {
                Weight = weight;
            }
        }
        public class BlendshapeGroup
        {
            [JsonProperty]
            private string _rendererPath;
            public string RendererPath
            {
                get => _rendererPath;
                set => _rendererPath = value;
            }

            [JsonIgnore]
            SkinnedMeshRenderer renderer;

            List<BlendshapeData> blendshapeDatas = new List<BlendshapeData>();

            [JsonIgnore]
            public SkinnedMeshRenderer Renderer => renderer;

            public List<BlendshapeData> BlendshapeDatas => blendshapeDatas;

            public BlendshapeGroup(SkinnedMeshRenderer _renderer, Transform avatarRoot)
            {
                if (_renderer != null && avatarRoot != null)
                {
                    _rendererPath = AnimationUtility.CalculateTransformPath(_renderer.transform, avatarRoot);
                    renderer = _renderer;
                }
            }

            public bool CheckRendererExistsAndRefleshPath(Transform avatarRoot)
            {
                if (renderer == null || avatarRoot == null)
                    return false;

                _rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, avatarRoot);
                if (string.IsNullOrEmpty(_rendererPath))
                    return false;

                return true;
            }
            public int GetBlendshapeDataIndex(string shapeKeyName)
            {
                if (blendshapeDatas.Count == 0)
                    return -1;

                var content = blendshapeDatas.Select((p, i) => new { Content = p, Index = i })
                    .Where(x => x.Content.BlendshapeKeyName == shapeKeyName)
                    .FirstOrDefault();

                if (content == null)
                    return -1;

                return content.Index;
            }
            public void Add(string shapeKeyName, float weight)
            {
                if (GetBlendshapeIndex(renderer, shapeKeyName) == -1) return;

                int index = GetBlendshapeDataIndex(shapeKeyName);

                if (index > -1)
                    blendshapeDatas[index].Weight = weight;
                else
                    blendshapeDatas.Add(new BlendshapeData(shapeKeyName, weight));
            }
            public void Remove(string shapeKeyName)
            {
                blendshapeDatas = blendshapeDatas.Where(x => !x.BlendshapeKeyName.Equals(shapeKeyName)).ToList();
            }
            /// <summary>
            /// JSONに保存されていたpathを元にアバターからSkinnedMeshRendererを取得
            /// </summary>
            /// <param name="avatarRoot"></param>
            /// <returns></returns>
            public bool LoadRenderer(Transform avatarRoot)
            {
                if (string.IsNullOrEmpty(_rendererPath))
                    return false;

                renderer = avatarRoot.transform.Find(_rendererPath)?.GetComponent<SkinnedMeshRenderer>();

                return renderer != null;
            }
            public Dictionary<int, BlendshapeData> GetBlendshapeIndexAndWeight(SkinnedMeshRenderer renderer)
            {
                if (renderer == null)
                    throw new System.ArgumentNullException("rendererがNullです。");

                Mesh mesh = renderer.sharedMesh;
                if (mesh == null)
                    throw new System.ArgumentNullException("renderer.sharedMeshがNullです。");

                Dictionary<int, BlendshapeData> ret = new Dictionary<int, BlendshapeData>();
                foreach (var data in blendshapeDatas)
                {
                    string name = data.BlendshapeKeyName;

                    int index = mesh.GetBlendShapeIndex(name);
                    if (index > -1)
                        ret.Add(index, data);
                }

                return ret;
            }
        }
        #endregion

        #region Method
        public int GetIndex(SkinnedMeshRenderer renderer)
        {
            if (blendshapePresetGroups.Count == 0)
                return -1;

            var content = blendshapePresetGroups
                .Select((p, i) => new { Content = p, Index = i })
                .Where(x => x.Content.Renderer == renderer)
                .FirstOrDefault();

            if (content == null)
                return -1;

            return content.Index;
        }
        public void Add(SkinnedMeshRenderer renderer, string shapeKeyName, float weight, Transform avatarRoot)
        {
            int index = GetIndex(renderer);

            if (index == -1)
            {
                var group = new BlendshapeGroup(renderer, avatarRoot);
                group.Add(shapeKeyName, weight);

                blendshapePresetGroups.Add(group);
            }
            else
            {
                var group = blendshapePresetGroups[index];
                group.Add(shapeKeyName, weight);
            }
        }
        public void Remove(SkinnedMeshRenderer renderer, string shapeKeyName)
        {
            int index = GetIndex(renderer);
            if (index == -1) return;

            blendshapePresetGroups = blendshapePresetGroups
                .Where(x => x.Renderer == renderer && x.GetBlendshapeDataIndex(shapeKeyName) == -1)
                .ToList();
        }
        public AMSBlendshapePreset DeepClone()
        {
            AMSBlendshapePreset cloned = new AMSBlendshapePreset(displayName);
            cloned.blendshapePresetGroups = new List<BlendshapeGroup>(blendshapePresetGroups);

            return cloned;
        }
        public AnimationClip CreateClip(string name, Transform rootTransform)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = name;

            foreach (BlendshapeGroup group in blendshapePresetGroups)
            {
                string path = AnimationUtility.CalculateTransformPath(group.Renderer?.transform, rootTransform);
                foreach (var data in group.BlendshapeDatas)
                {
                    AnimationCurve curve = new AnimationCurve();
                    string property = blendshapeProperty + data.BlendshapeKeyName;

                    curve.AddKey(0.0f, data.Weight);
                    curve.AddKey(0.01f, data.Weight);
                    clip.SetCurve(path, typeof(SkinnedMeshRenderer), property, curve);
                }
            }

            return clip;
        }
        public void ApplyToAvatar()
        {
            //Undo
            Undo.RecordObjects(blendshapePresetGroups.Select(x => x.Renderer).ToArray(), "Change blendshape weights");

            foreach (var group in blendshapePresetGroups)
            {
                var Renderer = group.Renderer;
                if (Renderer == null)
                    continue;

                Mesh mesh = Renderer.sharedMesh;
                int count = mesh.blendShapeCount;

                var datas = group.GetBlendshapeIndexAndWeight(Renderer);

                for (int i = 0; i < count; i++)
                {
                    if (!datas.ContainsKey(i))
                        Renderer.SetBlendShapeWeight(i, 0);
                    else
                        Renderer.SetBlendShapeWeight(i, datas[i].Weight);
                }

                EditorUtility.SetDirty(Renderer);
            }
        }
        #endregion

        #region Static Method
        public static int GetBlendshapeIndex(SkinnedMeshRenderer renderer, string shapeKeyName)
        {
            Mesh mesh = renderer?.sharedMesh;
            if (mesh == null || string.IsNullOrEmpty(shapeKeyName)) return -1;

            return mesh.GetBlendShapeIndex(shapeKeyName);
        }
        #endregion
    }
}
#endif