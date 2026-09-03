using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;

namespace io.github.azukimochi;

internal abstract class BasePrefs<T> : ScriptableObject, IPreferences, ISerializationCallbackReceiver where T : BasePrefs<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = CreateInstance<T>();
                instance.Reflesh();
            } 
            return instance;
        }
    }

    public abstract string FilePath { get; }
    public abstract bool UsePreferenceDirectory { get; }

    #region Members

    [SerializeField]
    private Preset[] Presets;

    Dictionary<string, string> IPreferences.Presets => presetDict;

    private Dictionary<string, string> presetDict;

    public void OnAfterDeserialize()
    {
        if (Presets == null)
            presetDict = new();
        else
            presetDict = Presets.ToDictionary(x => x.Name, x => x.Data);
    }

    public void OnBeforeSerialize()
    {
        if (presetDict == null)
            Presets = Array.Empty<Preset>();
        else
            Presets = presetDict.Select(x => new Preset(x.Key, x.Value)).ToArray();
    }

    #endregion

    public void Save() => Save(true);

    internal void Reflesh()
    {
        var path = GetFilePath();
        var obj = InternalEditorUtility.LoadSerializedFileAndForget(path)?.FirstOrDefault();
        if (obj == null)
        {
            OnAfterDeserialize();
            return;
        }
        EditorUtility.CopySerializedIfDifferent(obj, this);
        OnAfterDeserialize();
        DestroyImmediate(obj);
    }

    protected void Save(bool saveAsText)
    {
        string filePath = GetFilePath();
        string directoryName = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        OnBeforeSerialize();
        InternalEditorUtility.SaveToSerializedFileAndForget(new[] { this }, filePath, saveAsText);
    }

    protected string GetFilePath()
    {
        return Path.Join(UsePreferenceDirectory ? InternalEditorUtility.unityPreferencesFolder : "", FilePath);
    }

    [Serializable]
    private struct Preset
    {
        public string Name;
        public string Data;

        public Preset(string name, string data)
        {
            Name = name;
            Data = data;
        }
    }
}
