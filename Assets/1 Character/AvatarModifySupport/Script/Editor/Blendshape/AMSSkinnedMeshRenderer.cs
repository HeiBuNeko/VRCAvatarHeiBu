using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.ams.avatarmodifysupport
{
    internal sealed class AMSSkinnedMeshRenderer
    {
        SkinnedMeshRenderer renderer;
        List<AMSBlendshape> blendshapes;

        internal SkinnedMeshRenderer Renderer => renderer;
        internal List<AMSBlendshape> Blendshapes => blendshapes;
        internal Dictionary<string, List<AMSBlendshape>> categoriedBlendshape;
        internal bool[] categoryIsOpened = new bool[0];
        internal int blendshapeCount = 0;

        internal AMSSkinnedMeshRenderer(SkinnedMeshRenderer _renderer)
        {
            renderer = _renderer;
            blendshapes = new List<AMSBlendshape>();

            Mesh mesh = _renderer?.sharedMesh;
            if (mesh == null) return;

            blendshapeCount = mesh.blendShapeCount;
            for (int i = 0; i < blendshapeCount; i++)
            {
                int frameCount = mesh.GetBlendShapeFrameCount(i);
                float max = mesh.GetBlendShapeFrameWeight(i, frameCount - 1);
                string name = mesh.GetBlendShapeName(i);

                AMSBlendshape blendshape = new AMSBlendshape(i, name, max);

                blendshapes.Add(blendshape);
            }

            for (int i = 0; i < blendshapes.Count; i++)
            {
                var blendshape = blendshapes[i];

                string name = blendshape.DisplayName;

                if (name.Length > 2)
                {
                    string lastStr = name.Substring(name.Length - 2, 2);
                    if (lastStr.Equals("_L") || lastStr.Equals("_R"))
                    {
                        string rootName = name.Substring(0, name.Length - 2);
                        int index_parent = GetIndex(rootName);

                        if (index_parent != -1)
                            blendshape.parent = blendshapes[index_parent];

                        continue;
                    }
                }

                string _L = name + "_L";
                string _R = name + "_R";

                int index_L = GetIndex(_L);
                int index_R = GetIndex(_R);

                if (index_L != -1)
                {
                    blendshape.childs.Add(blendshapes[index_L]);
                }

                if (index_R != -1)
                {
                    blendshape.childs.Add(blendshapes[index_R]);
                }

            }
        }
        internal int GetIndex(string name)
        {
            var val = Blendshapes
                .Select((x, i) => new { Content = x, Index = i })
                .Where(x => x.Content.DisplayName.Equals(name))
                .FirstOrDefault();

            if (val == null)
                return -1;

            return val.Index;
        }
        internal float GetValue(string name)
        {
            var val = Blendshapes
                .Select((x, i) => new { Content = x, Index = i })
                .Where(x => x.Content.DisplayName.Equals(name))
                .FirstOrDefault();

            if (val == null)
                return 0f;

            return renderer.GetBlendShapeWeight(val.Index);
        }
        internal float GetValue(int index)
        {
            return renderer.GetBlendShapeWeight(index);
        }
        internal string GetName(int index)
        {
            var val = Blendshapes
                .Select((x, i) => new { Content = x, Index = i })
                .Where(x => x.Content.Index == index)
                .FirstOrDefault();

            if (val == null)
                return "";

            return val.Content.DisplayName;
        }

        /// <summary>
        /// Blendshapesの情報を元に他のSkinnedMeshRendererにウェイトを適用します。
        /// </summary>
        /// <param name="targetRenderer">適用先のSkinnedMeshRenderer</param>
        internal void SetWeights(SkinnedMeshRenderer targetRenderer)
        {
            if (targetRenderer == null)
                return;

            for (int i = 0; i < Blendshapes.Count; i++)
            {
                try
                {
                    var b = Blendshapes[i];
                    var value = renderer.GetBlendShapeWeight(b.Index);
                    targetRenderer.SetBlendShapeWeight(b.Index, value);
                }
                catch { }
            }
        }

        internal Dictionary<string, List<AMSBlendshape>> GetCategorizedBlendshapes()
        {
            var categorized = new Dictionary<string, List<AMSBlendshape>>();

            string categoryName = "";

            string menuBar = "======";
            for (int i = 0; i < Blendshapes.Count; i++)
            {
                var blendshape = Blendshapes[i];
                string name = blendshape.DisplayName;

                if (string.IsNullOrEmpty(categoryName) && !name.Contains(menuBar))
                {
                    categoryName = "Default";
                    categorized.Add(categoryName, new List<AMSBlendshape>());
                }

                if (name.Contains(menuBar))
                {
                    categoryName = name.Replace(menuBar, "");
                    categorized.Add(categoryName, new List<AMSBlendshape>());
                    continue;
                }

                categorized[categoryName].Add(blendshape);
            }

            return categorized;
        }
    }
}