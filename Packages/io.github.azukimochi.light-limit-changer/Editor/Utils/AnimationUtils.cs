namespace io.github.azukimochi;

internal static class AnimationUtils
{
    public static void AnimateAllFloat<T>(this ReadOnlyMemory<T> memory, AnimationClip animationClip, string propertyName, AnimationCurve curve) where T : Component
        => memory.Span.AnimateAllFloat(animationClip, propertyName, curve);

    public static void AnimateAllFloat<T>(this ReadOnlySpan<T> span, AnimationClip animationClip, string propertyName, AnimationCurve curve) where T : Component
    {
        foreach(var item in span)
        {
            AnimationUtility.SetEditorCurve(animationClip, item.ToFloatCurve(propertyName), curve);
        }
    }
    private static AnimationCurve _singleton;
    private static readonly Keyframe[] _buffer1 = new Keyframe[1];
    private static readonly Keyframe[] _buffer2 = new Keyframe[2];
    private static readonly Keyframe[] _buffer3 = new Keyframe[3];

    private const float TimeEnd = 1 / 60f;

    public static AnimationCurve ConstantCurve(float value)
    {
        var curve = _singleton;
        if (curve == null)
        {
            return _singleton = AnimationCurve.Constant(0, 0, value);
        }
        _buffer1[0] = new Keyframe(0, value);
        curve.keys = _buffer1;
        return curve;
    }

    public static AnimationCurve LinearCurve(float start, float end)
    {
        var curve = _singleton;
        if (curve == null)
        {
            return _singleton = AnimationCurve.Linear(0, start, 1 / 60f, end);
        }
        float tangent = (end - start) / TimeEnd;
        _buffer2[0] = new Keyframe(0, start, 0, tangent);
        _buffer2[1] = new Keyframe(TimeEnd, end, tangent, 0);
        curve.keys = _buffer2;
        return curve;
    }

    public static AnimationCurve LinearCurve(float start, float mid, float end)
    {
        var curve = _singleton;
        float tangent = (mid - start) / TimeEnd;
        float tangent2 = (end - mid) / TimeEnd;
        _buffer3[0] = new Keyframe(0, start, 0, tangent);
        _buffer3[1] = new Keyframe(TimeEnd, mid, tangent, tangent2);
        _buffer3[2] = new Keyframe(TimeEnd * 2, end, tangent2, 0);
        if (curve == null)
        {
            curve = _singleton = new AnimationCurve(_buffer3);
        }
        else
        {
            curve.keys = _buffer3;
        }
        return curve;
    }

    public static AnimationCurve LinearCurve(params float[] values) => LinearCurve(values.AsSpan());

    public static AnimationCurve LinearCurve(ReadOnlySpan<float> values)
    {
        var curve = _singleton;

        var keyframes = new Keyframe[values.Length];
        static float T(float x, float y) => (x - y) / TimeEnd;

        for (int i = 0; i < values.Length; i++)
        {
            keyframes[i] = new Keyframe(TimeEnd * i, values[i]);

            if (i > 0)
            {
                var t = T(keyframes[i - 1].value, keyframes[i].value);
                keyframes[i - 1].outTangent = t;
                keyframes[i].inTangent = t;
            }
        }

        if (curve == null)
        {
            curve = _singleton = new AnimationCurve(keyframes);
        }
        else
        {
            curve.keys = keyframes;
        }
        return curve;
    }

    public static AnimationCurve LinearCurveZeroDisabled(float initialValue, params float[] values)
    {
        var keys = new Keyframe[values.Length + 1];
        float prevTangent = Tangent(0, values[0]);
        keys[0] = new(0, initialValue, 0, prevTangent);
        for (int i = 0; i < values.Length; i++)
        {
            float tangent = (i + 1) < values.Length ? Tangent(values[i], values[i + 1]) : 0f;
            keys[i + 1] = new Keyframe(TimeEnd + TimeEnd * 10 * i, values[i], prevTangent, tangent);
            prevTangent = tangent;
        }
        return new(keys);
        static float Tangent(float x, float y)
        {
            return (y - x) / TimeEnd;
        }
    }
}