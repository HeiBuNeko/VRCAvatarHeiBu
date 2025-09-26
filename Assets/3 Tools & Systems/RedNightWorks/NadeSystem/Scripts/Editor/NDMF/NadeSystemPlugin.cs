using nadena.dev.ndmf;
using RedNightWorks.NadeSystem;

[assembly: ExportsPlugin(typeof(NadeSystemPlugin))]

namespace RedNightWorks.NadeSystem
{
    public class NadeSystemPlugin : Plugin<NadeSystemPlugin>
    {
        public override string DisplayName => "NadeSystemPlugin";

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Nade System Initialization", ctx =>
                {
                    NadeSystemProcessor.MainProcess(ctx);
                });
        }
    }
}