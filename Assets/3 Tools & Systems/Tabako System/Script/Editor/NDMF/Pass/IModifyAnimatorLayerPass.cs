using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using net.satania_shopping.tabakosystem.editor;
using net.satania_shopping.tabakosystem.runtime;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public class IModifyAnimatorLayerPass : Pass<IModifyAnimatorLayerPass>
    {
        public override string DisplayName => "Satania Tabako Modify Animator Layer Pass";

        protected override void Execute(BuildContext ctx)
        {
            IModifyAnimatorLayer[] modifiers = ctx.AvatarRootObject.GetComponentsInChildren<IModifyAnimatorLayer>(true);
            if (modifiers.Length == 0)
                return;

            VRCAvatarDescriptor descriptor = ctx.VRChatAvatarDescriptor();
            foreach (var group in modifiers.Where(m => m != null).GroupBy(m => m.animLayerType))
            {
                AnimatorController controller = AvatarUtils.GetAvatarBaseLayer(descriptor, group.Key);
                foreach (var modifier in group)
                {
                    if (controller == null)
                    {
                        SatabakoErrorReport.ReportInformation($"Modify Animator Layer: {group.Key} のコントローラーが無いため、レイヤー「{modifier.layerName}」は変更しませんでした。");
                        continue;
                    }

                    Apply(controller, group.Key, modifier);
                }
            }

            foreach (var modifier in modifiers)
            {
                if (modifier != null)
                    Object.DestroyImmediate(modifier);
            }
        }

        private static void Apply(AnimatorController controller, AnimLayerType layerType, IModifyAnimatorLayer modifier)
        {
            string layerName = (modifier.layerName ?? "").Trim();
            if (layerName.Length == 0)
            {
                SatabakoErrorReport.ReportInformation($"Modify Animator Layer: レイヤー名が空のため何もしませんでした ({layerType})。");
                return;
            }

            int index = AnimatorLayerMatcher.FindLayerIndex(controller, layerName);
            if (index < 0)
            {
                SatabakoErrorReport.ReportInformation($"Modify Animator Layer: {layerType} にレイヤー「{layerName}」が無いため何もしませんでした。");
                return;
            }

            var layers = controller.layers;
            string mismatch = AnimatorLayerMatcher.Check(layers[index], modifier.expectedStateNames, modifier.strictStateCount, out _);
            if (mismatch != null)
            {
                SatabakoErrorReport.ReportInformation($"Modify Animator Layer: レイヤー「{layerName}」はステートの構成が違うため変更しませんでした ({mismatch})。");
                return;
            }

            switch (modifier.action)
            {
                case IModifyAnimatorLayer.eAction.Remove:
                    WarnShiftedLayerControls(controller, layerType, index, layerName);
                    controller.RemoveLayer(index);
                    break;

                case IModifyAnimatorLayer.eAction.SetWeightZero:
                    if (index == 0)
                    {
                        SatabakoErrorReport.ReportInformation($"Modify Animator Layer: 「{layerName}」は {layerType} の最初のレイヤーなのでウェイトを 0 にできません (Unity が常に 1 として扱う)。削除する設定にしてください。");
                        return;
                    }
                    layers[index].defaultWeight = 0f;
                    controller.layers = layers;
                    break;
            }
        }

        private static void WarnShiftedLayerControls(AnimatorController controller, AnimLayerType layerType, int removedIndex, string layerName)
        {
            var found = AnimatorControllerUtils.FindStatesAndBehavioursAsArrays<VRCAnimatorLayerControl>(controller);
            int shifted = 0;
            foreach (var behaviours in found.Item2)
            {
                foreach (var control in behaviours)
                {
                    if (control != null && control.playable.ToString() == layerType.ToString() && control.layer > removedIndex)
                        shifted++;
                }
            }

            if (shifted > 0)
                SatabakoErrorReport.ReportWarning($"Modify Animator Layer: レイヤー「{layerName}」を削除したため、{layerType} の VRC Animator Layer Control {shifted} 個が指すレイヤー番号がずれます。ウェイトを 0 にする設定への変更を検討してください。");
        }
    }
}
