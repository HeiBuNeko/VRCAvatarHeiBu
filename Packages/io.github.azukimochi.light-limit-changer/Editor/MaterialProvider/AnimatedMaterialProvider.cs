using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;

namespace io.github.azukimochi;

internal sealed class AnimatedMaterialProvider : IMaterialProvider
{
    public void Collect(BuildContext context, IMaterialProvider.CloneDelegate clone)
    {
        var index = context.Extension<AnimatorServicesContext>().AnimationIndex;
        index.RewriteObjectCurves((binding, obj) =>
        {
            if (!typeof(Renderer).IsAssignableFrom(binding.type))
                return obj;

            var renderer = context.AvatarRootTransform.Find(binding.path)?.GetComponent<Renderer>();

            if (obj is Material mat)
                return clone(mat, renderer);
            return obj;
        });
    }

}
