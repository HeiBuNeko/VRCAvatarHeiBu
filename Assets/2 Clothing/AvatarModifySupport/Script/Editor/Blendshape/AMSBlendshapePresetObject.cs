using UnityEngine;

namespace com.ams.avatarmodifysupport.preset
{
    public class AMSBlendshapePresetObject : ScriptableObject
    {
        public string json;
        public string version;

        public AMSBlendshapePresetObject(string _json, string version)
        {
            json = _json;
        }
        //TODO: Preset GUI
    }
}