using nadena.dev.ndmf;
using net.satania_shopping.tabakosystem.runtime;
using System.Linq;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public class MoveMenuItemPass : Pass<MoveMenuItemPass>
    {
        public override string DisplayName => "Move MA MenuItem Pass";

        private PluginDefinition.Singleton singleton => PluginDefinition.Singleton.instance;

        protected override void Execute(BuildContext ctx)
        {
            GameObject avatarRoot = ctx.AvatarRootObject;
            IMoveToMenuItem[] movetoMenuItems = avatarRoot.GetComponentsInChildren<IMoveToMenuItem>(true);
            ISatabakoMenuItem[] satabakoMenuItems = avatarRoot.GetComponentsInChildren<ISatabakoMenuItem>(true);

            if (singleton.tabakoScriptBehaviour != null)
            {
                Transform menuRoot = satabakoMenuItems
                    .Where(x => x.submenuType == ISatabakoMenuItem.eSubmenuType.Root)
                    .Select(x => x.transform)
                    .FirstOrDefault();

                Transform advancedSetting = satabakoMenuItems
                    .Where(x => x.submenuType == ISatabakoMenuItem.eSubmenuType.AdvancedSetting)
                    .Select(x => x.transform)
                    .FirstOrDefault();

                Transform smokeSpeed = satabakoMenuItems
                    .Where(x => x.submenuType == ISatabakoMenuItem.eSubmenuType.SmokeSpeed)
                    .Select(x => x.transform)
                    .FirstOrDefault();

                Transform manualON = satabakoMenuItems
                    .Where(x => x.submenuType == ISatabakoMenuItem.eSubmenuType.ManualON)
                    .Select(x => x.transform)
                    .FirstOrDefault();

                foreach (var willMove in movetoMenuItems)
                {
                    if (willMove.MoveTo == ISatabakoMenuItem.eSubmenuType.Root && menuRoot != null)
                    {
                        willMove.transform.SetParent(menuRoot);
                    }
                    else if (willMove.MoveTo == ISatabakoMenuItem.eSubmenuType.AdvancedSetting && advancedSetting != null)
                    {
                        //詳細設定にSetParentするとMenuItem消える・・・なぜ？
                        willMove.transform.SetParent(advancedSetting);
                    }
                    else if (willMove.MoveTo == ISatabakoMenuItem.eSubmenuType.SmokeSpeed && smokeSpeed != null)
                    {
                        willMove.transform.SetParent(smokeSpeed);
                    }
                    else if (willMove.MoveTo == ISatabakoMenuItem.eSubmenuType.ManualON && manualON != null)
                    {
                        willMove.transform.SetParent(manualON);
                    }
                }
            }

            foreach (var willMove in movetoMenuItems)
                if (willMove != null)
                    Object.DestroyImmediate(willMove);

            foreach (var menuItem in satabakoMenuItems)
                if (menuItem != null)
                    Object.DestroyImmediate(menuItem);
        }
    }
}