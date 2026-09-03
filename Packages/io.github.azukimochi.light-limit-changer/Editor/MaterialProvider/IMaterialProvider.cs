using nadena.dev.ndmf;

namespace io.github.azukimochi;

internal interface IMaterialProvider
{
    public void Collect(BuildContext context, CloneDelegate clone);
    public delegate Material CloneDelegate(Material material, Renderer renderer = null);
}
