using UnityEngine;
using VRC.SDKBase;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// TabakoScript以外のコンポーネントに継承するクラス
    /// </summary>
    [HelpURL("https://saturnianjp.github.io/satania_shopping_document/docs/category/%E3%81%95%E3%81%9F%E3%81%AB%E3%81%82%E5%BC%8F%E3%82%BF%E3%83%90%E3%82%B3-1")]
    public abstract class SatabakoBehaviour : MonoBehaviour, IEditorOnly
    {
        protected virtual void OnValidate()
        {
        }
    }
}