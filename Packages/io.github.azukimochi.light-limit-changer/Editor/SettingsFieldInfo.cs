using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace io.github.azukimochi;

internal class SettingsFieldInfo
{
    private static readonly Dictionary<Type, SettingsFieldInfo> infos = new();
    public SettingOptionsAttribute Options { get; }

    public SettingsFieldInfo Parent { get; }

    public string Id { get; }
    public string MenuPath { get; }
    public string ParameterPrefix { get; }
    public (RuntimeMessageType Type, string Message)[] Messages { get; }

    public bool HasChildren { get; }

    public SettingsFieldInfo(Type type)
    {
        if (type == null || !typeof(ISettings).IsAssignableFrom(type))
            return;

        if (typeof(ISettings).IsAssignableFrom(type.DeclaringType))
            Parent = new SettingsFieldInfo(type.DeclaringType);

        Options = type.GetCustomAttribute<SettingOptionsAttribute>();
        if (Options is null)
        {
            Id = type.FullName;
            MenuPath = type.Name;
            ParameterPrefix = type.Name;
        }
        else if (Parent is SettingsFieldInfo parent)
        {
            Id = $"{parent.Id}/{Options.Id}"; 
            MenuPath = $"{parent.MenuPath}//{Options.MenuPath}";
            ParameterPrefix = $"{parent.ParameterPrefix}/{Options.ParameterPrefix}";
        }
        else
        {
            Id = Options.Id;
            MenuPath = Options.MenuPath;
            ParameterPrefix = Options.ParameterPrefix;
        }

        Messages = type.GetCustomAttributes<ShowInfoBoxAttribute>().Select(x => (x.Type, x.Message)).ToArray();
        HasChildren = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Any(x => typeof(ISettings).IsAssignableFrom(x.FieldType));

        infos.TryAdd(type, this);
    }

    public static SettingsFieldInfo GetInfo(Type type)
    {
        if (infos.TryGetValue(type, out var info))
            return info;

        info = new SettingsFieldInfo(type);
        return info;
    }
}

internal sealed class SettingsFieldInfo<TSettings> : SettingsFieldInfo where TSettings : ISettings
{
    public static SettingsFieldInfo Instance { get; }

    public static new string Id => Instance.Id;
    public static new string MenuPath => Instance.MenuPath;
    public static new string ParameterPrefix => Instance.ParameterPrefix;
    public static new (RuntimeMessageType Type, string Message)[] Messages => Instance.Messages;

    static SettingsFieldInfo()
    {
        Instance = GetInfo(typeof(TSettings));
    }

    private SettingsFieldInfo() : base(typeof(TSettings))
    {
    }
}