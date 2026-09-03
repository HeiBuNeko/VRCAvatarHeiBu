using System.Collections.Generic;
using nadena.dev.ndmf;

namespace io.github.azukimochi;

[ParameterProviderFor(typeof(LightLimitChangerComponent))]
internal sealed class ParameterProvider : IParameterProvider
{
    public ParameterProvider(LightLimitChangerComponent instance) => Instance = instance;

    public LightLimitChangerComponent Instance { get; }

    public IEnumerable<ProvidedParameter> GetSuppliedParameters(BuildContext context = null)
    {
        if (Instance == null)
            yield break;

        if (Instance.General.CompressionExpressionParameters)
        {
            yield return Parameter<int>("Index", true);
            yield return Parameter<float>("Value", true);

            yield break;
        }

        foreach(var group in Metadata.AllParameterGroups)
        {
            if (!group.Key.GetEnable(Instance))
                continue;

            foreach(var parameterInfo in group)
            {
                var parameter = parameterInfo.Get(Instance);

                if (!parameter.IsAnimated)
                    continue;

                if (parameterInfo.Id is Metadata.ID.General.LightingControl.LightDirection.Id && Instance.General.LightingControl.AllowAdvancedLightDirectionControl)
                {
                    yield return Parameter<float>($"{parameterInfo.ParameterName}.X", parameter.Synced);
                    yield return Parameter<float>($"{parameterInfo.ParameterName}.Y", parameter.Synced);
                    yield return Parameter<bool>($"{parameterInfo.ParameterName}.Enable", parameter.Synced);
                    continue;
                }

                if (parameterInfo.ParameterType == typeof(bool))
                {
                    yield return Parameter<bool>(parameterInfo.ParameterName, parameter.Synced);
                    continue;
                }

                var count = parameter.GetValues(stackalloc float[4]);
                for (int i = 0; i < count; i++)
                {
                    yield return Parameter<float>($"{parameterInfo.ParameterName}{(i == 0 ? "": $".{count + 1}")}", parameter.Synced);
                }

                if (parameterInfo.ColorSettings.HDR)
                    yield return Parameter<float>($"{parameterInfo.ParameterName}.intensity", parameter.Synced);
            }
        }
        yield break;

    }

    private ProvidedParameter Parameter<T>(string name, bool sync = true, ParameterNamespace @namespace = ParameterNamespace.Animator)
            => Parameter(name, sync, @namespace, typeof(T) == typeof(int) ? AnimatorControllerParameterType.Int : typeof(T) == typeof(float) ? AnimatorControllerParameterType.Float : typeof(T) == typeof(bool) ? AnimatorControllerParameterType.Bool : default);

    private ProvidedParameter Parameter(string name, bool sync = true, ParameterNamespace @namespace = ParameterNamespace.Animator, AnimatorControllerParameterType? type = null)
        => new(name, @namespace, Instance, LightLimitChanger.Instance, type)
        {
            WantSynced = sync,
        };
}