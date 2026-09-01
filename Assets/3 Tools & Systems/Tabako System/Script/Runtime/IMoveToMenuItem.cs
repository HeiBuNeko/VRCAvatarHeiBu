using UnityEngine;
using static net.satania_shopping.tabakosystem.runtime.ISatabakoMenuItem;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// MenuItemをモジュール化するために、どこのMenuItemを移動するか示すBehaviour
    /// </summary>
    [AddComponentMenu("Satania Tabako/Satabako I Move to MA MenuItem")]
    [DisallowMultipleComponent]
    public class IMoveToMenuItem : SatabakoBehaviour
    {
        public eSubmenuType MoveTo = eSubmenuType.Root;
    }
}