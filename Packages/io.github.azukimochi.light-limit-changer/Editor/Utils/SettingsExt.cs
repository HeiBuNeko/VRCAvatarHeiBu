using System.Linq;
using System.Reflection;
using System.Text;

namespace io.github.azukimochi;

internal static class SettingsExt
{
    public static string ToJson<TSettings>(this TSettings settings, bool includeChildSettings = false) where TSettings : ISettings
    {
        IndentedStringBuilder sb = new(new());
        sb.AppendLineIndented("{");
        void WriteObj(object obj, FieldInfo parent = null)
        {
            sb.Indent++;
            var type = obj.GetType();
            var fields = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => !x.IsLiteral)
                .Where(x => !(x.Name == nameof(Parameter.MinMaxRange) && parent?.GetCustomAttribute<MinMaxRangeAttribute>() == null))
                .Where(x => includeChildSettings || !typeof(ISettings).IsAssignableFrom(x.FieldType))
                .ToArray();
            if (typeof(ISettings).IsAssignableFrom(type))
            {
                sb.AppendIndented($"\"Type\": \"{type.Name}\"");
                if (fields.Length != 0)
                    sb.Append(",");
                sb.AppendLine();
            }
            for (int i = 0; i < fields.Length; i++)
            {
                var x = fields[i];
                sb.AppendIndented($"\"{x.Name}\": ");
                var value = x.GetValue(obj);
                if (value is string)
                {
                    sb.Append($"\"{value}\"");
                }
                else if (value is bool b)
                {
                    sb.Append(b ? "true" : "false");
                }
                else if (value is int or float)
                {
                    sb.Append($"{value}");
                }
                else if (value is null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.AppendLine("{");
                    WriteObj(value, x);
                }
                if (i != fields.Length - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.Indent--;
            sb.AppendIndented("}");
        }
        WriteObj(settings);
        return sb.StringBuilder.ToString();
    }
}

internal sealed class IndentedStringBuilder
{
    public StringBuilder StringBuilder;
    public int Indent;

    public IndentedStringBuilder(StringBuilder stringBuilder)
    {
        StringBuilder = stringBuilder;
        Indent = 0;
    }

    public void AppendIndented(string value)
    {
        StringBuilder.Append(' ', Indent * 2);
        StringBuilder.Append(value);
    }

    public void AppendLineIndented(string value)
    {
        StringBuilder.Append(' ', Indent * 2);
        StringBuilder.AppendLine(value);
    }

    public void Append(string value) => StringBuilder.Append(value);
    public void AppendLine() => StringBuilder.AppendLine();
    public void AppendLine(string value) => StringBuilder.AppendLine(value);
}