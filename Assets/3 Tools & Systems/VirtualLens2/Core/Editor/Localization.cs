using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualLens2.AV3EditorLib;

#if WITH_NDMF_1_3
using nadena.dev.ndmf.localization;
#endif

namespace VirtualLens2
{
    public static class Localization
    {
        private static readonly Dictionary<string, string> LocalizationTableSet = new Dictionary<string, string>()
        {
            { "en-US", "ace8e65c647e56842907d2d8e9d99a75" }
        };

        public static LocalizationAsset GetLocalizationTable(string language)
        {
            if (LocalizationTableSet.TryGetValue(language, out var guid))
            {
                return AssetUtility.LoadAssetByGUID<LocalizationAsset>(guid);
            }
            return GetDefaultLocalizationTable();
        }

        public static LocalizationAsset GetDefaultLocalizationTable() { return GetLocalizationTable("en-US"); }

#if WITH_NDMF_1_3
        public static Localizer GetNdmfLocalizer()
        {
            var t = GetDefaultLocalizationTable();
            return new Localizer("en-US",
                () => LocalizationTableSet.Values
                    .Select(AssetUtility.LoadAssetByGUID<LocalizationAsset>)
                    .ToList());
        }
#endif
    }
}
