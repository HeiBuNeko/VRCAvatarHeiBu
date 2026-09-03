using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;

namespace io.github.azukimochi;

internal sealed class MaterialSwapMaterialProvider : IMaterialProvider
{
    public void Collect(BuildContext context, IMaterialProvider.CloneDelegate clone)
    {
        var components = context.AvatarRootObject.GetComponentsInChildren<ModularAvatarMaterialSwap>(true);
        foreach (var component in components) 
        {
            foreach (ref var swap in component.Swaps.AsSpan())
            {
                swap.To = clone(swap.To);
            }
        }
    }
}