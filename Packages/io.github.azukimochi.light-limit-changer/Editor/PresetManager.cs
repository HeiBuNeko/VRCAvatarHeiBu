using System.Collections.Generic;
using System.Linq;

namespace io.github.azukimochi;

internal sealed partial class PresetManager
{
    public static GameObject Setup(GameObject avatarRoot, Action<LightLimitChangerComponent> factory = null)
    {
        var prefab = DefaultSettings;
        if (prefab != null)
        {
            prefab = PrefabUtility.InstantiatePrefab(prefab, avatarRoot.transform) as GameObject;
            prefab.name = LightLimitChanger.Title;
            if (!prefab.TryGetComponent<LightLimitChangerComponent>(out _))
                prefab.AddComponent<LightLimitChangerComponent>();
        }
        else
        {
            prefab = new GameObject(LightLimitChanger.Title);
            prefab.AddComponent<LightLimitChangerComponent>();
            prefab.transform.parent = avatarRoot.transform;
        }
        Undo.RegisterCreatedObjectUndo(prefab, $"Add {LightLimitChanger.Title}");

        factory?.Invoke(prefab.GetComponent<LightLimitChangerComponent>());

        return prefab;
    }

    private const string DefaultSettingsPrefabGUID = "c34f27003cae48a459266092c574f293";
    public static GameObject DefaultSettings => AssetUtils.FromGUID<GameObject>(DefaultSettingsPrefabGUID);

    public static PresetManager Local { get; } = new(Preferences.Local);

    public static PresetManager Global { get; } = new(Preferences.Global);

    private readonly IPreferences preferences;

    private PresetManager(IPreferences preferences)
    {
        this.preferences = preferences;

        Undo.undoRedoEvent += (in UndoRedoInfo x) =>
        {
            if (x.undoName == "Remove Preset")
            {
                preferences.Save();
            }
        };
    }

    public bool ContainsKey(string key) => preferences.Presets.ContainsKey(key);

    public bool TryAdd(string key, Preset preset)
    {
        if (preferences.Presets.ContainsKey(key))
            return false;
        Update(key, preset);
        return true;
    }

    public void Update(string key, Preset preset)
    {
        var json = JsonUtility.ToJson(preset);
        Undo.RecordObject(preferences as Object, $"Save Preset as {key}");
        preferences.Presets[key] = json;
        preferences.Save();
    }

    public bool TryLoad(string key, LightLimitChangerComponent component, bool createChildPresets = true)
    {
        if (!preferences.Presets.TryGetValue(key, out var json))
            return false;

        using var undo = new UndoScope($"Load Preset {key}");
        Undo.RecordObject(component, $"[LightLimitChangerPreset] Load Preset");
        var preset = JsonUtility.FromJson<EditorPreset>(json);
        JsonUtility.FromJsonOverwrite(preset.Data, component);
        if (!createChildPresets)
            return true;

        foreach(var c in component.GetComponentsInChildren<LightLimitChangerComponent>(true).Skip(1))
        {
            var name = c.PresetName;
            if (preset.Children.Any(x => x.Name == name))
                continue;
            Undo.DestroyObjectImmediate(c.gameObject);
        }

        foreach(var child in preset.Children)
        {
            LightLimitChangerComponent target = component.GetComponentsInChildren<LightLimitChangerComponent>(true).Skip(1).Where(x => x.PresetName == child.Name || x.name == child.Name).FirstOrDefault();
            if (target == null)
            {
                var obj = Setup(component.gameObject);
                if (obj.GetComponent<MAMenuInstaller>() is { } mami)
                {
                    Object.DestroyImmediate(mami);
                }
                obj.name = child.Name;
                Undo.RegisterCreatedObjectUndo(obj, "child");
                target = obj.GetComponent<LightLimitChangerComponent>();
            }
            Undo.RecordObject(component, $"Load Preset {key}");
            JsonUtility.FromJsonOverwrite(child.Data, target);
        }
        return true;
    }

    public bool Remove(string key)
    {
        Undo.RecordObject(preferences as Object, "Remove Preset");
        bool result = preferences.Presets.Remove(key);
        if (result)
            preferences.Save();
        return result;
    }

    public IEnumerable<string> Keys => (preferences.Presets?.Keys as IEnumerable<string>) ?? Array.Empty<string>();

    [Serializable]
    public class Preset
    {
        public string Name;
        public string Data;

        public Preset(LightLimitChangerComponent component)
        {
            Name = component.PresetName;
            Data = JsonUtility.ToJson(component);
        }
    }

    [Serializable]
    public sealed class EditorPreset : Preset
    {
        public Preset[] Children;

        public EditorPreset(LightLimitChangerComponent component) : base(component)
        {
            Children = component.GetComponentsInChildren<LightLimitChangerComponent>(true).Skip(1).Select(x => new Preset(x)).ToArray();
        }
    }
}

