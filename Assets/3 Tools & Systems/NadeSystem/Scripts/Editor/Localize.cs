using UnityEditor;

namespace RedNightWorks.NadeSystem
{
    public static class Localize
    {
        public static readonly string[] menuLanguageLabel = {
            "Menu language", "メニュー言語", "메뉴 언어", "菜单语言", "選單語言"
        };
        private static readonly string[] automaticLanguage = {
            "Automatic (OS language)", "自動（OSの言語）", "자동 (OS 언어)", "自动（系统语言）", "自動（系統語言）"
        };
        public static string[] MenuLanguageOptions(int uiLanguage) => new[] {
            automaticLanguage[uiLanguage], "English", "日本語", "한국어", "简体中文", "繁體中文"
        };
        public static readonly string[] resetDefaultsTooltip = {
            "Reset the Installer settings to defaults", "Installer内の設定をデフォルトに戻す",
            "Installer 내 설정을 기본값으로 재설정", "将 Installer 内的设置恢复为默认值", "將 Installer 內的設定恢復為預設值"
        };
        public static readonly string[] resetDefaults = {
            "Reset to defaults", "デフォルトに戻す", "기본값으로 재설정", "恢复默认设置", "恢復預設設定"
        };
        public static readonly string[] loadAvatarAudioClips = {
            "Load sounds from the avatar", "アバターの音源設定を読み込む", "아바타의 음원 설정 불러오기",
            "读取虚拟形象的音源设置", "讀取虛擬形象的音源設定"
        };
        public static readonly string[] parameterBudgetTitle = {
            "Synced parameter limit exceeded", "同期パラメーター上限超過", "동기화 파라미터 한도 초과",
            "同步参数超出上限", "同步參數超出上限"
        };
        public static readonly string[] parameterBudgetUsage = {
            "{0} / {1} bits (estimated) — {2} over",
            "{0} / {1} bits（推定）・{2} bits超過",
            "{0} / {1} bits (추정) · {2} bits 초과",
            "{0} / {1} bits（预计）· 超出 {2} bits",
            "{0} / {1} bits（預估）· 超出 {2} bits"
        };
        public static readonly string[] parameterBudgetExceeded = {
            "The number of synced parameters may prevent uploading. We recommend reducing the number of avatar gimmicks or taking similar measures.",
            "同期パラメーターが多いためアップロードできない可能性があります。ギミックを減らすなどの対策をおすすめします。",
            "동기화 파라미터가 많아 업로드하지 못할 수 있습니다. 아바타 기믹을 줄이는 등의 조치를 권장합니다.",
            "同步参数较多，可能无法上传。建议减少虚拟形象上的功能组件等。",
            "同步參數較多，可能無法上傳。建議減少虛擬形象上的功能元件等。"
        };
        public static readonly string[] parameterBudgetUnavailable = {
            "Unable to estimate synced parameter usage. Check it in the SDK build panel before uploading.",
            "同期パラメーターの使用量を推定できません。アップロード前にSDKのビルド画面で確認してください。",
            "동기화 파라미터 사용량을 추정할 수 없습니다. 업로드 전에 SDK 빌드 패널에서 확인하세요.",
            "无法估算同步参数用量。请在上传前通过 SDK 构建面板确认。",
            "無法估算同步參數用量。請在上傳前透過 SDK 建置面板確認。"
        };
        public static readonly string[] additionalFeatures = {
            "Additional features & shadows", "追加機能・影の設定", "추가 기능 및 그림자", "附加功能与阴影", "附加功能與陰影"
        };
        public static readonly string[] compatibilitySettings = {
            "Detailed settings", "詳細設定", "세부 설정", "详细设置", "詳細設定"
        };
        public static readonly string[] installButton = {
            "Install on avatar", "アバターに導入する", "아바타에 설치", "安装到虚拟形象", "安裝到虛擬形象"
        };
        public static readonly string[] selectAvatarHint = {
            "Select an avatar in the Hierarchy, or drag it into the field above.",
            "Hierarchyでアバターを選択するか、上の欄へドラッグしてください。",
            "Hierarchy에서 아바타를 선택하거나 위 필드로 드래그하세요.",
            "在 Hierarchy 中选择虚拟形象，或将其拖到上方字段。",
            "在 Hierarchy 中選擇虛擬形象，或將其拖到上方欄位。"
        };
        public static readonly string[] sceneAvatarHint = {
            "Choose an avatar placed in the scene.", "シーンに配置したアバターを選んでください。",
            "씬에 배치된 아바타를 선택하세요.", "请选择放置在场景中的虚拟形象。", "請選擇放置在場景中的虛擬形象。"
        };
        public static readonly string[] soundSettings = {
            "Sound Settings", "音源設定", "음원 설정", "音源设置", "音源設定"
        };
        public static readonly string[] soundSlot = {
            "Sound", "音源", "음원", "音源", "音源"
        };
        public static readonly string[] soundSettingsHelp = {
            "You can also configure sounds in the NadeSystem Inspector after installation.",
            "導入後もNadeSystemのInspectorで設定できます",
            "설치 후에도 NadeSystem Inspector에서 설정할 수 있습니다.",
            "安装后也可在 NadeSystem Inspector 中设置。",
            "安裝後也可在 NadeSystem Inspector 中設定。"
        };
        public static readonly string[] soundSettingsUnavailable = {
            "Unable to load the default sounds. Check that the NadeSystem prefab and its settings component are present.",
            "標準音源を読み込めません。NadeSystemのPrefabと設定コンポーネントが存在するか確認してください。",
            "기본 음원을 불러올 수 없습니다. NadeSystem 프리팹과 설정 컴포넌트가 있는지 확인하세요.",
            "无法加载默认音源。请检查 NadeSystem 预制件及其设置组件是否存在。",
            "無法載入預設音源。請檢查 NadeSystem 預製件及其設定元件是否存在。"
        };
        public static readonly string[][] writeDefaultsOptions = {
            new[] { "Use prefab settings", "Force ON", "Force OFF" },
            new[] { "既定の設定を使う", "ONに固定", "OFFに固定" },
            new[] { "프리팹 설정 사용", "ON으로 고정", "OFF로 고정" },
            new[] { "使用预制件设置", "强制 ON", "强制 OFF" },
            new[] { "使用預製件設定", "強制 ON", "強制 OFF" }
        };
        public static readonly string[] writeDefaultsHelp = {
            "Applied at build time to this system, including optional features. Modular Avatar may keep layers requiring Write Defaults ON enabled.",
            "ビルド時に追加機能を含む本ギミックへ適用します。Modular AvatarがONを必須とするレイヤーはONが優先されます。",
            "빌드 시 추가 기능을 포함한 이 시스템에 적용됩니다. Modular Avatar에서 ON이 필요한 레이어는 ON으로 유지될 수 있습니다.",
            "构建时应用于本系统及附加功能。Modular Avatar 要求 ON 的层可能保持 ON。",
            "建置時套用於本系統及附加功能。Modular Avatar 要求 ON 的層可能保持 ON。"
        };
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
        public static void Apply(NadeSystemSettings settings)
        {
            int language = (int)settings.menuLanguage;
            if (language < 1 || language > 5)
            {
                switch (UnityEngine.Application.systemLanguage)
                {
                    case UnityEngine.SystemLanguage.Japanese: language = 2; break;
                    case UnityEngine.SystemLanguage.Korean: language = 3; break;
                    case UnityEngine.SystemLanguage.Chinese:
                    case UnityEngine.SystemLanguage.ChineseSimplified: language = 4; break;
                    case UnityEngine.SystemLanguage.ChineseTraditional: language = 5; break;
                    default: language = 1; break;
                }
            }

            // Only change display labels on the build copy. Paths and parameters are stable.
            SetLabel("", topMenu);
            SetLabel("ExMenu/Nade Control", nadeMenu);
            SetLabel("ExMenu/Naderare Control", naderareMenu);
            SetLabel("ExMenu/NadeShadowMenu", shadowEnable);
            SetLabel("ExMenu/Nade Control/Right Hand Enable", rightHandEnable);
            SetLabel("ExMenu/Nade Control/Left Hand Enable", leftHandEnable);
            SetLabel("ExMenu/Nade Control/Nade Hand Sync", nadeHandSync);
            SetLabel("ExMenu/Nade Control/Nade Volume", nadeVolume);
            SetLabel("ExMenu/Nade Control/Nade Sound", nadeSoundSelect);
            SetLabel("ExMenu/Nade Control/FootSystemMenu", footEnable);
            SetLabel("ExMenu/Nade Control/NadeSphereSystemMenu", nadeSphere);
            SetLabel("ExMenu/Nade Control/NadeSphereSystemMenu/Enable", nadeSphereEnable);
            SetLabel("ExMenu/Nade Control/NadeSphereSystemMenu/Position Lock", nadeSphereLock);
            SetLabel("ExMenu/Naderare Control/Enable", naderareEnable);
            SetLabel("ExMenu/Naderare Control/Naderare Hand Sync", naderareHandSync);
            SetLabel("ExMenu/Naderare Control/Naderare Volume", naderareVolume);
            SetLabel("ExMenu/Naderare Control/Naderare Sound", naderareSoundSelect);

            void SetLabel(string path, string[] labels)
            {
                var target = string.IsNullOrEmpty(path) ? settings.transform : settings.transform.Find(path);
                if (target == null) return; // Optional features may not be installed.
                var item = target.GetComponent<nadena.dev.modular_avatar.core.ModularAvatarMenuItem>();
                if (item != null) item.label = labels[language];
            }
        }

