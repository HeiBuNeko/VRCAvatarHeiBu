namespace io.github.azukimochi;

/// <summary>
/// マテリアル値の自動上書きを無効にする
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class DisableOverwriteMaterialValueAttribute : Attribute { }