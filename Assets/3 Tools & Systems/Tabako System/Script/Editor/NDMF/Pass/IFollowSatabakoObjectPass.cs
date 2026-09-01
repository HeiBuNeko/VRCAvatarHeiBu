using UnityEngine;
using System;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;
using net.satania_shopping.tabakosystem.runtime;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public sealed class IFollowSatabakoObjectPass : Pass<IFollowSatabakoObjectPass>
    {
        public override string DisplayName => "I Follow Satabako Object Pass";
        private PluginDefinition.Singleton singleton => PluginDefinition.Singleton.instance;
        protected override void Execute(BuildContext ctx)
        {
            if (singleton.tabakoScriptBehaviour == null)
                return;

            IFollowSatabakoObject[] followSatabakoObjects = ctx.AvatarRootObject.GetComponentsInChildren<IFollowSatabakoObject>(true);
            //ない場合はスルー
            if (followSatabakoObjects.Length == 0)
                return;

            //TabakoScriptBehaviour tabakoScriptBehaviour = ctx.AvatarRootObject.GetComponentInChildren<TabakoScriptBehaviour>();
            //if (tabakoScriptBehaviour == null)
            //    throw new Exception("Tabako Script Behaviourが見つかりませんでした。\n誤ってIFollowSatabakoObjectを設定している可能性があります。");

            foreach (var follower in followSatabakoObjects)
            {
                try
                {
                    var tO = follower.TargetObject;

                    if (tO == IFollowSatabakoObject.SatabakoObject.Case)
                        AddBoneProxyChildRoot(follower, singleton.tabakoScriptBehaviour.CT_Case);
                    else if (tO == IFollowSatabakoObject.SatabakoObject.Tabako)
                        AddBoneProxyChildRoot(follower, singleton.tabakoScriptBehaviour.CT_Tabako);
                    else if (tO == IFollowSatabakoObject.SatabakoObject.Lighter)
                        AddBoneProxyChildRoot(follower, singleton.tabakoScriptBehaviour.CT_Lighter);
                    else if (tO == IFollowSatabakoObject.SatabakoObject.Mouth)
                        AddBoneProxyChildRoot(follower, singleton.tabakoScriptBehaviour.CT_Mouth);
                }
                catch (System.Exception)
                {
                    //追従先(CT_◯◯)が未設定等で失敗 → ビルドは止めずNonFatal警告で通知 (原因特定用に follower と本体を参照に付ける)
                    var targetName = follower.TargetObject;
                    SatabakoErrorReport.ReportIFollowMissingTarget(
                        $"IFollowSatabakoObject「{follower.name}」の追従先 {targetName}（TabakoScriptBehaviour.CT_{targetName}）が未設定です。",
                        follower, singleton.tabakoScriptBehaviour);
                }
                finally
                {
                    //成功・失敗どちらでもエディタ専用マーカーは削除
                    UnityEngine.Object.DestroyImmediate(follower);
                }
            }
        }

        private void AddBoneProxyChildRoot(IFollowSatabakoObject follower, Transform target)
        {
            if (target == null)
                throw new NullReferenceException("target is null.");

            //追従した際にローカルスケールが1になるように、Matrix計算で前もってスケールを設定する
            //(target のワールドスケールを follower.parent のローカル空間に変換して localScale に適用)
            Transform followerParent = follower.transform.parent;
            Matrix4x4 parentWorldToLocal = followerParent != null ? followerParent.worldToLocalMatrix : Matrix4x4.identity;
            follower.transform.localScale = (parentWorldToLocal * target.localToWorldMatrix).lossyScale;

            var boneProxy = follower.gameObject.AddComponent<ModularAvatarBoneProxy>();
            boneProxy.attachmentMode = BoneProxyAttachmentMode.AsChildAtRoot;
            boneProxy.target = target;
        }
    }
}