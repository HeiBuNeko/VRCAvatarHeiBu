using UnityEngine;
using VRC.SDKBase;

namespace RedNightWorks.NadeSystem
{
    public enum Language
    {
        English,
        Japanese
    }

    public enum MenuLanguage
    {
        Automatic = 0,
        English = 1,
        Japanese = 2,
        Korean = 3,
        ChineseSimplified = 4,
        ChineseTraditional = 5
    }

    public enum WriteDefaultsMode
    {
        UsePrefabSettings = 0,
        ForceOn = 1,
        ForceOff = 2
    }

    public class NadeSystemSettings : MonoBehaviour, IEditorOnly
    {
        public MenuLanguage menuLanguage = MenuLanguage.Automatic;
        public WriteDefaultsMode writeDefaultsMode = WriteDefaultsMode.UsePrefabSettings;
        public AudioClip[] audioClips = new AudioClip[16];
        public GameObject nadeSoundListTarget;
        public GameObject naderareSoundListTarget;

    }
}