        public static string[] topMenu = new string[] { "", "RNW Petting Sound", "<color=red>赤夜式</color>撫で音", "<color=red>적야식</color> 쓰다듬기 사운드", "<color=red>赤夜式</color>抚摸音", "<color=red>赤夜式</color>撫摸音" };
        public static string[] nadeMenu = new string[] { "NadeControl", "Pet others", "撫でる", "쓰다듬기", "抚摸", "撫摸" };
        public static string[] naderareMenu = new string[] { "NaderareControl", "Be petted", "撫でられる", "쓰다듬받기", "被抚摸", "被撫摸" };
        public static string[] shadowEnable = new string[] { "NadeShadowMenu", "Shadow", "影", "그림자", "阴影", "陰影" };
        public static string[] rightHandEnable = new string[] { "RightHandEnable", "Right hand", "右手", "오른손", "右手", "右手" };
        public static string[] leftHandEnable = new string[] { "LeftHandEnable", "Left hand", "左手", "왼손", "左手", "左手" };
        public static string[] nadeHandSync = new string[] { "NadeHandSync", "Motion volume", "動きに連動", "움직임 연동", "动作联动", "動作連動" };
        public static string[] nadeVolume = new string[] { "NadeVolume", "Volume", "音量", "볼륨", "音量", "音量" };
        public static string[] nadeSoundSelect = new string[] { "NadeSound", "Sound", "音源", "음원", "音源", "音源" };
        public static string[] footEnable = new string[] { "FootEnable", "Feet", "足", "발", "脚", "腳" };
        public static string[] nadeSphere = new string[] { "NadeSphereSystemMenu", "Camera petting", "カメラなで", "카메라 쓰다듬기", "相机抚摸", "相機撫摸" };
        public static string[] nadeSphereEnable = new string[] { "NadeSphereEnable", "Enable", "有効化", "활성화", "启用", "啟用" };
        public static string[] nadeSphereLock = new string[] { "PositionLock", "Position Lock", "位置ロック", "위치 고정", "位置锁定", "位置鎖定" };
        public static string[] naderareEnable = new string[] { "NaderareEnable", "Enable", "有効化", "활성화", "启用", "啟用" };
        public static string[] naderareHandSync = new string[] { "NaderareHandSync", "Motion volume", "動きに連動", "움직임 연동", "动作联动", "動作連動" };
        public static string[] naderareVolume = new string[] { "NaderareVolume", "Volume", "音量", "볼륨", "音量", "音量" };
        public static string[] naderareSoundSelect = new string[] { "NaderareSound", "Sound", "音源", "음원", "音源", "音源" };
    }
}
