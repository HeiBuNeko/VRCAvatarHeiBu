namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class SettingOptionsAttribute : Attribute
{
    public SettingOptionsAttribute(string id, string menuPath, string parameterPrefix = null, bool enabled = true)
    {
        Id = id;
        MenuPath = menuPath;
        ParameterPrefix = parameterPrefix;
        Enabled = enabled;
    }

    /// <summary>
    /// ローカライズとかに使うID
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// メニューのパス
    /// </summary>
    public string MenuPath { get; }

    /// <summary>
    /// パラメーター名の接頭辞
    /// </summary>
    public string ParameterPrefix { get; }

    /// <summary>
    /// 初期状態で有効になっているか
    /// </summary>
    public bool Enabled { get; }
}