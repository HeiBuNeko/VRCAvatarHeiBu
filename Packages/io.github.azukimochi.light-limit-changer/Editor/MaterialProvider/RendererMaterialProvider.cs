using nadena.dev.ndmf;
using System.Linq;

namespace io.github.azukimochi;

internal sealed class RendererMaterialProvider : IMaterialProvider
{
    public void Collect(BuildContext context, IMaterialProvider.CloneDelegate clone)
    {
        var processor = context.GetState(LightLimitChangerState.New).Processor;
        var excludes = processor.Excludes;

        var excludeMaterials = excludes.Where(x => x.Value is { Object: Material, SkipAnimation: true }).Select(x => x.Key as Material).Where(x => x != null).ToHashSet();
        // MA実行後のパスから呼ばれた場合、除外対象 (特に「下層を含める」で展開された子) が
        // 他プラグインにより破棄されていることがある。as + ?. はUnityの破棄チェックを通らないため、
        // Unityのnull判定 (go != null) で生存確認をしてからGetComponentを呼ぶ
        var excludeRenderers = excludes.Keys.Select(x => x is GameObject go && go != null ? go.GetComponent<Renderer>() : null)
            .Where(x => x != null)
            .ToHashSet();
        
        bool IsTargetRenderer(Renderer renderer)
        {
            if (renderer is ParticleSystemRenderer && !processor.Component.General.IncludeParticleSystem)
                return false;
            if (excludeRenderers.Contains(renderer))
                return false;

            foreach (var material in renderer.sharedMaterials)
            {
                if (excludeMaterials.Contains(material))
                    return false;
            }

            return true;
        }

        foreach (var renderer in context.AvatarRootObject.GetComponentsInChildren<Renderer>(true))
        {
            if (!IsTargetRenderer(renderer))
                continue;

            var materials = renderer.sharedMaterials;
            foreach (ref var material in materials.AsSpan())
            {
                material = clone(material, renderer);
            }
            renderer.sharedMaterials = materials;
        }
    }
}
