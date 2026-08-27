using UnityEditor;

namespace RedNightWorks.NadeSystem
{
    public static class Localize
    {
        public static string[] avatar = new string[] { "Avatar", "アバター", "아바타", "虚拟形象", "虛擬形象" };
        public static string[] contactParameters = new string[] { "Contact Parameters", "コンタクトパラメーター", "콘택트 매개변수", "接触参数", "接觸參數" };
        public static string[] contactRadius = new string[] { "Contact Radius", "コンタクトの半径", "콘택트 반경", "接触半径", "接觸半徑" };
        public static string[] contactOffsetY = new string[] { "Contact Offset Y", "コンタクトのYオフセット", "콘택트 Y 오프셋", "接触Y轴偏移", "接觸Y軸偏移" };
        public static string[] shadowShaderInstall = new string[] { "Shadow Shader Install", "影シェーダーの導入", "그림자 셰이더 도입", "安装阴影着色器", "安裝陰影著色器" };
        public static string[] installHands = new string[] { "Install to Hands (for petting)", "手へ導入(撫でる用)", "손에 설치 (쓰다듬기용)", "安装到手部（用于抚摸）", "安裝到手部（用於撫摸）" };
        public static string[] installFeet = new string[] { "Install to Feet (for petting)", "足へ導入(撫でる用)", "발에 설치 (쓰다듬기용)", "安装到脚部（用于抚摸）", "安裝到腳部（用於撫摸）" };
        public static string[] installHead = new string[] { "Install to Head (for being petted)", "頭へ導入(撫でられる用)", "머리에 설치 (쓰다듬히기용)", "安装到头部（用于被抚摸）", "安裝到頭部（用於被撫摸）" };
        public static string[] installNadeSphere = new string[] { "Install Camera Petting Sphere", "カメラ撫でスフィアの導入", "카메라 쓰다듬기 스피어 설치", "安装相机抚摸球", "安裝相機撫摸球" };
        public static string[] installFoot = new string[] { "Install Petting Sound System to Foot (Requires a full body tracking system to use)", "足へ撫で音ギミックを導入 (使用にはフルトラが必要)", "발에 쓰다듬기 사운드 시스템 설치 (사용하려면 풀 바디 트래킹 시스템 필요)", "安装抚摸声音系统到脚部（需要全身追踪系统才能使用）", "安裝撫摸聲音系統到腳部（需要全身追蹤系統才能使用）" };

        public static string[] notSelectAvatarTitle = new string[]{
            "Nade System Install Error",
            "撫でギミック導入エラー",
            "네이드 시스템 설치 오류",
            "抚摸系统安装错误",
            "撫摸系統安裝錯誤"
        };
        public static string[] notSelectAvatarMsg = new string[]{
            "Select avatar and then press the Setup button.",
            "アバターを選択してからセットアップボタンを押してください。",
            "아바타를 선택한 후 설정 버튼을 눌러주세요.",
            "请先选择虚拟形象，然后再按设置按钮。",
            "請先選擇虛擬形象，然後再按設定按鈕。"
        };

        public static string[] installCompleteTitle = new string[]{
            "Nade System Install Complete",
            "撫で音ギミック導入完了",
            "네이드 시스템 설치 완료",
            "抚摸系统安装完成",
            "撫摸系統安裝完成"
        };
        public static string[] installCompleteMsg = new string[]{
            "NadeSystem install complete!",
            "NadeSystemの導入が完了しました。",
            "NadeSystem 설치가 완료되었습니다.",
            "NadeSystem 安装完成。",
            "NadeSystem 安裝完成。"
        };

    }

    public class MenuLocalize
    {
        public static string[] topMenu = new string[] { "", "RNW Petting Sound", "赤夜式撫で音", "적야식 쓰다듬기 사운드", "赤夜式抚摸音", "赤夜式撫摸音" };
        public static string[] nadeMenu = new string[] { "NadeControl", "Petting Settings", "撫で設定", "쓰다듬기 설정", "抚摸设置", "撫摸設定" };
        public static string[] naderareMenu = new string[] { "NaderareControl", "Being Petted Settings", "撫でられ設定", "쓰다듬히기 설정", "被抚摸设置", "被撫摸設定" };
        public static string[] shadowEnable = new string[] { "NadeShadowMenu", "Enable Shadow", "影を有効化", "그림자 활성화", "启用阴影", "啟用陰影" };
        public static string[] rightHandEnable = new string[] { "RightHandEnable", "Enable RightHand", "右手を有効化", "오른손 활성화", "启用右手", "啟用右手" };
        public static string[] leftHandEnable = new string[] { "LeftHandEnable", "Enable LeftHand", "左手を有効化", "왼손 활성화", "启用左手", "啟用左手" };
        public static string[] nadeHandSync = new string[] { "NadeHandSync", "Volume Sync with Hand Movement", "手の動きに音量を同期", "손 움직임에 볼륨 동기화", "与手部动作同步音量", "與手部動作同步音量" };
        public static string[] nadeVolume = new string[] { "NadeVolume", "Volume", "音量", "볼륨", "音量", "音量" };
        public static string[] nadeSoundSelect = new string[] { "NadeSound", "Nade Sound Select", "撫で音選択", "쓰다듬기 사운드 선택", "抚摸声音选择", "撫摸聲音選擇" };
        public static string[] footEnable = new string[] { "FootEnable", "Enable Foot", "足を有効化", "발 활성화", "启用脚部", "啟用腳部" };
        public static string[] nadeSphere = new string[] { "NadeSphereSystemMenu", "Nade Sphere", "撫でスフィア", "네이드 스피어", "抚摸球", "撫摸球" };
        public static string[] nadeSphereEnable = new string[] { "NadeSphereEnable", "Enable", "有効化", "활성화", "启用", "啟用" };
        public static string[] nadeSphereLock = new string[] { "PositionLock", "Position Lock", "位置ロック", "위치 고정", "位置锁定", "位置鎖定" };
        public static string[] naderareEnable = new string[] { "NaderareEnable", "Enable Being Petted Sound", "撫でられ音を有効化", "쓰다듬히기 사운드 활성화", "启用被抚摸声音", "啟用被撫摸聲音" };
        public static string[] naderareHandSync = new string[] { "NaderareHandSync", "Volume Sync with Hand Movement", "手の動きに音量を同期", "손 움직임에 볼륨 동기화", "与手部动作同步音量", "與手部動作同步音量" };
        public static string[] naderareVolume = new string[] { "NaderareVolume", "Volume", "音量", "볼륨", "音量", "音量" };
        public static string[] naderareSoundSelect = new string[] { "NaderareSound", "Being Petted Sound Select", "撫でられ音選択", "쓰다듬히기 사운드 선택", "被抚摸声音选择", "被撫摸聲音選擇" };
    }
}
