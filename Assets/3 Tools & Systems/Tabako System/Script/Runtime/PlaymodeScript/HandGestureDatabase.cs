using System;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// 非対応ジェスチャー用データベース (プレイモードのみ)
    /// I Override Hand Gestureから置き換えられたジェスチャーを保存する
    /// </summary>
    [AddComponentMenu("/")]
    [Obsolete("Trace And Optimizeで消えるので非推奨")]
    public class HandGestureDatabase : DatabaseBase
    {
        [NonSerialized] public Motion gestureCase;
        [NonSerialized] public Motion gestureLighter;
        [NonSerialized] public Motion gestureTabacco;

        [NonSerialized] public RuntimeAnimatorController handGestureController;
    }
}