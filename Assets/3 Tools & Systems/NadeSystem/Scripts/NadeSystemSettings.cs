using UnityEngine;
using VRC.SDKBase;

namespace RedNightWorks.NadeSystem
{
    public enum Language
    {
        English,
        Japanese
    }

    public class NadeSystemSettings : MonoBehaviour, IEditorOnly
    {
        public AudioClip[] audioClips = new AudioClip[16];
        public GameObject nadeSoundListTarget;
        public GameObject naderareSoundListTarget;

    }
}
