#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.colormodify;
using System;
using System.Collections.Generic;
using UnityEngine;
using static com.ams.avatarmodifysupport.colormodify.AMSColorModifyPreset;
using static com.ams.avatarmodifysupport.colormodify.AMSColorModifyPresetGroup;

namespace com.ams.avatarmodifysupport.callback
{
    internal class AMSPopupCallback
    {
        /// <summary>
        /// プリセットを削除するときの動作
        /// </summary>
        internal Action onPresetRemove;
        /// <summary>
        /// プリセットを複製するときの動作
        /// </summary>
        internal Action onPresetDuplicate;
        /// <summary>
        /// プリセットを1つ上に移動する
        /// </summary>
        internal Action onPresetOrderUp;
        /// <summary>
        /// プリセットを1つ下に移動する
        /// </summary>
        internal Action onPresetOrderDown;
        /// <summary>
        /// アニメーションファイルとして保存を押した時
        /// </summary>
        internal Action onPresetAnimationSave;
        /// <summary>
        /// プリセットの中身を初期化する時
        /// </summary>
        internal Action onPresetClear;
        /// <summary>
        /// ヘルプアイコンをクリックした時
        /// </summary>
        internal Action onPresetHelpClicked;
        /// <summary>
        /// アバターへ適用を押した時
        /// </summary>
        internal Action onApplyToAvatar;

        /// <summary>
        /// プリセットをファイルからインポートした時
        /// </summary>
        internal Action onImportPreset;
        /// <summary>
        /// プリセットをファイルにエクスポートした時
        /// </summary>
        internal Action onExportPreset;
        /// <summary>
        /// プリセットの作成ボタンを押した時
        /// </summary>
        internal Action<string> onCreatePresetClicked;
        /// <summary>
        /// アバターの値を元に作成するボタンを押した時
        /// </summary>
        internal Action<string> onCreatePresetByAvatarClicked;

        /// <summary>
        /// ブレンドシェイプのプリセットを作成する際に、レンダラーを選択した時
        /// </summary>
        internal Action<List<SkinnedMeshRenderer>> onClickSelectRenderer;

        /// <summary>
        /// ブレンドシェイプのプリセットを作成する際に、レンダラーを選択をキャンセルした時
        /// </summary>
        internal Action onCancelSelectRenderers;

        /// <summary>
        /// (rendererIndex, materialIndex)
        /// </summary>
        internal Action<int, int> onClickedSelectMaterial;

        /// <summary>
        /// カラーグループを作成するボタンを押した時
        /// </summary>
        internal Action<AMSColorModifyPreset> onClickCreateColorPreset;

        /// <summary>
        /// 作成したカラーグループを削除する時
        /// </summary>
        internal Action onClickRemoveColorPreset;

        /// <summary>
        /// カラーグループの中身を初期化する時
        /// </summary>
        internal Action onClickClearColorPreset;

        /// <summary>
        /// カラーグループにマテリアルを追加する時
        /// (マテリアル、ユーザーに表示するサンプル色、名前)
        /// </summary>
        internal Action<Material, Color32, string> onClickAddMaterial;

        /// <summary>
        /// カラーを削除する時
        /// </summary>
        internal Action<AMSMaterial> onClickRemoveMaterial;

        internal Action<SkinnedMeshRenderer, string> onSelectRendererForSave;
        internal Action<string> onClickSelectName;
        internal Action<List<ColorModifyPresetNamePair>> onClickColorPreset;
        internal Action<ColorGroup> onCreateColorGroup;
    }
}
#endif