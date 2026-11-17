#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRC.SDK3A.Editor
{
    // Authoring references belong to this project, not to the uploaded configuration.
    [FilePath("Library/VRChat/FaceTrackingAuthoring.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class FaceTrackingAuthoringState : ScriptableSingleton<FaceTrackingAuthoringState>
    {
        [Serializable]
        internal class Entry
        {
            public string key;
            public bool overrideSource;
            public string source;
            public List<string> meshes = new List<string>();
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        internal Entry Find(string key) => entries.Find(entry => entry.key == key);
        internal void Store(Entry entry, bool flush = true)
        {
            if (!entries.Contains(entry))
                entries.Add(entry);
            if (flush)
                Flush();
        }
        internal void Flush() => Save(true);
    }
}
#endif
