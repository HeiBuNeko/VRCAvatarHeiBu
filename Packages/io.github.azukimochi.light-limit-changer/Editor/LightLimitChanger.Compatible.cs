using System.Linq;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

namespace io.github.azukimochi;

partial class LightLimitChanger
{
    private void ConfigureCompatiblePlugins()
    {
        InPhase(BuildPhase.Optimizing).AfterPlugin("net.raitichan.int-parameter-compressor").Run($"{DisplayName} Compatible Pass for IntParameterCompressor", ExecuteForIntParameterCompressor);
    }

    private void ExecuteForIntParameterCompressor(BuildContext context)
    {
        var descriptor = context.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
        if (descriptor == null || 
            descriptor.baseAnimationLayers?.FirstOrDefault(x => x.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController is not AnimatorController fx)
            return;

        try
        {
            // レイヤーを消去する
            {
                var layers = fx.layers;
                int idx = -1;
                for (int i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];
                    if (layer.name != $"{LightLimitChangerProcessor.ParameterName_ParameterSyncerIndex}_encode_decode")
                        continue;

                    idx = i;
                    break;
                }
                if (idx == -1)
                    return;

                fx.RemoveLayer(idx);
            }

            // ExParamを元に戻して追加された分を消す
            {
                var exparams = descriptor.expressionParameters;
                foreach(ref var param in exparams.parameters.AsSpan())
                {
                    if (!param.name.StartsWith(LightLimitChangerProcessor.ParameterName_ParameterSyncerIndex))
                        continue;

                    if (param.name.Length == LightLimitChangerProcessor.ParameterName_ParameterSyncerIndex.Length)
                    {
                        param.networkSynced = true;
                    }
                    else // _bit.N
                    {
                        param = null;
                    }
                }
                exparams.parameters = exparams.parameters.Where(x => x != null).ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }
    }
}
