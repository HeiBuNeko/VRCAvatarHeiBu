#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.colormodify
{
    public sealed class AMSColorModifyPreset
    {
        public class AMSMaterial
        {
            [JsonProperty]
            string materialGUID;

            [JsonIgnore]
            public Material material;

            public string colorCode = "#ffffffff";
            //public Color color;
            public string displayName = "Name";

            public bool Load()
            {
                if (string.IsNullOrEmpty(materialGUID))
                    return false;

                material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGUID));
                return material != null;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="_material">MainAssetであるMaterialのみ有効</param>
            /// <param name="_color"></param>
            /// <param name="_displayName"></param>
            public AMSMaterial(Material _material, string _color, string _displayName)
            {
                if (!AssetDatabase.IsMainAsset(_material))
                    throw new Exception("SubAssetであるmaterialが参照されました。");

                material = _material;
                colorCode = _color;
                displayName = _displayName;

                if (material != null)
                {
                    materialGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material));
                }
            }

            public AMSMaterial() { }
        }

        [JsonProperty]
        string displayName;

        [JsonProperty]
        string _rendererPath;

        [JsonProperty]
        int materialIndex;

        [JsonIgnore]
        public bool isOpened;

        [JsonIgnore]
        public Renderer renderer;

        [JsonIgnore]
        public Vector2 scroll;

        public string DisplayName => displayName;
        public int MaterialIndex => materialIndex;

        public List<AMSMaterial> amsMaterials = new List<AMSMaterial>();

        public AMSColorModifyPreset(Renderer _renderer, Transform avatarRoot, string _displayName, int _materialIndex)
        {
            _rendererPath = AnimationUtility.CalculateTransformPath(_renderer.transform, avatarRoot);
            renderer = _renderer;
            displayName = _displayName;
            materialIndex = _materialIndex;
        }

        public AMSColorModifyPreset() { }

        public bool Load(Transform avatarRoot)
        {
            renderer = avatarRoot.Find(_rendererPath)?.GetComponent<SkinnedMeshRenderer>();

            List<Action> actions = new List<Action>();

            foreach (var m in amsMaterials)
            {
                if (!m.Load())
                {
                    actions.Add(() => { amsMaterials.Remove(m); });
                }
            }

            foreach (var a in actions)
                a?.Invoke();

            return renderer != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listIndex">AMSMaterialの番号</param>
        public bool TrySetMaterial(int listIndex)
        {
            if (listIndex >= amsMaterials.Count)
                return false;

            Undo.RecordObject(renderer, "Change sharedMaterials");
            var materials = renderer.sharedMaterials;
            materials[materialIndex] = amsMaterials[listIndex].material;
            renderer.sharedMaterials = materials;

            EditorUtility.SetDirty(renderer);
            return true;
        }

        public void AddMaterial(Material material, Color32 color, string displayName)
        {
            if (!AssetDatabase.IsMainAsset(material))
                return;

            if (amsMaterials.Where(x => x.displayName == displayName || x.material == material).Any())
                return;

            var r = color.r.ToString("X2").ToLower();
            var g = color.g.ToString("X2").ToLower();
            var b = color.b.ToString("X2").ToLower();
            var a = color.a.ToString("X2").ToLower();

            amsMaterials.Add(new AMSMaterial(material, $"#{r}{g}{b}{a}", displayName));
        }
    }
}
#endif