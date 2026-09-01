using JetBrains.Annotations;
using nadena.dev.modular_avatar.core;
using net.satania_shopping.tabakosystem.runtime;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    /// <summary>
    /// 自身がさたにあ式タバコの追加機能な事を示すBehaviour
    /// Tabako Scriptのみで使用
    /// </summary>
    [AddComponentMenu("Satania Tabako/Satabako I Satabako Plugin")]
    public class ISatabakoPlugin : SatabakoBehaviour
    {
        public enum ePluginType
        {
            Custom,
            [InspectorName("(Internal) Cigarette")] _Internal_Cigarette,
            [InspectorName("(Internal) ExhaleSmoke")] _Internal_ExhaleSmoke,
            [InspectorName("(Internal) LipSync")] _Internal_LipSync,
            [InspectorName("(Internal) DesktopArm")] _Internal_DesktopArm
        }
        [SerializeField] private int useBitCount = 0;

        //MA Menu Itemで作成したGameObjectを入れる
        [SerializeField] private GameObject[] expressionMenus;

        [SerializeField] private ModularAvatarParameters[] parameters;

        [PublicAPI] public int UseBitCount => useBitCount;
        [PublicAPI] public GameObject[] ExpressionMenuObjects => expressionMenus;
        [PublicAPI] public ModularAvatarParameters[] Parameters => parameters;
    }
}