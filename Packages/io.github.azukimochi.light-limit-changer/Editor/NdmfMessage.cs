using System;
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine.UIElements;

namespace io.github.azukimochi;

internal class NdmfMessage : SimpleError
{
    public NdmfMessage(ErrorSeverity severity, string titleKey, string detailsKey = null, string hintKey = null)
    {
        TitleKey = titleKey;
        Severity = severity;
        DetailsKey = detailsKey;
        HintKey = hintKey;
    }

    public override Localizer Localizer => L10n.Localizer;

    public override ErrorSeverity Severity { get; }

    public override string TitleKey { get; }
    public override string[] TitleSubst => TitleSubstitutions;
    public string[] TitleSubstitutions { get => titleSubst; set => titleSubst = value; }
    private string[] titleSubst;

    public override string DetailsKey { get; }

    public override string[] DetailsSubst => detailsSubst;
    public string[] DetailsSubstitutions { get => detailsSubst; set => detailsSubst = value; }
    private string[] detailsSubst;

    public override string HintKey { get; }

    public override string[] HintSubst => HintSubstitutions;
    public string[] HintSubstitutions { get => hintSubst; set => hintSubst = value; }
    private string[] hintSubst;

    public Func<ErrorReport, bool> AutoFix { get; set; }

    private bool _isFixed;

    public override VisualElement CreateVisualElement(ErrorReport report)
    {
        var inner = base.CreateVisualElement(report);
        if (AutoFix == null) return inner;

        // SimpleErrorUI 自身を handle として言語変更コールバックを登録しているため、
        // 同じ要素を handle に使うと SimpleErrorUI のコールバックを上書きしてしまう。
        // そのため別のラッパー要素を生成し、それを handle / ライフタイムの基準とする。
        var wrapper = new VisualElement();
        wrapper.style.flexGrow = 1;
        wrapper.Add(inner);

        ApplyAutofixButton(inner, report);

        // エディター言語を変更すると SimpleErrorUI が RenderContent() で UXML を作り直し、
        // 自動修正ボタンが既定の非表示状態にリセットされてしまう。
        // 言語変更のたびにボタンを再適用する。言語変更コールバックは HashSet 管理で
        // 実行順が不定（RenderContent より先に走る可能性がある）ため、スケジューラで
        // 1フレーム遅延させ、UXML の再構築が完了した後に再適用する。
        LanguagePrefs.RegisterLanguageChangeCallback(wrapper,
            w => w.schedule.Execute(() => ApplyAutofixButton(inner, report)));

        return wrapper;
    }

    private void ApplyAutofixButton(VisualElement inner, ErrorReport report)
    {
        var buttons = inner.Q<VisualElement>("error-list-buttons");
        if (buttons == null) return;

        buttons.style.display = DisplayStyle.Flex;

        var autofixButton = inner.Q<Button>("autofix");
        var fixedButton = inner.Q<Button>("fixed");

        fixedButton?.SetEnabled(false);

        // 既に修正済みなら「修正済み」表示を維持する。
        if (_isFixed)
        {
            if (autofixButton != null) autofixButton.style.display = DisplayStyle.None;
            if (fixedButton != null) fixedButton.style.display = DisplayStyle.Flex;
            return;
        }

        if (autofixButton == null) return;

        var capturedReport = report;
        var capturedFix = AutoFix;
        autofixButton.clickable.clicked += () =>
        {
            if (EditorApplication.isPlaying)
            {
                if (EditorUtility.DisplayDialog(
                    L10n.TrStr("autofix:playmode-dialog/title", "自動修正"),
                    L10n.TrStr("autofix:playmode-dialog/message",
                        "プレイモード中は自動修正を実行できません。\nプレイモードを終了してから実行してください。"),
                    L10n.TrStr("autofix:playmode-dialog/exit", "プレイモードを終了"),
                    L10n.TrStr("autofix:playmode-dialog/cancel", "キャンセル")))
                {
                    EditorApplication.isPlaying = false;
                }
                return;
            }

            bool applied = capturedFix(capturedReport);
            if (!applied) return;

            _isFixed = true;
            autofixButton.style.display = DisplayStyle.None;
            if (fixedButton != null) fixedButton.style.display = DisplayStyle.Flex;
        };
        autofixButton.style.display = DisplayStyle.Flex;
        if (fixedButton != null) fixedButton.style.display = DisplayStyle.None;
    }

    public static NdmfMessage Create(ErrorSeverity severity, string key)
    {
        return new(severity, $"{key}/title", $"{key}/details", $"{key}/hint");
    }

    public static NdmfMessage CreateSimply(ErrorSeverity severity, string key) => new(severity, key);
}

internal class DebugMessage : IError
{
    public ErrorSeverity Severity => ErrorSeverity.Information;

    public string Message { get; set; }

    public void AddReference(ObjectReference obj)
    {

    }

    public VisualElement CreateVisualElement(ErrorReport report)
    {
        var tree = AssetUtils.FromGUID<VisualTreeAsset>("43d0b8ff1eaaff84aa53b73499a9fbef");
        var e = tree.CloneTree();
        e.Q<Button>("button").clicked += () =>
        {
            EditorGUIUtility.systemCopyBuffer = Message;
        };
        return e;
    }

    public string ToMessage()
    {
        return Message;
    }
}