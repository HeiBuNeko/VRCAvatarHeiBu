using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// プラグインのMenuItemをさたにあ式タバコのMenuItem内に入れるための物
    /// </summary>
    [AddComponentMenu("Satania Tabako/Satabako I MA MenuItem")]
    [DisallowMultipleComponent]
    public class ISatabakoMenuItem : SatabakoBehaviour
    {
        public enum eSubmenuType
        {
            Root,
            AdvancedSetting,
            SmokeSpeed,
            ManualON,
        }

        public eSubmenuType submenuType = eSubmenuType.Root;
    }
}