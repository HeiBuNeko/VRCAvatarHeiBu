using nadena.dev.modular_avatar.core;

namespace io.github.azukimochi;

[Serializable]
public sealed class MeshSettings 
{
    public bool EnableMeshSettigsOverride;

    public AvatarObjectReference Anchor;
    public AvatarObjectReference RootBone;

    public Bounds Bounds = new(Vector3.zero, Vector3.one * 2);
}