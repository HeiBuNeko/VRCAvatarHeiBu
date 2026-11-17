#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VRC.SDK3.Avatars.Components;

/// <summary>
/// Control panel warnings for Face Tracking parameter behavior custom expressions.
/// This is a deliberately simple (and different), approximation of the client's validation.
/// (it may slightly differ from what works or doesn't in client, and in general is intended only as a basic helper).
/// </summary>
public static class FaceTrackingExpressionValidator
{
    // Constraints matching the documented expression support (see the Avatar Descriptor inspector);
    // expressions past these are rejected by the client
    internal const int MaxExpressionLength = 1024;
    const int MaxTokenCount = 256;
    const int MaxInstructionCount = 256;
    const int MaxNestingDepth = 32;
    const int MaxStackDepth = 32;
    const int MaxNumericLiteralLength = 20;
    const float MaxNumericMagnitude = 10000f;
    const int MaxTotalInstructionsPerAvatar = 2048;

    // Supported functions and their argument counts (documented in the inspector's expression help)
    static readonly Dictionary<string, int> FunctionArgCounts = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        { "abs", 1 },
        { "min", 2 },
        { "max", 2 },
        { "clamp", 3 },
    };

    // Built-in expression input: the avatar owner's voice level (0-1); not a tracking key, so it is
    // always available regardless of the 'Use All Tracking Keys' opt-in. Public as the canonical
    // SDK-side name for the input (the editor's key picker shows it too)
    public const string VoiceInputName = "Voice";

    public static List<string> CollectWarnings(VRCAvatarDescriptor descriptor)
        => CollectWarnings(descriptor, null);

    internal static List<string> CollectWarnings(VRCAvatarDescriptor descriptor, Action<string, int> onWarning)
    {
        var warnings = new List<string>();
        void AddWarning(string message, int behaviorIndex = -1)
        {
            warnings.Add(message);
            onWarning?.Invoke(message, behaviorIndex);
        }
        if (descriptor == null || !descriptor.enableFaceTracking || descriptor.faceTrackingParameterBehaviors == null)
            return warnings;

        var keyLookup = new KeyNameLookup(descriptor.useAllTrackingKeys);
        int totalInstructionEstimate = 0;

        for (int b = 0; b < descriptor.faceTrackingParameterBehaviors.Count; b++)
        {
            var behavior = descriptor.faceTrackingParameterBehaviors[b];
            if (behavior == null || behavior.combineMode != VRCAvatarDescriptor.ParameterBehaviorCombineMode.Custom)
                continue;

            if (string.IsNullOrWhiteSpace(behavior.customExpression))
            {
                AddWarning($"Face Tracking parameter behavior #{b + 1} uses Custom combine mode but its expression is empty - the behavior will have no effect.", b);
                continue;
            }

            var problems = ValidateExpression(behavior.customExpression, keyLookup, out int instructionEstimate);
            totalInstructionEstimate += instructionEstimate;
            if (problems.Count == 0)
                continue;

            string trimmed = behavior.customExpression.Length <= 64
                ? behavior.customExpression
                : behavior.customExpression.Substring(0, 61) + "...";
            var message = new StringBuilder($"Face Tracking parameter behavior #{b + 1} has a custom expression that will be rejected (\"{trimmed}\"):");
            foreach (string problem in problems)
                message.Append("\n- ").Append(problem);
            AddWarning(message.ToString(), b);
        }

        // The per-avatar instruction budget is applied after simplification, so this rough sum can
        // over-report; overly sensitive is acceptable here, missed problems are not
        if (totalInstructionEstimate > MaxTotalInstructionsPerAvatar)
            AddWarning($"The custom expressions across this avatar's parameter behaviors total roughly {totalInstructionEstimate} instructions, which may exceed the per-avatar budget of {MaxTotalInstructionsPerAvatar} - some behaviors may be skipped. This estimate is conservative and can over-report.");

        return warnings;
    }

    // Tracking key name resolution with specific reasons for names that don't resolve
    class KeyNameLookup
    {
        readonly HashSet<string> _availableKeys = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _allKeys = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> _keysByLowerName = new Dictionary<string, string>(StringComparer.Ordinal);

        public KeyNameLookup(bool useAllTrackingKeys)
        {
            for (int i = 0; i < (int)VRCAvatarDescriptor.FaceTrackingKey.Max; i++)
            {
                string name = ((VRCAvatarDescriptor.FaceTrackingKey)i).ToString();
                _allKeys.Add(name);
                _keysByLowerName[name.ToLowerInvariant()] = name;
                if (useAllTrackingKeys || VRCAvatarDescriptor.IsBasicFaceTrackingKey(i))
                    _availableKeys.Add(name);
            }

            // The built-in Voice input is always available; register it in the lookup so it validates
            // like a key name (including the case-sensitivity suggestion for 'voice')
            _allKeys.Add(VoiceInputName);
            _keysByLowerName[VoiceInputName.ToLowerInvariant()] = VoiceInputName;
            _availableKeys.Add(VoiceInputName);
            for (int i = 0; i < (int)VRCAvatarDescriptor.Viseme.Count; i++)
            {
                string name = VRCAvatarDescriptor.GetFaceTrackingVisemeInputName((VRCAvatarDescriptor.Viseme)i);
                _allKeys.Add(name);
                _keysByLowerName[name.ToLowerInvariant()] = name;
                _availableKeys.Add(name);
            }
        }

        // Null when the name resolves; otherwise the specific reason it doesn't
        public string GetUnknownReason(string name)
        {
            if (_availableKeys.Contains(name))
                return null;
            if (_allKeys.Contains(name))
                return $"'{name}' requires 'Use All Tracking Keys' (at the top of the Face Tracking section) to be enabled";
            if (_keysByLowerName.TryGetValue(name.ToLowerInvariant(), out string actual))
            {
                string suggestion = $"unknown tracking key '{name}' (names are case-sensitive - did you mean '{actual}'?";
                if (!_availableKeys.Contains(actual))
                    suggestion += ", which also requires 'Use All Tracking Keys'";
                return suggestion + ")";
            }
            return $"unknown tracking key '{name}'";
        }
    }

    static List<string> ValidateExpression(string expression, KeyNameLookup keyLookup, out int instructionEstimate)
    {
        var problems = new List<string>();
        instructionEstimate = 0;

        if (expression.Length > MaxExpressionLength)
            problems.Add($"the expression is {expression.Length} characters long (max {MaxExpressionLength})");

        // Single left-to-right pass tracking whether an operand or an operator is expected next,
        // the open parenthesis frames (function calls or grouping), the arithmetic still waiting
        // for its right-hand side (for a rough evaluation stack estimate), and conservative
        // counts for the complexity limits
        bool expectOperand = true;
        int tokens = 0;
        int stackNow = 0, stackMax = 0;
        var pendingOps = new List<(int precedence, bool unary)>();
        var frames = new List<(string function, int arity, int commas, int pendingBase)>();
        string pendingCall = null;
        int pendingCallPosition = 0;

        void PushOperand()
        {
            stackNow++;
            if (stackNow > stackMax)
                stackMax = stackNow;
        }

        // Operators waiting for their right-hand side complete once that side ends: at another
        // operator, a comma, or a closing parenthesis. Completion never crosses out of the
        // current parenthesis frame, and tighter (or equally tight, left-associative) operators
        // complete first
        void CompletePending(int minPrecedence)
        {
            int baseline = frames.Count > 0 ? frames[frames.Count - 1].pendingBase : 0;
            for (int j = pendingOps.Count - 1; j >= baseline && pendingOps[j].precedence >= minPrecedence; j--)
            {
                if (!pendingOps[j].unary)
                    stackNow--;
                pendingOps.RemoveAt(j);
            }
        }

        int i = 0;
        while (i < expression.Length)
        {
            char c = expression[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (++tokens > MaxTokenCount)
            {
                problems.Add($"the expression has more than {MaxTokenCount} tokens");
                return problems;
            }

            // Numbers: digits with at most one decimal point (".5" is fine)
            if (char.IsDigit(c) || (c == '.' && i + 1 < expression.Length && char.IsDigit(expression[i + 1])))
            {
                if (!expectOperand)
                {
                    problems.Add($"missing operator before the number at position {i + 1}");
                    return problems;
                }
                int start = i;
                bool decimalPoint = false;
                while (i < expression.Length)
                {
                    char n = expression[i];
                    if (char.IsDigit(n))
                        i++;
                    else if (n == '.' && !decimalPoint)
                    {
                        decimalPoint = true;
                        i++;
                    }
                    else
                        break;
                }
                string number = expression.Substring(start, i - start);
                if (number.Length > MaxNumericLiteralLength)
                    problems.Add($"the number '{number}' at position {start + 1} is too long");
                else if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                         || float.IsNaN(value) || float.IsInfinity(value) || Math.Abs(value) > MaxNumericMagnitude)
                    problems.Add($"the number '{number}' at position {start + 1} is outside the supported range");
                instructionEstimate++;
                PushOperand();
                expectOperand = false;
                continue;
            }

            // Identifiers: tracking keys, or function names when a '(' follows (even after whitespace)
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                    i++;
                string identifier = expression.Substring(start, i - start);

                if (!expectOperand)
                {
                    problems.Add($"missing operator before '{identifier}' at position {start + 1}");
                    return problems;
                }

                int peek = i;
                while (peek < expression.Length && char.IsWhiteSpace(expression[peek]))
                    peek++;
                if (peek < expression.Length && expression[peek] == '(')
                {
                    // the '(' itself is handled on the next loop pass
                    pendingCall = identifier;
                    pendingCallPosition = start + 1;
                }
                else
                {
                    string reason = keyLookup.GetUnknownReason(identifier);
                    if (reason != null)
                        problems.Add($"{reason} at position {start + 1}");
                    instructionEstimate++;
                    PushOperand();
                    expectOperand = false;
                }
                continue;
            }

            switch (c)
            {
                case '(':
                    if (!expectOperand)
                    {
                        problems.Add($"missing operator before '(' at position {i + 1}");
                        return problems;
                    }
                    if (frames.Count >= MaxNestingDepth)
                        problems.Add($"the expression nests more than {MaxNestingDepth} levels deep");
                    if (pendingCall != null)
                    {
                        if (!FunctionArgCounts.TryGetValue(pendingCall, out int arity))
                        {
                            problems.Add($"unknown function '{pendingCall}' at position {pendingCallPosition} (supported: abs, min, max, clamp)");
                            arity = -1; // keep walking its arguments without an arity check
                        }
                        frames.Add((pendingCall, arity, 0, pendingOps.Count));
                        instructionEstimate++;
                        pendingCall = null;
                    }
                    else
                    {
                        frames.Add((null, 0, 0, pendingOps.Count));
                    }
                    expectOperand = true;
                    i++;
                    continue;

                case ')':
                    if (expectOperand)
                    {
                        problems.Add($"missing operand or argument before ')' at position {i + 1}");
                        return problems;
                    }
                    if (frames.Count == 0)
                    {
                        problems.Add($"')' at position {i + 1} has no matching '('");
                        return problems;
                    }
                    CompletePending(0);
                    var frame = frames[frames.Count - 1];
                    frames.RemoveAt(frames.Count - 1);
                    if (frame.function != null)
                    {
                        int args = frame.commas + 1;
                        if (frame.arity >= 0 && args != frame.arity)
                            problems.Add($"{frame.function}() takes {frame.arity} argument{(frame.arity == 1 ? "" : "s")} but got {args}");
                        if (frame.function == "min" || frame.function == "max")
                            stackNow--;
                        else if (frame.function == "clamp")
                            stackNow -= 2;
                        // abs() and unknown functions keep the stack depth (approximation)
                    }
                    i++;
                    continue;

                case ',':
                    if (expectOperand)
                    {
                        problems.Add($"missing operand or argument before ',' at position {i + 1}");
                        return problems;
                    }
                    if (frames.Count == 0 || frames[frames.Count - 1].function == null)
                    {
                        problems.Add($"the comma at position {i + 1} is only valid between function arguments");
                        return problems;
                    }
                    CompletePending(0);
                    var top = frames[frames.Count - 1];
                    frames[frames.Count - 1] = (top.function, top.arity, top.commas + 1, top.pendingBase);
                    expectOperand = true;
                    i++;
                    continue;
            }

            if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                if (expectOperand)
                {
                    if (c != '-')
                    {
                        problems.Add($"unexpected '{c}' at position {i + 1} - an operand is expected there");
                        return problems;
                    }
                    // unary minus binds tighter than any binary operator
                    pendingOps.Add((100, true));
                    instructionEstimate++;
                }
                else
                {
                    int precedence = (c == '*' || c == '/') ? 2 : 1;
                    CompletePending(precedence);
                    pendingOps.Add((precedence, false));
                    instructionEstimate++;
                    expectOperand = true;
                }
                i++;
                continue;
            }

            problems.Add($"unexpected character '{c}' at position {i + 1}");
            return problems;
        }

        if (expectOperand)
            problems.Add("the expression ends without a final operand");
        if (frames.Count > 0)
            problems.Add($"the expression ends with {frames.Count} unclosed '('");
        if (instructionEstimate > MaxInstructionCount)
            problems.Add($"the expression is too complex (about {instructionEstimate} instructions, max {MaxInstructionCount})");
        if (stackMax > MaxStackDepth)
            problems.Add($"the expression needs an evaluation stack depth of about {stackMax} (max {MaxStackDepth})");

        return problems;
    }
}
#endif
