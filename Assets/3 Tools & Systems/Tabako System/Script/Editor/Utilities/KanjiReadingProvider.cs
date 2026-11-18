using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class KanjiReadingProvider
    {
        private const string k_prefDicDir = "SataniaTabako.NMeCabDicDir";
        private const string k_taggerTypeName = "NMeCab.Specialized.MeCabIpaDicTagger";
        private const string k_dicFileName = "sys.dic";

        private static object _tagger;
        private static string _taggerDicDir;
        private static MethodInfo _parse;
        private static PropertyInfo _surface;
        private static PropertyInfo _reading;
        private static string _autoDetected;
        private static bool _autoDetectDone;

        private static Type _taggerType;
        private static bool _taggerTypeSearched;

        public static Type TaggerType
        {
            get
            {
                if (_taggerTypeSearched)
                    return _taggerType;

                _taggerTypeSearched = true;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = null;
                    try { type = assembly.GetType(k_taggerTypeName); }
                    catch { }
                    if (type != null)
                    {
                        _taggerType = type;
                        break;
                    }
                }
                return _taggerType;
            }
        }

        public static bool HasLibrary => TaggerType != null;

        public static string DicDir
        {
            get
            {
                string saved = EditorPrefs.GetString(k_prefDicDir, "");
                if (IsDicDir(saved))
                    return saved;

                if (!_autoDetectDone)
                {
                    _autoDetectDone = true;
                    _autoDetected = AutoDetect();
                }
                return _autoDetected;
            }
            set
            {
                EditorPrefs.SetString(k_prefDicDir, value ?? "");
                _autoDetectDone = false;
            }
        }

        public static bool IsAvailable => HasLibrary && !string.IsNullOrEmpty(DicDir);

        public static bool IsDicDir(string dir)
        {
            return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) && File.Exists(Path.Combine(dir, k_dicFileName));
        }

        public static string AutoDetect()
        {
            try
            {
                var hit = Directory.EnumerateFiles(Application.dataPath, k_dicFileName, SearchOption.AllDirectories).FirstOrDefault();
                return hit != null ? Path.GetDirectoryName(hit).Replace('\\', '/') : null;
            }
            catch
            {
                return null;
            }
        }

        public static void ResetAutoDetect()
        {
            _autoDetectDone = false;
        }

        public static bool TryToKana(string text, out string kana, out string error)
        {
            kana = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "empty";
                return false;
            }

            try
            {
                if (!EnsureTagger(out error))
                    return false;

                var nodes = _parse.Invoke(_tagger, new object[] { text }) as IEnumerable;
                if (nodes == null)
                {
                    error = "parse returned null";
                    return false;
                }

                var sb = new StringBuilder();
                foreach (var node in nodes)
                {
                    if (node == null)
                        continue;

                    string surface = _surface.GetValue(node) as string ?? "";
                    if (surface.Length == 0)
                        continue;

                    string reading = _reading != null ? _reading.GetValue(node) as string : null;
                    sb.Append(!string.IsNullOrEmpty(reading) && reading != "*" ? reading : surface);
                }

                kana = KanaRomajiConverter.Normalize(sb.ToString());
                return kana.Length > 0;
            }
            catch (TargetInvocationException e)
            {
                error = (e.InnerException ?? e).Message;
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static bool EnsureTagger(out string error)
        {
            error = null;
            var type = TaggerType;
            if (type == null)
            {
                error = "LibNMeCab not found";
                return false;
            }

            string dicDir = DicDir;
            if (string.IsNullOrEmpty(dicDir))
            {
                error = "dictionary not found";
                return false;
            }

            if (_tagger != null && _taggerDicDir == dicDir)
                return true;

            var create = type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string[]) }, null);
            if (create == null)
            {
                error = "MeCabIpaDicTagger.Create not found";
                return false;
            }

            _tagger = create.Invoke(null, new object[] { dicDir, null });
            _taggerDicDir = dicDir;
            _parse = type.GetMethod("Parse", new[] { typeof(string) });
            if (_tagger == null || _parse == null)
            {
                error = "tagger could not be created";
                _tagger = null;
                return false;
            }

            Type nodeType = _parse.ReturnType.IsArray ? _parse.ReturnType.GetElementType() : null;
            if (nodeType == null)
            {
                error = "unexpected Parse return type " + _parse.ReturnType.Name;
                _tagger = null;
                return false;
            }

            _surface = nodeType.GetProperty("Surface");
            _reading = nodeType.GetProperty("Reading");
            if (_surface == null)
            {
                error = "Surface property not found";
                _tagger = null;
                return false;
            }
            return true;
        }
    }
}
