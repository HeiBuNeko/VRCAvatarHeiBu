
using nadena.dev.ndmf;

namespace io.github.azukimochi;

partial class LightLimitChanger
{
    public static class Passes
    {
        public abstract class LightLimitChangerPass<T> : Pass<T> where T : LightLimitChangerPass<T>, new()
        {
            internal LightLimitChangerPass() { }

            protected override void Execute(BuildContext context)
            {
                var state = context.GetState(LightLimitChangerState.New);
                Run(context, state);
            }

            internal abstract void Run(BuildContext context, LightLimitChangerState state);

            protected abstract string ID { get; }

            internal const string QualifiedNamePrefix = "io.github.azukimochi.light-limit-changer.";
            public override string QualifiedName => $"{QualifiedNamePrefix}{ID}";
        }

        public sealed class CloneMaterialsPass : LightLimitChangerPass<CloneMaterialsPass>
        {
            protected override string ID => "clone-materials";

            public override string DisplayName => "Clone Materials";

            internal override void Run(BuildContext context, LightLimitChangerState state)
            {
                state.Processor?.CloneMaterials();
            }
        }

        public sealed class GenerateAnimationsPass : LightLimitChangerPass<GenerateAnimationsPass>
        {
            protected override string ID => "generate-animations";

            public override string DisplayName => "Generate Animations";

            internal override void Run(BuildContext context, LightLimitChangerState state)
            {
                state.Processor?.GenerateAnimations();
            }
        }

        public sealed class NormalizeMaterialsPass : LightLimitChangerPass<NormalizeMaterialsPass>
        {
            protected override string ID => "normalize-materials";

            public override string DisplayName => "Normalize Materials";

            internal override void Run(BuildContext context, LightLimitChangerState state)
            {
                state.Processor?.NormalizeMaterials();
            }
        }

        public sealed class OverwriteMaterialSettingsPass : LightLimitChangerPass<OverwriteMaterialSettingsPass>
        {
            protected override string ID => "overwrite-material-settings";

            public override string DisplayName => "Overwrite Material Settings";

            internal override void Run(BuildContext context, LightLimitChangerState state)
            {
                state.Processor?.OverwriteMaterialSettings();
            }
        }
    }
}
