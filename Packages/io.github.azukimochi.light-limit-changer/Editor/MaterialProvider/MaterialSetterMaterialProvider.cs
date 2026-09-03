using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;

namespace io.github.azukimochi;

internal sealed class MaterialSetterMaterialProvider : IMaterialProvider
{
    public void Collect(BuildContext context, IMaterialProvider.CloneDelegate clone)
    {
        var components = context.AvatarRootObject.GetComponentsInChildren<ModularAvatarMaterialSetter>(true);
        foreach (var component in components)
        {
            foreach (var x in component.Objects)
            {
                x.Material = clone(x.Material);
            }
        }
    }
}