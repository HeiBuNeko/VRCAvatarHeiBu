using UnityEditor;
using UnityEngine;

namespace VRC.SDKBase.Editor.Elements
{
    public class ThumbnailAccuracyWarningWindow : EditorWindow
    {
        private const string Message =
            "Content should accurately represent what a user will experience. Names, tags, descriptions, images, acknowledgements and previews (including thumbnails) should not be misleading. If your content looks different in VRChat than in the images you've used to showcase it, users may have a negative experience.";

        private static readonly Vector2 WindowSize = new Vector2(520, 200);
        private static GUIStyle _paddingStyle;

        private bool _dontShowAgain;
        private bool _confirmed;

        /// <summary>
        /// Shows the warning modally. Returns true if the user continued, or previously opted out for this project.
        /// </summary>
        public static bool Confirm()
        {
            if (VRCPackageSettings.Instance.thumbnailAccuracyWarningDismissed) return true;

            var window = CreateInstance<ThumbnailAccuracyWarningWindow>();
            window.titleContent = new GUIContent("Thumbnail Accuracy Warning");
            window.minSize = window.maxSize = WindowSize;
            var mainWindow = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(mainWindow.center - WindowSize / 2, WindowSize);
            window.ShowModal();
            return window._confirmed;
        }

        private void OnGUI()
        {
            _paddingStyle ??= new GUIStyle { padding = new RectOffset(12, 12, 12, 12) };

            using (new EditorGUILayout.VerticalScope(_paddingStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(48), GUILayout.Height(48));
                    GUILayout.Space(8);
                    GUILayout.Label(Message, EditorStyles.wordWrappedLabel);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Continue", GUILayout.Width(100)))
                    {
                        if (_dontShowAgain)
                        {
                            VRCPackageSettings.Instance.thumbnailAccuracyWarningDismissed = true;
                            VRCPackageSettings.Instance.Save();
                        }
                        _confirmed = true;
                        Close();
                        GUIUtility.ExitGUI();
                    }
                    if (GUILayout.Button("Back", GUILayout.Width(100)))
                    {
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.Space(8);
                _dontShowAgain = EditorGUILayout.ToggleLeft("Don't show again for this project", _dontShowAgain);
            }
        }
    }
}
