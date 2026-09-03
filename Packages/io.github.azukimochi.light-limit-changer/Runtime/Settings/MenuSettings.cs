using System.Collections.Generic;

namespace io.github.azukimochi;

[Serializable]
public sealed class MenuSettings
{
    public List<string> FavoriteParameterIds = new();

    /// <summary>
    /// グループごとに分けられたメニュー項目を全てルートに展開する
    /// </summary>
    public bool UnfoldGroupMenus = false;
}