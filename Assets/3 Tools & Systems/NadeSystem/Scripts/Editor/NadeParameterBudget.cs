using System;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedNightWorks.NadeSystem
{
    internal static class NadeParameterBudget
    {
        // Query the same providers used by MA's parameter usage window, without running a build.
        internal static int Estimate(GameObject avatar, Action<Transform> installPreview)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var holder = new GameObject("NadeSystem parameter preview");
                SceneManager.MoveGameObjectToScene(holder, scene);
                holder.SetActive(false);
                var copy = UnityEngine.Object.Instantiate(avatar, holder.transform);
                var existing = copy.transform.Find("NadeSystem");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                installPreview(copy.transform);
                bool conflict = false;
                int bits = ParameterInfo.ForUI.GetParametersForObject(copy,
                    (type, first, second) => conflict = true).Sum(parameter => parameter.BitUsage);
                if (conflict)
                    throw new InvalidOperationException("Conflicting expression parameter types prevent estimation.");
                return bits;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
