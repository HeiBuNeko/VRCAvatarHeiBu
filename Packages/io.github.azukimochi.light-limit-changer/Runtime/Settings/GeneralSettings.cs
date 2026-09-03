using System.Collections.Generic;

namespace io.github.azukimochi;

[Serializable]
public sealed class GeneralSettings : ISettingsProvider
{
    /// <summary>
    /// パラメータを圧縮する
    /// </summary>
    public bool CompressionExpressionParameters = true;

    /// <summary>
    /// パラメータ同期を開始する周期（秒単位）
    /// </summary>
    public float ParameterSyncFrequency = 60 / 60f;

    /// <summary>
    /// 手動同期をするためのボタンをメニューに追加する
    /// </summary>
    public bool AddManualSyncButton = false;

    /// <summary>
    /// マテリアル内の同一参照テクスチャを置き換えるのを許可する
    /// </summary>
    public bool AllowForceOverrideSameReference = true;

    /// <summary>
    /// EXメニューから保存・読み出しできるプリセットを生成する
    /// </summary>
    public bool GenerateDynamicPreset = true;

    /// <summary>
    /// 保存スロットの生成数
    /// </summary>
    [UnityEngine.Range(1, 8)]
    public int DynamicPresetSlotCount = 2;

    /// <summary>
    /// 初期値プリセットを生成する
    /// </summary>
    public bool GenerateDefaultPreset = true;
    
    /// <summary>
    /// パーティクルシステムもアニメーション対象に追加するか
    /// </summary>
    public bool IncludeParticleSystem = false;

    public LightingSettings LightingControl = new();
    public ColorControlSettings ColorControl = new();

    IEnumerable<ISettingsProvider> ISettingsProvider.Children
    {
        get
        {
            yield return LightingControl;
            yield return ColorControl;
        }
    } 
}
