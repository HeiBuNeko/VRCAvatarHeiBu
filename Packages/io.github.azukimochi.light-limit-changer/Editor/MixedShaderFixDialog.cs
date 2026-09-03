using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace io.github.azukimochi;

internal sealed class MixedShaderFixDialog : EditorWindow
{
    private enum ExclusionMode { Material, Object }

    private sealed class FixItem
    {
        public ObjectReference RendererRef;
        public Material[] UnsupportedMaterials;
        public ExclusionMode Mode = ExclusionMode.Object;
    }

    private List<FixItem> _items;
    private ErrorReport _report;
    private Vector2 _scroll;
    internal bool Applied;

    private GUIStyle _paddedStyle;
    private GUIStyle _itemBoxStyle;
    private GUIStyle _radioStyle;
    private GUIStyle _subLabelStyle;

    public static bool Show(ErrorReport report, ObjectReference rendererRef, Material[] materials)
    {
        // プレイモード／ビルド中はアバターがクローン・結合されている可能性があり、
        // 変更してもプレイモード終了時に破棄されてしまうため、編集モードでのみ実行を許可する。
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                L10n.TrStr("autofix:dialog/title", "LLC - 混在シェーダー 自動修正"),
                L10n.TrStr("autofix:dialog/error/playmode",
                    "プレイモード中は自動修正を実行できません。\n\nプレイモード中はアバターが再ビルドされているため、除外設定を変更してもプレイモードを終了すると破棄されてしまいます。\nプレイモードを終了してから、もう一度お試しください。"),
                L10n.TrStr("common:label/ok", "OK"));
            return false;
        }

        var window = CreateInstance<MixedShaderFixDialog>();
        window.titleContent = new GUIContent(L10n.TrStr("autofix:dialog/title", "LLC - 混在シェーダー 自動修正"));
        window._report = report;
        window._items = new List<FixItem>
        {
            new() { RendererRef = rendererRef, UnsupportedMaterials = materials }
        };

        int matCount = materials.Count(m => m != null);
        float height = 160 + 80 + matCount * 22;
        var size = new Vector2(500, Mathf.Min(height, 460));
        window.minSize = new Vector2(420, 220);
        window.maxSize = new Vector2(900, 700);

        var mainPos = EditorGUIUtility.GetMainWindowPosition();
        window.position = new Rect(
            mainPos.center.x - size.x / 2f,
            mainPos.center.y - size.y / 2f,
            size.x, size.y);
        window.ShowModal();
        return window.Applied;
    }

    private void EnsureStyles()
    {
        if (_paddedStyle != null) return;
        _paddedStyle = new GUIStyle { padding = new RectOffset(12, 12, 10, 10) };
        _itemBoxStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8) };
        _radioStyle = new GUIStyle(EditorStyles.radioButton) { margin = new RectOffset(0, 0, 2, 2) };
        _subLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            margin = new RectOffset(20, 0, 0, 4),
        };
    }

    private void OnGUI()
    {
        EnsureStyles();

        EditorGUILayout.BeginVertical(_paddedStyle);
        {
            EditorGUILayout.HelpBox(
                L10n.TrStr("autofix:dialog/description", "非対応シェーダーのマテリアルをどの単位でLLCの除外リストに追加しますか？"),
                MessageType.Info);

            EditorGUILayout.Space(8);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var item in _items)
            {
                DrawItem(item);
                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            DrawFooterButtons();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawItem(FixItem item)
    {
        EditorGUILayout.BeginVertical(_itemBoxStyle);
        {
            var displayName = item.RendererRef.Path?.Split('/').LastOrDefault()
                ?? item.RendererRef.ToString();
            EditorGUILayout.LabelField(
                L10n.TrStr("autofix:dialog/renderer", "オブジェクト") + ": " + displayName,
                EditorStyles.boldLabel);

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(L10n.TrStr("autofix:dialog/unsupported-materials", "非対応マテリアル") + ":");
            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(true))
            {
                foreach (var mat in item.UnsupportedMaterials.Where(m => m != null))
                {
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);
                }
            }

            EditorGUILayout.Space(8);

            DrawRadioOption(item, ExclusionMode.Object,
                L10n.TrStr("autofix:dialog/mode/object/label", "オブジェクト単位で除外"),
                L10n.TrStr("autofix:dialog/mode/object/hint", "このオブジェクト全体を除外リストに追加します（同一オブジェクト上の他のマテリアルも除外されます）"));

            DrawRadioOption(item, ExclusionMode.Material,
                L10n.TrStr("autofix:dialog/mode/material/label", "マテリアル単位で除外"),
                L10n.TrStr("autofix:dialog/mode/material/hint", "非対応マテリアルのみを除外リストに追加します"));

        }
        EditorGUILayout.EndVertical();
    }

    private void DrawRadioOption(FixItem item, ExclusionMode mode, string label, string hint)
    {
        if (GUILayout.Toggle(item.Mode == mode, label, _radioStyle))
            item.Mode = mode;
        GUILayout.Label(hint, _subLabelStyle);
    }

    private void DrawFooterButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(L10n.TrStr("autofix:dialog/cancel", "キャンセル"), GUILayout.Width(100), GUILayout.Height(24)))
            Close();
        GUILayout.Space(4);
        if (GUILayout.Button(L10n.TrStr("autofix:dialog/apply", "適用"), GUILayout.Width(100), GUILayout.Height(24)))
            ApplyFix();
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyFix()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Light Limit Changer",
                L10n.TrStr("autofix:dialog/error/playmode",
                    "プレイモード中は自動修正を実行できません。\n\nプレイモード中はアバターが再ビルドされているため、除外設定を変更してもプレイモードを終了すると破棄されてしまいます。\nプレイモードを終了してから、もう一度お試しください。"),
                L10n.TrStr("common:label/ok", "OK"));
            Close();
            return;
        }

        if (!TryResolveAvatarAndLLC(out var avatarRoot, out var llc, out var debugLog))
        {
            Debug.LogWarning(debugLog);
            EditorUtility.DisplayDialog(
                "Light Limit Changer",
                L10n.TrStr("autofix:dialog/error/avatar-not-found",
                    "アバターが見つかりませんでした。シーンに対象のアバターが存在するか確認してください。"),
                "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(llc, "LLC: Fix mixed shader warning");
        bool changed = false;

        foreach (var item in _items)
        {
            if (item.Mode == ExclusionMode.Object)
            {
                var rendererPath = item.RendererRef.Path;
                if (rendererPath == null)
                {
                    EditorUtility.DisplayDialog(
                        "Light Limit Changer",
                        L10n.TrStr("autofix:dialog/error/path-not-found",
                            "オブジェクトのパスが取得できませんでした。マテリアル単位での除外をお試しください。"),
                        "OK");
                    continue;
                }

                var t = avatarRoot.transform.Find(rendererPath);
                if (t == null)
                {
                    EditorUtility.DisplayDialog(
                        "Light Limit Changer",
                        string.Format(
                            L10n.TrStr("autofix:dialog/error/object-not-found",
                                "オブジェクト '{0}' が見つかりませんでした。\n他のプラグインによってオブジェクトが統合されている可能性があります。\nマテリアル単位での除外をお試しください。"),
                            rendererPath),
                        "OK");
                    continue;
                }

                var go = t.gameObject;
                if (!llc.Excludes.Any(e => e.Object == go))
                {
                    llc.Excludes.Add(new ExcludeOptions(go));
                    changed = true;
                }
            }
            else
            {
                foreach (var mat in item.UnsupportedMaterials)
                {
                    if (mat == null || !EditorUtility.IsPersistent(mat))
                        continue;
                    if (!llc.Excludes.Any(e => e.Object == mat))
                    {
                        llc.Excludes.Add(new ExcludeOptions(mat));
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(llc);
            if (llc.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(llc.gameObject.scene);
        }

        Applied = true;
        Close();
    }

    private bool TryResolveAvatarAndLLC(out GameObject avatarRoot, out LightLimitChangerComponent llc, out string debugLog)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[LLC AutoFix] Avatar resolution debug:");
        sb.AppendLine($"  ErrorReport.AvatarName: '{_report.AvatarName}'");
        sb.AppendLine($"  ErrorReport.AvatarRootPath: '{_report.AvatarRootPath}'");
        sb.AppendLine($"  ActiveScene: '{SceneManager.GetActiveScene().name}'");
        sb.AppendLine($"  Loaded scene count: {SceneManager.sceneCount}");

        avatarRoot = null;
        llc = null;

        // Strategy 1: NDMF's TryResolveAvatar (active scene, root objects only)
        if (_report.TryResolveAvatar(out var resolved) && resolved != null)
        {
            sb.AppendLine($"  [Strategy 1] TryResolveAvatar succeeded: '{resolved.name}' in scene '{resolved.scene.name}'");
            var foundLLC = resolved.GetComponentInChildren<LightLimitChangerComponent>(true);
            if (foundLLC != null)
            {
                avatarRoot = resolved;
                llc = foundLLC;
                sb.AppendLine($"  -> LLC component found on '{foundLLC.gameObject.name}'");
                debugLog = sb.ToString();
                return true;
            }
            sb.AppendLine($"  -> LLC component NOT found under resolved avatar");
        }
        else
        {
            sb.AppendLine($"  [Strategy 1] TryResolveAvatar FAILED");
        }

        // Strategy 2: Search all loaded scenes by avatar name
        var path = _report.AvatarRootPath;
        if (!string.IsNullOrEmpty(path))
        {
            var firstName = path.Split('/')[0];
            var remaining = firstName == path ? null : path.Substring(firstName.Length + 1);
            sb.AppendLine($"  [Strategy 2] searching all loaded scenes for root '{firstName}' (remaining: '{remaining}')...");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (IsPreviewScene(scene))
                {
                    sb.AppendLine($"    Scene '{scene.name}': skipped (preview scene)");
                    continue;
                }
                sb.AppendLine($"    Scene '{scene.name}': scanning {scene.rootCount} root objects");

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    if (rootGo.name != firstName) continue;
                    GameObject candidate = remaining == null ? rootGo : rootGo.transform.Find(remaining)?.gameObject;
                    if (candidate == null) continue;

                    var foundLLC = candidate.GetComponentInChildren<LightLimitChangerComponent>(true);
                    if (foundLLC != null)
                    {
                        avatarRoot = candidate;
                        llc = foundLLC;
                        sb.AppendLine($"  -> Matched in '{scene.name}': '{candidate.name}' (LLC on '{foundLLC.gameObject.name}')");
                        debugLog = sb.ToString();
                        return true;
                    }
                    sb.AppendLine($"    Found '{candidate.name}' but no LLC component");
                }
            }
            sb.AppendLine($"  [Strategy 2] FAILED");
        }
        else
        {
            sb.AppendLine($"  [Strategy 2] skipped (AvatarRootPath is empty)");
        }

        // Strategy 3: Find any LLC component in loaded (non-preview) scenes
        sb.AppendLine($"  [Strategy 3] searching for any LightLimitChangerComponent in loaded scenes...");
        var candidates = new List<LightLimitChangerComponent>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            if (IsPreviewScene(scene)) continue;

            foreach (var rootGo in scene.GetRootGameObjects())
            {
                foreach (var found in rootGo.GetComponentsInChildren<LightLimitChangerComponent>(true))
                {
                    candidates.Add(found);
                    sb.AppendLine($"    Found LLC: '{found.gameObject.name}' in scene '{scene.name}'");
                }
            }
        }

        if (candidates.Count == 1)
        {
            llc = candidates[0];
            avatarRoot = llc.transform.root.gameObject;
            sb.AppendLine($"  [Strategy 3] succeeded (single candidate): avatar='{avatarRoot.name}', LLC='{llc.gameObject.name}'");
            debugLog = sb.ToString();
            return true;
        }
        if (candidates.Count > 1)
        {
            sb.AppendLine($"  [Strategy 3] FAILED: multiple LLC components found ({candidates.Count}). Cannot disambiguate.");
        }
        else
        {
            sb.AppendLine($"  [Strategy 3] FAILED: no LLC component in any loaded scene");
        }

        debugLog = sb.ToString();
        return false;
    }

    private static bool IsPreviewScene(Scene scene)
    {
        return scene.name == "___NDMF Preview___" || scene.name.StartsWith("___NDMF");
    }
}
