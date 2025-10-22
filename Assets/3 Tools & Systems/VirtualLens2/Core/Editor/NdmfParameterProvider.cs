#if WITH_NDMF_1_4

using System.Collections.Generic;
using System.Collections.Immutable;
using nadena.dev.ndmf;
using UnityEngine;

namespace VirtualLens2
{
    [ParameterProviderFor(typeof(VirtualLensSettings))]
    internal class NdmfParameterProvider : IParameterProvider
    {
        private const string ParameterPrefix = Constants.ParameterPrefix;
        private readonly VirtualLensSettings _settings;

        public NdmfParameterProvider(VirtualLensSettings settings)
        {
            _settings = settings;
        }

        public IEnumerable<ProvidedParameter> GetSuppliedParameters(BuildContext context = null)
        {
            var ret = new List<ProvidedParameter>();
            
            void AddParameter(string name, AnimatorControllerParameterType type)
            {
                ret.Add(new ProvidedParameter(
                    ParameterPrefix + name, ParameterNamespace.Animator,
                    _settings, ApplyNonDestructive.Instance, type)
                {
                    IsAnimatorOnly = false,
                    WantSynced = true,
                    IsHidden = false,
                });
            }
            
            // TODO Add non-synchronized parameters
            AddParameter("Control", AnimatorControllerParameterType.Int);
            if (_settings.synchronizeFocalLength)
            {
                AddParameter("Zoom", AnimatorControllerParameterType.Float);
            }
            if (_settings.enableBlurring && _settings.synchronizeFNumber)
            {
                AddParameter("Aperture", AnimatorControllerParameterType.Float);
            }
            if (_settings.enableBlurring && _settings.enableManualFocusing && _settings.synchronizeFocusDistance)
            {
                AddParameter("Distance", AnimatorControllerParameterType.Float);
            }
            if (_settings.enableExposure && _settings.synchronizeExposure)
            {
                AddParameter("Exposure", AnimatorControllerParameterType.Float);
            }
            
            return ret;
        }

        public void RemapParameters(ref ImmutableDictionary<(ParameterNamespace, string), ParameterMapping> nameMap,
            BuildContext context = null)
        {
            // TODO Add identity mapping?
        }
    }
}

#endif
