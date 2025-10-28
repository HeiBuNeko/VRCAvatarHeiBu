using UnityEngine;

namespace com.ams.avatarmodifysupport.language
{
    public class AMSLanguage : ScriptableObject
    {
        [TextArea] public string title = "AvatarModifySupport";
        [TextArea] public string ok = "OK";
        [TextArea] public string cancel = "キャンセル";

        //----------------------------------------------------------------
        //プリセット欄
        [TextArea] public string blendshapePreset = "ブレンドシェイププリセット";
        [TextArea] public string preset = "プリセット";
        [TextArea] public string whatPreset = "プリセット機能とは";
        [TextArea] public string whatPresetEx = "シェイプキーの値を保存しておける機能！\n他のユーザーさんと共有することも可能です！";
        [TextArea] public string errorResetAvatarByEmptyFBXPath = "FBXのパスが取得出来なかったため、シェイプキーの値をリセット出来ませんでした。";
        [TextArea] public string finishResetAvatarShapekeys = "シェイプキーの値をリセットしました！";
        [TextArea] public string createNewPreset = "プリセットを新規作成";
        [TextArea] public string newPreset = "新規プリセット";
        [TextArea] public string createByAvatarValue = "アバターの値で作成";
        [TextArea] public string importPresetFromFile = "ファイルからインポート";
        [TextArea] public string addBlendshape = "ブレンドシェイプを追加";
        [TextArea] public string resetAvatarShapekeys = "シェイプキーをリセット";
        [TextArea] public string presetName = "プリセットの名前";
        [TextArea] public string removePreset = "プリセットを削除します。\nよろしいですか？";
        [TextArea] public string selectSaveAnimPath = "アニメーションファイルの保存先を選んでください。";
        [TextArea] public string resetPresetQuestion = "プリセットの中身を初期化します。\nよろしいですか？";
        [TextArea] public string applyPresetEmpty = "プリセットの中にブレンドシェイプが入っていません。\nブレンドシェイプを追加してから、プリセットを適用してください。";
        [TextArea] public string applyPresetQuestion = "プリセットをアバターに適用します。\nよろしいですか？";
        [TextArea] public string exportPresetEmpty = "プリセット内にブレンドシェイプが存在しません。\nブレンドシェイプを追加した後に出力してください。";
        [TextArea] public string errorExportPreset = "プリセットの出力に失敗しました。";
        [TextArea] public string invalidPath = "選択されたプリセットファイルの場所が不正です。";
        [TextArea] public string reason = "理由";
        [TextArea] public string loadError = "プリセットファイルの読み込みに失敗しました。";
        [TextArea] public string selectExportPresetPath = "プリセットファイルの保存先を選んでください。";
        [TextArea] public string applyToAvatar = "アバターに適用";
        [TextArea] public string upPreset = "▲ 1つ上に移動";
        [TextArea] public string downPreset = "▼ 1つ下に移動";
        [TextArea] public string duplicate = "複製";
        [TextArea] public string remove = "削除";
        [TextArea] public string create = "作成";
        [TextArea] public string selectSkinnedMeshRendererTitle = "プリセットに含むメッシュを選択してください。";
        [TextArea] public string saveAsAnimation = "アニメーションファイルとして保存";
        [TextArea] public string export = "出力";
        [TextArea] public string clear = "クリア";
        [TextArea] public string apply = "適用";
        [TextArea] public string select = "選択";
        [TextArea] public string add = "追加";
        [TextArea] public string resetShapekeyQuestion = "アバターのシェイプキーの値を全てリセットします。\nよろしいですか？";
        [TextArea] public string saveAsDefaultAnimation = "Default用アニメーションファイルとして保存";
        [TextArea] public string selectRendererForAnimation = "顔のメッシュを選択してください。";

        //----------------------------------------------------------------
        //色改変
        [TextArea] public string colorModify = "色改変";
        [TextArea] public string whatColorModify = "色改変機能とは";
        [TextArea] public string whatColorModifyEx = "色改変されたテクスチャを簡単に適用出来る機能！\n好きな色のテクスチャを使ってアバターを改変しよう！";
        [TextArea] public string addColor = "カラーを追加";
        [TextArea] public string colorName = "カラーの名前";
        [TextArea] public string addGroup = "グループを追加";
        [TextArea] public string color = "色";
        [TextArea] public string material = "マテリアル";
        [TextArea] public string renderer = "レンダラー";
        [TextArea] public string createGroup = "グループを作成";
        [TextArea] public string createGroupError_PleaseSelect = "マテリアルが選択されていません！\nマテリアルを選択した後にグループを作成してください。";
        [TextArea] public string selectMaterial = "マテリアルを選択";
        [TextArea] public string createGroupError_ExistsSomeGroup = "既に同じターゲットのグループが存在します！";
        [TextArea] public string createGroupError_ExsitsSomeName = "既に同じ名前のグループが存在します！";
        [TextArea] public string selectMesh = "メッシュを選択";
        [TextArea] public string changedToMaterial = "[[MATERIALNAME]]に変更しました！";
        [TextArea] public string pingMaterial = "設定されたマテリアルをハイライト";
        [TextArea] public string resetMaterials = "アバターのマテリアルをリセット";
        [TextArea] public string resetMaterialsQuestion = "アバターのマテリアルを全てリセットします。\nよろしいですか？";
        [TextArea] public string errorResetMaterialByEmptyFBXPath = "FBXのパスが取得出来なかったため、アバターのマテリアルをリセット出来ませんでした。";
        [TextArea] public string finishResetAvatarMaterials = "アバターのマテリアルをリセットしました！";
        [TextArea] public string alreadyAdded = "追加済";
        [TextArea] public string multiApply = "まとめて適用";
        [TextArea] public string nameOfGroup = "グループの名前";
        [TextArea] public string removeGroupQues = "グループを削除します。\nよろしいですか？";
        [TextArea] public string selectColorPresetError = "カラープリセットを選んでください！";
        [TextArea] public string someNameGroupExists = "同じ名前のグループが存在します！\n他の名前に変更してください。";

        //----------------------------------------------------------------
        //Behaviourのエディタ
        [TextArea] public string cantusePreviewMode = "プレビューモードでは使用できません";
        [TextArea] public string openSupportMenu = "サポートメニューを開く";
    }
}