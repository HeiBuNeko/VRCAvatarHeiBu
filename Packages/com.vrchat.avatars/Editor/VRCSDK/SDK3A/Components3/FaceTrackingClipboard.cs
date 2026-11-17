#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using VRC.SDK3.Avatars.Components;

namespace VRC.SDK3A.Editor
{
    /// <summary>
    /// Portable JSON for face tracking authoring tools. Each configuration is a standalone object
    /// containing a version; multiple objects are separated by a blank line. Enum values use names.
    /// These methods neither access the system clipboard nor modify an Avatar or display dialogs.
    /// </summary>
    public static class FaceTrackingClipboard
    {
        public const int ParameterBehaviorVersion = 2;
        public const int BlendshapesSetVersion = 2;

        static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,
            MaxDepth = 16,
            Converters = { new StringEnumConverter { AllowIntegerValues = false } }
        };

        /// <summary>Serializes all configuration fields in enumeration order.</summary>
        public static string SerializeParameterBehaviors(IEnumerable<VRCAvatarDescriptor.FaceTrackingParameterBehavior> behaviors, VRCAvatarDescriptor sourceAvatar = null)
            => Serialize(behaviors, ParameterBehaviorVersion, sourceAvatar);

        /// <summary>Serializes all set fields, including tracking and viseme mappings, in enumeration order.</summary>
        public static string SerializeBlendshapesSets(IEnumerable<VRCAvatarDescriptor.FaceTrackingBlendshapesSet> sets, VRCAvatarDescriptor sourceAvatar = null)
            => Serialize(sets, BlendshapesSetVersion, sourceAvatar);

        /// <summary>
        /// Parses one or more complete objects. On failure, returns false, an empty list, and an error.
        /// The caller decides where to append the results and enforces destination collection limits.
        /// </summary>
        public static bool TryParseParameterBehaviors(string json,
            out List<VRCAvatarDescriptor.FaceTrackingParameterBehavior> behaviors, out string error)
            => TryParseParameterBehaviors(json, out behaviors, out _, out error);

        /// <summary>Also returns optional source-avatar GlobalObjectIds, parallel to the configurations.</summary>
        public static bool TryParseParameterBehaviors(string json,
            out List<VRCAvatarDescriptor.FaceTrackingParameterBehavior> behaviors, out List<string> sourceAvatars, out string error)
        {
            if (!TryParse(json, ParameterBehaviorVersion, 1024 * 1024, out behaviors, out sourceAvatars, out error, "targetMode", "combineMode"))
                return false;
            foreach (var behavior in behaviors)
            {
                if (!Enum.IsDefined(typeof(VRCAvatarDescriptor.ParameterBehaviorTargetMode), behavior.targetMode)
                    || !Enum.IsDefined(typeof(VRCAvatarDescriptor.ParameterBehaviorCombineMode), behavior.combineMode))
                {
                    behaviors.Clear();
                    sourceAvatars.Clear();
                    error = "Unsupported parameter behavior mode.";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Parses one or more complete objects without resolving names against a particular Avatar.
        /// On failure, returns false, an empty list, and an error. Destination limits are the caller's responsibility.
        /// Names target all configured meshes
        /// </summary>
        public static bool TryParseBlendshapesSets(string json,
            out List<VRCAvatarDescriptor.FaceTrackingBlendshapesSet> sets, out string error)
            => TryParseBlendshapesSets(json, out sets, out _, out error);

        /// <summary>Also returns optional source-avatar GlobalObjectIds. Missing scene objects do not invalidate configurations.</summary>
        public static bool TryParseBlendshapesSets(string json,
            out List<VRCAvatarDescriptor.FaceTrackingBlendshapesSet> sets, out List<string> sourceAvatars, out string error)
            => TryParse(json, BlendshapesSetVersion, 8 * 1024 * 1024, out sets, out sourceAvatars, out error, "setName", "setWeight");

        static string Serialize<T>(IEnumerable<T> entries, int version, VRCAvatarDescriptor sourceAvatar)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            var serializer = JsonSerializer.Create(JsonSettings);
            string sourceId = ReferenceEquals(sourceAvatar, null) ? null : FaceTrackingEditorContext.ObjectId(sourceAvatar);
            return string.Join(Environment.NewLine + Environment.NewLine, entries.Select(entry =>
            {
                if (ReferenceEquals(entry, null))
                    throw new ArgumentException("Configurations cannot be null.", nameof(entries));
                var data = JObject.FromObject(entry, serializer);
                if (!string.IsNullOrEmpty(sourceId))
                    data.AddFirst(new JProperty("sourceAvatar", sourceId));
                data.AddFirst(new JProperty("version", version));
                return data.ToString(Formatting.Indented);
            }));
        }

        static bool TryParse<T>(string json, int version, int maxLength, out List<T> entries, out List<string> sourceAvatars, out string error,
            params string[] requiredFields)
        {
            entries = new List<T>();
            sourceAvatars = new List<string>();
            error = null;
            if (string.IsNullOrWhiteSpace(json) || json.Length > maxLength)
            {
                error = $"JSON text must be non-empty and no longer than {maxLength:N0} characters.";
                return false;
            }

            try
            {
                // Field values can contain blank lines or braces, so text splitting is not sufficient.
                using (var reader = new JsonTextReader(new StringReader(json)) { SupportMultipleContent = true, MaxDepth = 16 })
                {
                    var serializer = JsonSerializer.Create(JsonSettings);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.Comment)
                            continue;
                        var data = JObject.Load(reader);
                        if (!JToken.DeepEquals(data["version"], new JValue(version)))
                            throw new JsonSerializationException("Unsupported configuration version.");
                        data.Remove("version");
                        // Authoring metadata is optional and never participates in runtime name resolution.
                        string source = data["sourceAvatar"]?.Type == JTokenType.String ? (string)data["sourceAvatar"] : null;
                        data.Remove("sourceAvatar");
                        foreach (string field in requiredFields)
                        {
                            if (data[field] == null)
                                throw new JsonSerializationException($"Each configuration must specify {field}.");
                        }
                        entries.Add(data.ToObject<T>(serializer));
                        sourceAvatars.Add(source);
                    }
                }
                if (entries.Count == 0)
                    throw new JsonSerializationException("No configurations were found.");
                return true;
            }
            catch (JsonException exception)
            {
                entries.Clear();
                sourceAvatars.Clear();
                error = exception.Message;
                return false;
            }
        }
    }
}
#endif
