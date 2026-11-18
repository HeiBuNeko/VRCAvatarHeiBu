using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class KanaRomajiConverter
    {
        private static readonly Dictionary<string, string[]> k_table = new Dictionary<string, string[]>
        {
            { "あ", new[] { "a" } }, { "い", new[] { "i" } }, { "う", new[] { "u" } }, { "え", new[] { "e" } }, { "お", new[] { "o" } },
            { "か", new[] { "ka" } }, { "き", new[] { "ki" } }, { "く", new[] { "ku" } }, { "け", new[] { "ke" } }, { "こ", new[] { "ko" } },
            { "さ", new[] { "sa" } }, { "し", new[] { "shi", "si" } }, { "す", new[] { "su" } }, { "せ", new[] { "se" } }, { "そ", new[] { "so" } },
            { "た", new[] { "ta" } }, { "ち", new[] { "chi", "ti" } }, { "つ", new[] { "tsu", "tu" } }, { "て", new[] { "te" } }, { "と", new[] { "to" } },
            { "な", new[] { "na" } }, { "に", new[] { "ni" } }, { "ぬ", new[] { "nu" } }, { "ね", new[] { "ne" } }, { "の", new[] { "no" } },
            { "は", new[] { "ha" } }, { "ひ", new[] { "hi" } }, { "ふ", new[] { "fu", "hu" } }, { "へ", new[] { "he" } }, { "ほ", new[] { "ho" } },
            { "ま", new[] { "ma" } }, { "み", new[] { "mi" } }, { "む", new[] { "mu" } }, { "め", new[] { "me" } }, { "も", new[] { "mo" } },
            { "や", new[] { "ya" } }, { "ゆ", new[] { "yu" } }, { "よ", new[] { "yo" } },
            { "ら", new[] { "ra" } }, { "り", new[] { "ri" } }, { "る", new[] { "ru" } }, { "れ", new[] { "re" } }, { "ろ", new[] { "ro" } },
            { "わ", new[] { "wa" } }, { "ゐ", new[] { "wi" } }, { "ゑ", new[] { "we" } }, { "を", new[] { "wo", "o" } },
            { "が", new[] { "ga" } }, { "ぎ", new[] { "gi" } }, { "ぐ", new[] { "gu" } }, { "げ", new[] { "ge" } }, { "ご", new[] { "go" } },
            { "ざ", new[] { "za" } }, { "じ", new[] { "ji", "zi" } }, { "ず", new[] { "zu" } }, { "ぜ", new[] { "ze" } }, { "ぞ", new[] { "zo" } },
            { "だ", new[] { "da" } }, { "ぢ", new[] { "ji", "di" } }, { "づ", new[] { "zu", "du" } }, { "で", new[] { "de" } }, { "ど", new[] { "do" } },
            { "ば", new[] { "ba" } }, { "び", new[] { "bi" } }, { "ぶ", new[] { "bu" } }, { "べ", new[] { "be" } }, { "ぼ", new[] { "bo" } },
            { "ぱ", new[] { "pa" } }, { "ぴ", new[] { "pi" } }, { "ぷ", new[] { "pu" } }, { "ぺ", new[] { "pe" } }, { "ぽ", new[] { "po" } },
            { "ゔ", new[] { "vu" } },
            { "きゃ", new[] { "kya" } }, { "きゅ", new[] { "kyu" } }, { "きょ", new[] { "kyo" } },
            { "しゃ", new[] { "sha", "sya" } }, { "しゅ", new[] { "shu", "syu" } }, { "しょ", new[] { "sho", "syo" } }, { "しぇ", new[] { "she", "sye" } },
            { "ちゃ", new[] { "cha", "tya" } }, { "ちゅ", new[] { "chu", "tyu" } }, { "ちょ", new[] { "cho", "tyo" } }, { "ちぇ", new[] { "che", "tye" } },
            { "にゃ", new[] { "nya" } }, { "にゅ", new[] { "nyu" } }, { "にょ", new[] { "nyo" } },
            { "ひゃ", new[] { "hya" } }, { "ひゅ", new[] { "hyu" } }, { "ひょ", new[] { "hyo" } },
            { "みゃ", new[] { "mya" } }, { "みゅ", new[] { "myu" } }, { "みょ", new[] { "myo" } },
            { "りゃ", new[] { "rya" } }, { "りゅ", new[] { "ryu" } }, { "りょ", new[] { "ryo" } },
            { "ぎゃ", new[] { "gya" } }, { "ぎゅ", new[] { "gyu" } }, { "ぎょ", new[] { "gyo" } },
            { "じゃ", new[] { "ja", "zya", "jya" } }, { "じゅ", new[] { "ju", "zyu", "jyu" } }, { "じょ", new[] { "jo", "zyo", "jyo" } }, { "じぇ", new[] { "je", "zye" } },
            { "ぢゃ", new[] { "ja", "dya" } }, { "ぢゅ", new[] { "ju", "dyu" } }, { "ぢょ", new[] { "jo", "dyo" } },
            { "びゃ", new[] { "bya" } }, { "びゅ", new[] { "byu" } }, { "びょ", new[] { "byo" } },
            { "ぴゃ", new[] { "pya" } }, { "ぴゅ", new[] { "pyu" } }, { "ぴょ", new[] { "pyo" } },
            { "ふぁ", new[] { "fa" } }, { "ふぃ", new[] { "fi" } }, { "ふぇ", new[] { "fe" } }, { "ふぉ", new[] { "fo" } }, { "ふゅ", new[] { "fyu" } },
            { "てぃ", new[] { "ti", "thi" } }, { "でぃ", new[] { "di", "dhi" } }, { "てゅ", new[] { "tyu", "thu" } }, { "でゅ", new[] { "dyu", "dhu" } },
            { "とぅ", new[] { "tu", "twu" } }, { "どぅ", new[] { "du", "dwu" } },
            { "うぃ", new[] { "wi" } }, { "うぇ", new[] { "we" } }, { "うぉ", new[] { "wo", "who" } },
            { "ゔぁ", new[] { "va" } }, { "ゔぃ", new[] { "vi" } }, { "ゔぇ", new[] { "ve" } }, { "ゔぉ", new[] { "vo" } },
            { "くぁ", new[] { "kwa", "qa" } }, { "くぃ", new[] { "kwi", "qi" } }, { "くぇ", new[] { "kwe", "qe" } }, { "くぉ", new[] { "kwo", "qo" } },
            { "ぐぁ", new[] { "gwa" } },
            { "いぇ", new[] { "ye" } },
            { "ぁ", new[] { "a", "la", "xa" } }, { "ぃ", new[] { "i", "li", "xi" } }, { "ぅ", new[] { "u", "lu", "xu" } }, { "ぇ", new[] { "e", "le", "xe" } }, { "ぉ", new[] { "o", "lo", "xo" } },
            { "ゃ", new[] { "ya", "lya", "xya" } }, { "ゅ", new[] { "yu", "lyu", "xyu" } }, { "ょ", new[] { "yo", "lyo", "xyo" } }, { "ゎ", new[] { "wa", "lwa", "xwa" } },
        };

        private static readonly HashSet<string> k_wapuro = new HashSet<string> { "si", "ti", "tu", "hu", "zi", "di", "du", "sya", "syu", "syo", "sye", "tya", "tyu", "tyo", "tye", "zya", "zyu", "zyo", "zye", "jya", "jyu", "jyo", "dya", "dyu", "dyo", "nn", "thi", "dhi" };

        private const string k_smallFollowers = "ぁぃぅぇぉゃゅょゎ";

        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var sb = new StringBuilder(text.Length);
            foreach (char c in text.Trim())
            {
                if (c >= 0x30A1 && c <= 0x30F6)
                    sb.Append((char)(c - 0x60));
                else if (c == 0x30FC)
                    sb.Append('ー');
                else if (c >= 0xFF01 && c <= 0xFF5E)
                    sb.Append((char)(c - 0xFEE0));
                else if (c == 0x3000)
                    sb.Append(' ');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static bool IsKana(char c)
        {
            return (c >= 0x3041 && c <= 0x3096) || c == 'ー' || c == 0x30FC;
        }

        public static bool ContainsNonKana(string text)
        {
            return Normalize(text).Any(c => !IsKana(c) && !char.IsWhiteSpace(c) && !(c < 0x80));
        }

        public static List<string> ToRomajiVariants(string kana, int maxResults = 24)
        {
            var results = new List<string>();
            var morae = Segment(Normalize(kana));
            if (morae.Count == 0)
                return results;

            var alternatives = morae.Select(m => Alternatives(m)).ToList();
            for (int i = 0; i < alternatives.Count; i++)
                alternatives[i] = ApplyContext(alternatives, i, morae);

            AddUnique(results, Build(alternatives, alts => alts[0]));
            AddUnique(results, Build(alternatives, alts => alts.FirstOrDefault(k_wapuro.Contains) ?? alts[0]));

            Expand(alternatives, 0, new StringBuilder(), results, maxResults);
            return results;
        }

        private static List<string> Segment(string text)
        {
            var morae = new List<string>();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c))
                    continue;

                if (i + 1 < text.Length && k_smallFollowers.IndexOf(text[i + 1]) >= 0 && k_table.ContainsKey(text.Substring(i, 2)))
                {
                    morae.Add(text.Substring(i, 2));
                    i++;
                    continue;
                }

                morae.Add(c.ToString());
            }
            return morae;
        }

        private static List<string> Alternatives(string mora)
        {
            if (k_table.TryGetValue(mora, out var alts))
                return alts.ToList();

            if (mora == "っ")
                return new List<string> { "っ" };
            if (mora == "ん")
                return new List<string> { "ん" };
            if (mora == "ー")
                return new List<string> { "ー" };

            return new List<string> { mora.ToLowerInvariant() };
        }

        private static List<string> ApplyContext(List<List<string>> alternatives, int index, List<string> morae)
        {
            var current = alternatives[index];
            if (morae[index] == "っ")
            {
                var next = index + 1 < alternatives.Count ? alternatives[index + 1] : null;
                if (next == null || next.Count == 0 || morae[index + 1] == "ん" || morae[index + 1] == "ー")
                    return new List<string> { "xtu", "ltu" };

                var doubled = new List<string>();
                foreach (var n in next)
                {
                    if (n.Length == 0 || !char.IsLetter(n[0]) || "aiueo".IndexOf(n[0]) >= 0)
                        continue;
                    string head = n.StartsWith("ch") ? "t" : n[0].ToString();
                    AddUnique(doubled, head);
                    if (n.StartsWith("ch"))
                        AddUnique(doubled, "c");
                }
                if (doubled.Count == 0)
                    return new List<string> { "xtu", "ltu" };
                return doubled;
            }

            if (morae[index] == "ん")
            {
                bool beforeVowelOrY = index + 1 < alternatives.Count && alternatives[index + 1].Any(n => n.Length > 0 && "aiueoy".IndexOf(n[0]) >= 0);
                return beforeVowelOrY ? new List<string> { "nn", "n" } : new List<string> { "n", "nn" };
            }

            if (morae[index] == "う" && index > 0 && morae[index - 1] != "ー" && alternatives[index - 1].Any(p => p.EndsWith("o")))
                return new List<string> { "u", "" };

            if (morae[index] == "ー")
            {
                string vowel = null;
                for (int j = index - 1; j >= 0; j--)
                {
                    var prev = alternatives[j].FirstOrDefault(p => p.Length > 0 && "aiueo".IndexOf(p[p.Length - 1]) >= 0);
                    if (prev != null)
                    {
                        vowel = prev[prev.Length - 1].ToString();
                        break;
                    }
                    if (morae[j] != "ー")
                        break;
                }
                var options = new List<string>();
                if (vowel != null)
                    options.Add(vowel);
                options.Add("");
                options.Add("-");
                return options;
            }

            return current;
        }

        private static string Build(List<List<string>> alternatives, System.Func<List<string>, string> pick)
        {
            var sb = new StringBuilder();
            foreach (var alts in alternatives)
                sb.Append(pick(alts));
            return sb.ToString();
        }

        private static void Expand(List<List<string>> alternatives, int index, StringBuilder sb, List<string> results, int maxResults)
        {
            if (results.Count >= maxResults)
                return;

            if (index == alternatives.Count)
            {
                AddUnique(results, sb.ToString());
                return;
            }

            foreach (var alt in alternatives[index])
            {
                int length = sb.Length;
                sb.Append(alt);
                Expand(alternatives, index + 1, sb, results, maxResults);
                sb.Length = length;
                if (results.Count >= maxResults)
                    return;
            }
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (!string.IsNullOrEmpty(value) && !list.Contains(value))
                list.Add(value);
        }
    }
}
