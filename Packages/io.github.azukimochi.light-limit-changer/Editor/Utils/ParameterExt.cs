using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nadena.dev.modular_avatar.core;
using UnityEngine;

namespace io.github.azukimochi;

internal static class ParameterExt
{
    internal static int GetValues(this Parameter parameter, Span<float> destination)
    {
        return parameter switch
        {
            Parameter<int> x => Int32(x, destination),
            Parameter<bool> x => Bool(x, destination),
            Parameter<float> x => Single(x, destination),
            Parameter<Vector4> x => Vector4OrColor(x, destination),
            Parameter<Color> x => Vector4OrColor(x, destination),
            _ => 0,
        };

        static int Int32(Parameter<int> parameter, Span<float> destination)
        {
            destination[0] = parameter.Value;
            return 1;
        }
        static int Bool(Parameter<bool> parameter, Span<float> destination)
        {
            destination[0] = parameter.Value ? 1 : 0;
            return 1;
        }
        static int Single(Parameter<float> parameter, Span<float> destination)
        {
            destination[0] = parameter.Value;
            return 1;
        }
        static int Vector4OrColor<T>(Parameter<T> parameter, Span<float> destination)
        {
            var value = parameter.Value;
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, float>(ref value), 4).CopyTo(destination);
            return 4;
        }
    }

    internal static void SetValue(this Parameter parameter, ReadOnlySpan<float> values)
    {
        switch (parameter)
        {
            case Parameter<float> x:
                {
                    x.Value = values[0];
                }
                break;

            case Parameter<int> x:
                {
                    x.Value = (int)values[0];
                }
                break;

            case Parameter<bool> x:
                {
                    x.Value = values[0] != 0;
                }
                break;

            case Parameter<Color> x:
                {
                    x.Value = MemoryMarshal.Read<Color>(MemoryMarshal.AsBytes(values));
                }
                break;

            case Parameter<Vector4> x:
                {
                    x.Value = MemoryMarshal.Read<Vector4>(MemoryMarshal.AsBytes(values));
                }
                break;
        }
    }

    internal static T GetValueDirect<T>(this Parameter parameter) => (parameter as Parameter<T>).Value;

    public static ReadOnlySpan<string> GetPropertyNames(this Metadata.ParameterInfo fieldInfo, ShaderProcessor processor)
    {
        ReadOnlySpan<string> propertyNames;
        if (fieldInfo.MaterialProperties.TryGetValue(processor.QualifiedName, out var propertyNamesArray))
            propertyNames = propertyNamesArray;
        else
            propertyNames = processor.GetMaterialPropertyNames(fieldInfo.Id);

        return propertyNames;
    }

    private static readonly string[] parameterNamePostfixes = { "", ".x", ".y", ".z", ".w", ".r", ".g", ".b", ".a", ".intensity" };

    public static string GetParameterNamePostfix(this Metadata.ParameterInfo parameterInfo, int index)
    {
        return 
            parameterInfo.ParameterType == typeof(Vector4) ? parameterNamePostfixes[1 + index] :
            parameterInfo.ParameterType == typeof(Color) ? parameterNamePostfixes[5 + index] :
            string.Empty;
    }

    public static ParameterConfig[] GetAvatarParameters(this Metadata.ParameterInfo info, LightLimitChangerComponent source)
    {
        var parameter = info.Get(source);
        if (info.Id == GeneralControlIDs.LightDirection && source.General.LightingControl.AllowAdvancedLightDirectionControl)
        {
            return new ParameterConfig[3]
            {
                CreateParameter($"{info.ParameterName}.X", parameter.Saved, !parameter.Synced),
                CreateParameter($"{info.ParameterName}.Y", parameter.Saved, !parameter.Synced),
                CreateParameter($"{info.ParameterName}.Enable", parameter.Saved, !parameter.Synced, asBool: true)
            };

            static ParameterConfig CreateParameter(string name, bool saved, bool localOnly, bool asBool = false) => new()
            {
                nameOrPrefix = name,
                remapTo = $"{LightLimitChangerProcessor.AvatarParameterNamePrefix}{name}",
                defaultValue = asBool ? 0 : 0.5f,
                syncType = asBool ? ParameterSyncType.Bool : ParameterSyncType.Float,
                saved = saved,
                localOnly = localOnly,
            };
        }

        var ranges = info.GetRanges(source);

        var parameterBuffer = (stackalloc float[5]); 
        ReadOnlySpan<float> values = parameterBuffer;

        if (info.ParameterType != typeof(Color))
        {
            values = parameterBuffer[..parameter.GetValues(parameterBuffer)];
        }
        else
        {
            var color = parameter.GetValueDirect<Color>();
            if (color.maxColorComponent > 1)
            {
                var normalized = Unsafe.As<Color, Vector3>(ref color) / color.maxColorComponent;

                parameterBuffer[0] = normalized.x;
                parameterBuffer[1] = normalized.y;
                parameterBuffer[2] = normalized.z;
                parameterBuffer[3] = color.a;
                parameterBuffer[4] = MathF.Max(0, color.maxColorComponent - 1);

                values = parameterBuffer[..5];
                ranges = new(ranges, (span, ranges) =>
                {
                    span[4] = new(0, ranges[4].y - 1);
                });
            }
            else
            {
                parameter.GetValues(parameterBuffer);
                parameterBuffer[4] = 0;
                values = parameterBuffer[..5];
            }
        }


        var t = info.ParameterType;

        var result = new ParameterConfig[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            var postfix = parameterNamePostfixes[parameter switch { Parameter<Vector4> => 1, Parameter<Color> => 5, _ => 0 } + i];
            var avatarParameter = new ParameterConfig()
            {
                nameOrPrefix = $"{info.ParameterName}{postfix}",
                remapTo = $"{LightLimitChangerProcessor.AvatarParameterNamePrefix}{info.ParameterName}{postfix}",
                defaultValue = Utils.NormalizeInRange(values[i], ranges[i].x, ranges[i].y),
                syncType =
                    t == typeof(bool) ? ParameterSyncType.Bool :
                    t == typeof(int) ? ParameterSyncType.Int :
                    t == typeof(float) || t == typeof(Vector4) || t == typeof(Color) ? ParameterSyncType.Float :
                ParameterSyncType.NotSynced,
                saved = parameter.Saved,
                localOnly = !parameter.Synced,
            };
            result[i] = avatarParameter;
        }

        return result;
    }
}