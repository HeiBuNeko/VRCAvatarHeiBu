using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace io.github.azukimochi;

public interface ISettingsProvider
{
    IEnumerable<ISettingsProvider> Children => Enumerable.Empty<ISettingsProvider>();
}

public interface ISettings : ISettingsProvider
{
}

internal interface ITogglable
{
    bool Enable { get; set; }
}

[Serializable]
public abstract class Settings<T> : ISettings, ITogglable where T : Settings<T>
{
    [HideInInspector]
    public bool Enable = typeof(T).GetCustomAttribute<SettingOptionsAttribute>().Enabled;

    bool ITogglable.Enable
    {
        get => Enable; 
        set => Enable = value;
    }
}