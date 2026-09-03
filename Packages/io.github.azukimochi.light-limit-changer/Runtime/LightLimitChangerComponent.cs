using System.Collections.Generic;
using VRC.SDKBase;

namespace io.github.azukimochi
{
    [DisallowMultipleComponent]
    [AddComponentMenu("NDMF/Light Limit Changer")]
    public sealed class LightLimitChangerComponent : MonoBehaviour, IEditorOnly, ISettingsProvider
    {
        // コンポーネント作成時のバージョン マイグレーションとかに使える？
        [HideInInspector]
        public SemVer Version = EditorMarshal.GetCurrentVersion?.Invoke() ?? default;

        [HideInInspector]
        [SerializeField]
        private string presetName;

        public string PresetName
        {
            get => string.IsNullOrEmpty(presetName) ? gameObject.name : presetName;
            set => presetName = value;
        }

        /// <summary>
        /// 基本設定
        /// </summary>
        public GeneralSettings General;

        // シェーダー固有の設定たち
        public LilToonSettings LilToon;
        public PoiyomiSettings Poiyomi;
        public UnlitWFSettings UnlitWF;

        /// <summary>
        /// メニュー設定
        /// </summary>
        public MenuSettings MenuSettings;

        /// <summary>
        /// 除外設定
        /// </summary>
        public List<ExcludeOptions> Excludes = new();

        /// <summary>
        /// WDの設定
        /// </summary>
        public WriteDefaultsSetting WriteDefaults = WriteDefaultsSetting.MatchAvatar;

        /// <summary>
        /// 対象シェーダー
        /// </summary>
        public TargetShaderContainer TargetShader = TargetShaderContainer.Everything;

        /// <summary>
        /// メニューテキストの色
        /// </summary>
        public Color MenuTextColor = Color.white;

        /// <summary>
        /// メニューアイコン上書き
        /// </summary>
        public Texture2D MenuIcon = null;

        /// <summary>
        /// メッシュ設定
        /// </summary>
        public MeshSettings MeshSettings;

        /// <summary>
        /// デバッグ設定
        /// </summary>
        public DebugOptions DebugOptions = new();

        IEnumerable<ISettingsProvider> ISettingsProvider.Children
        {
            get
            {
                yield return General;
                yield return LilToon;
                yield return Poiyomi;
                yield return UnlitWF;
            }
        }
    }
}