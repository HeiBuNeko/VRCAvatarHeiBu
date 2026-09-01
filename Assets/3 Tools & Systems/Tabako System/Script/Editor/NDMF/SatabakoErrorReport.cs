using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.localization;
using UnityEngine.UIElements;

namespace net.satania_shopping.tabakosystem.ndmf
{
    internal static class SatabakoErrorReport
    {
        public const string PassExceptionKey = "net.satania_shopping.tabakosystem:pass-exception";
        public const string PassWarningKey = "net.satania_shopping.tabakosystem:pass-warning";
        public const string PassInfoKey = "net.satania_shopping.tabakosystem:pass-info";
        public const string IFollowMissingTargetKey = "net.satania_shopping.tabakosystem:ifollow-missing-target";

        public static readonly Localizer L = new Localizer("ja-JP", () =>
        {
            Dictionary<string, string> ja = new Dictionary<string, string>
            {
                { PassExceptionKey, "Satabako: パスの処理中に例外が発生しました（ビルドは継続されます）" },
                { PassExceptionKey + ":description", "{0}" },
                { PassExceptionKey + ":hint", "アバターの設定を確認してください。問題が解決しない場合は作者にご連絡ください。" },

                { PassWarningKey, "Satabako: 設定が不足しています（ビルドは継続されます）" },
                { PassWarningKey + ":description", "{0}" },
                { PassWarningKey + ":hint", "該当のコンポーネントを確認し、不足しているフィールドを設定してください。" },

                { PassInfoKey, "Satabako: 情報（ビルドは継続されます）" },
                { PassInfoKey + ":description", "{0}" },
                { PassInfoKey + ":hint", "意図した設定であればそのままで問題ありません。" },

                { IFollowMissingTargetKey, "Satabako: 追従先が未設定です（ビルドは継続されます）" },
                { IFollowMissingTargetKey + ":description", "{0}" },
                { IFollowMissingTargetKey + ":hint", "Tabako System の unitypackage を再インポートし、アバターにプレハブを再導入してください。" },
            };

            return new List<(string, Func<string, string>)>
            {
                ("ja-JP", key => ja.TryGetValue(key, out var value) ? value : null),
            };
        });

        /// <summary>
        /// 例外を NonFatal の警告として NDMF コンソールに報告する（ビルドはブロックしない）。
        /// contexts に発生元コンポーネント等を渡すと、警告にクリック可能な参照として表示される。
        /// </summary>
        public static void ReportAsWarning(Exception exception, params UnityEngine.Object[] contexts)
        {
            ErrorReport.ReportError(new SatabakoPassExceptionWarning(exception, contexts));
        }

        /// <summary>
        /// 任意のメッセージを NonFatal の警告として NDMF コンソールに報告する（ビルドはブロックしない）。
        /// contexts に発生元コンポーネント等を渡すと、警告にクリック可能な参照として表示される。
        /// </summary>
        public static void ReportWarning(string message, params UnityEngine.Object[] contexts)
        {
            ErrorReport.ReportError(new SatabakoPassMessage(ErrorSeverity.NonFatal, PassWarningKey, message, contexts));
        }

        /// <summary>
        /// 任意のメッセージを Information として NDMF コンソールに報告する（ビルドはブロックしない）。
        /// 「通常と違うが正常」な通知向け。contexts を渡すとクリック可能な参照として表示される。
        /// </summary>
        public static void ReportInformation(string message, params UnityEngine.Object[] contexts)
        {
            ErrorReport.ReportError(new SatabakoPassMessage(ErrorSeverity.Information, PassInfoKey, message, contexts));
        }

        /// <summary>
        /// IFollowSatabakoObject の追従先（CT_◯◯）が未設定のときの NonFatal 警告（ビルドはブロックしない）。
        /// 解決策（unitypackage 再インポート / プレハブ再導入）は hint に表示される。
        /// </summary>
        public static void ReportIFollowMissingTarget(string message, params UnityEngine.Object[] contexts)
        {
            ErrorReport.ReportError(new SatabakoPassMessage(ErrorSeverity.NonFatal, IFollowMissingTargetKey, message, contexts));
        }
    }

    /// <summary>
    /// 捕捉済み例外を NonFatal（ビルドをブロックしない警告）として報告する SimpleError。
    /// NDMF本体の StackTraceError と同様、コピー可能なスタックトレース欄を表示する。
    /// </summary>
    internal sealed class SatabakoPassExceptionWarning : SimpleError
    {
        private readonly string _fullText;
        private readonly string[] _subst;

        public SatabakoPassExceptionWarning(Exception exception, params UnityEngine.Object[] contexts)
        {
            _fullText = exception?.ToString() ?? "<null>";
            string summary = exception == null
                ? "<null>"
                : $"{exception.GetType().Name}: {exception.Message}";
            _subst = new[] { summary };

            //発生元コンポーネント等をクリック可能な参照として追加
            //(GetReference/AddReference ともに null 安全)
            if (contexts != null)
            {
                foreach (UnityEngine.Object context in contexts)
                {
                    AddReference(ObjectRegistry.GetReference(context));
                }
            }
        }

        public override Localizer Localizer => SatabakoErrorReport.L;
        public override string TitleKey => SatabakoErrorReport.PassExceptionKey;
        public override ErrorSeverity Severity => ErrorSeverity.NonFatal;

        // 詳細(description)の {0} に「例外型: メッセージ」の1行サマリを差し込む
        public override string[] DetailsSubst => _subst;

        public override VisualElement CreateVisualElement(ErrorReport report)
        {
            VisualElement baseUI = base.CreateVisualElement(report);

            Foldout foldout = new Foldout
            {
                text = "スタックトレース",
                value = false,
            };

            TextField traceField = new TextField
            {
                multiline = true,
                isReadOnly = true,
                value = _fullText,
            };
            traceField.style.whiteSpace = WhiteSpace.Normal;

            //WarningでもStackTraceを出す
            foldout.Add(traceField);

            VisualElement root = new VisualElement();
            root.Add(baseUI);
            root.Add(foldout);
            return root;
        }
    }

    /// <summary>
    /// 任意メッセージを指定した重要度（NonFatal / Information 等）で報告する SimpleError。
    /// 発生元コンポーネントをクリック可能な参照として表示する（スタックトレースは無し）。
    /// </summary>
    internal sealed class SatabakoPassMessage : SimpleError
    {
        private readonly ErrorSeverity _severity;
        private readonly string _titleKey;
        private readonly string[] _subst;

        public SatabakoPassMessage(ErrorSeverity severity, string titleKey, string message, params UnityEngine.Object[] contexts)
        {
            _severity = severity;
            _titleKey = titleKey;
            _subst = new[] { message ?? "" };

            //発生元コンポーネント等をクリック可能な参照として追加 (null安全)
            if (contexts != null)
            {
                foreach (UnityEngine.Object context in contexts)
                {
                    AddReference(ObjectRegistry.GetReference(context));
                }
            }
        }

        public override Localizer Localizer => SatabakoErrorReport.L;
        public override string TitleKey => _titleKey;
        public override ErrorSeverity Severity => _severity;

        // 詳細(description)の {0} にメッセージを差し込む
        public override string[] DetailsSubst => _subst;
    }
}
