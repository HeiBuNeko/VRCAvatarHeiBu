using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class PreviewViewport : IDisposable
    {
        [Serializable]
        public struct CameraState
        {
            public Vector3 pivot;
            public Vector2 orbit;
            public float distance;
            public bool orthographic;

            public static CameraState Default => new CameraState { pivot = new Vector3(0f, 1f, 0f), orbit = new Vector2(10f, 180f), distance = 2f };

            public Quaternion Rotation => Quaternion.Euler(orbit.x, orbit.y, 0f);
            public Vector3 Position => pivot - Rotation * Vector3.forward * distance;
        }

        public enum Tool
        {
            Move,
            Rotate,
        }

        private const float k_orbitSpeed = 0.4f;
        private const float k_zoomSpeed = 0.05f;
        private const float k_minDistance = 0.05f;
        private const float k_maxDistance = 20f;
        private static readonly Color k_background = new Color(0.30f, 0.31f, 0.35f);
        private static readonly Color k_gridColor = new Color(1f, 1f, 1f, 0.12f);
        private static Vector3[] s_gridLines;

        private PreviewRenderUtility _pru;
        private Scene _scene;
        private string _captureRequest;

        public Scene Scene => _scene;
        public Camera Camera => _pru != null ? _pru.camera : null;
        public bool IsCreated => _pru != null && _scene.IsValid();
        public bool DrawGizmos { get; set; }
        public bool ShowGrid { get; set; } = true;
        public bool HasKeyboardFocus { get; private set; }
        public Rect LastViewportRect { get; private set; }
        public readonly List<Rect> IgnoreMouseRects = new List<Rect>();

        public Action<Rect> HandlesGUI;
        public Func<KeyCode, bool> KeyDown;
        public Func<Bounds?> FrameTarget;

        public void RequestCapture(string path) => _captureRequest = path;

        private void CaptureActiveRenderTexture()
        {
            var rt = RenderTexture.active;
            string path = _captureRequest;
            _captureRequest = null;
            if (rt == null || string.IsNullOrEmpty(path))
                return;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log($"[PreviewViewport] captured: {path} ({rt.width}x{rt.height})");
        }

        public void EnsureCreated()
        {
            if (IsCreated)
                return;

            Dispose();
            _pru = new PreviewRenderUtility();
            var cam = _pru.camera;
            cam.cameraType = CameraType.SceneView;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = k_background;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            _pru.lights[0].intensity = 1.0f;
            _pru.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            _pru.lights[1].intensity = 0.3f;
            _pru.ambientColor = new Color(0.35f, 0.35f, 0.35f);
            _scene = cam.scene;
        }

        public void Dispose()
        {
            if (_pru != null)
            {
                _pru.Cleanup();
                _pru = null;
            }
            _scene = default;
        }

        public void Tick()
        {
            if (IsCreated)
                EditorApplication.QueuePlayerLoopUpdate();
        }

        public void OnGUI(Rect local, ref CameraState camera)
        {
            LastViewportRect = local;
            if (!IsCreated)
            {
                EditorGUI.DrawRect(local, k_background);
                return;
            }

            var e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Keyboard);
            var cam = _pru.camera;

            bool overIgnored = e.isMouse && IsMouseIgnored(e.mousePosition);

            if (e.type == EventType.MouseDown && local.Contains(e.mousePosition) && !overIgnored)
                GUIUtility.keyboardControl = controlId;
            HasKeyboardFocus = GUIUtility.keyboardControl == controlId;

            HandleKeys(e, ref camera);
            if (!overIgnored)
                HandleNavigation(local, e, ref camera);
            cam.transform.SetPositionAndRotation(camera.Position, camera.Rotation);
            cam.orthographic = camera.orthographic;
            cam.orthographicSize = camera.distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            if (e.type == EventType.Repaint)
            {
                _pru.BeginPreview(local, GUIStyle.none);
                Handles.DrawCamera(local, cam, DrawCameraMode.Textured, DrawGizmos);
                Handles.SetCamera(local, cam);
                if (ShowGrid)
                    DrawGrid();
                HandlesGUI?.Invoke(local);
                if (!string.IsNullOrEmpty(_captureRequest))
                    CaptureActiveRenderTexture();
                _pru.EndAndDrawPreview(local);
            }
            else if (!overIgnored)
            {
                Handles.SetCamera(local, cam);
                BeginHandlesClip(cam, local);
                HandlesGUI?.Invoke(local);
                GUI.EndClip();
            }

            Camera.SetupCurrent(null);
        }

        private bool IsMouseIgnored(Vector2 position)
        {
            foreach (var rect in IgnoreMouseRects)
            {
                if (rect.width > 0f && rect.Contains(position))
                    return true;
            }
            return false;
        }

        private static void BeginHandlesClip(Camera cam, Rect local)
        {
            var rt = cam.targetTexture;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            var clip = rt != null ? new Rect(0f, 0f, rt.width, rt.height) : new Rect(0f, 0f, local.width * ppp, local.height * ppp);
            GUI.BeginClip(clip, Vector2.zero, Vector2.zero, true);
        }

        private void HandleKeys(Event e, ref CameraState camera)
        {
            if (!HasKeyboardFocus || e.type != EventType.KeyDown)
                return;

            if (e.keyCode == KeyCode.F)
            {
                var bounds = FrameTarget?.Invoke();
                if (bounds.HasValue)
                {
                    Frame(ref camera, bounds.Value);
                    e.Use();
                }
                return;
            }

            if (KeyDown != null && KeyDown(e.keyCode))
                e.Use();
        }

        private static void HandleNavigation(Rect local, Event e, ref CameraState camera)
        {
            if (GUIUtility.hotControl != 0)
                return;
            if (!local.Contains(e.mousePosition) && e.type != EventType.MouseDrag)
                return;

            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (e.button == 1 || (e.button == 0 && e.alt))
                    {
                        camera.orbit += new Vector2(e.delta.y, e.delta.x) * k_orbitSpeed;
                        camera.orbit.x = Mathf.Clamp(camera.orbit.x, -89f, 89f);
                        e.Use();
                    }
                    else if (e.button == 2)
                    {
                        var rot = camera.Rotation;
                        camera.pivot -= (rot * Vector3.right * e.delta.x - rot * Vector3.up * e.delta.y) * (camera.distance * 0.0015f);
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    camera.distance = Mathf.Clamp(camera.distance * (1f + e.delta.y * k_zoomSpeed), k_minDistance, k_maxDistance);
                    e.Use();
                    break;
            }
        }

        private static void DrawGrid()
        {
            if (s_gridLines == null)
            {
                const int half = 8;
                const float step = 0.25f;
                var lines = new Vector3[(half * 2 + 1) * 4];
                int n = 0;
                for (int i = -half; i <= half; i++)
                {
                    float v = i * step;
                    lines[n++] = new Vector3(v, 0f, -half * step);
                    lines[n++] = new Vector3(v, 0f, half * step);
                    lines[n++] = new Vector3(-half * step, 0f, v);
                    lines[n++] = new Vector3(half * step, 0f, v);
                }
                s_gridLines = lines;
            }

            var color = Handles.color;
            var zTest = Handles.zTest;
            Handles.color = k_gridColor;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.DrawLines(s_gridLines);
            Handles.zTest = zTest;
            Handles.color = color;
        }

        public static void Frame(ref CameraState camera, Bounds bounds)
        {
            camera.pivot = bounds.center;
            camera.distance = Mathf.Clamp(bounds.extents.magnitude * 2.2f, 0.3f, k_maxDistance);
        }

        public static void LookAt(ref CameraState camera, Vector3 pivot, float distance, Vector2 orbit)
        {
            camera.pivot = pivot;
            camera.distance = Mathf.Clamp(distance, k_minDistance, k_maxDistance);
            camera.orbit = orbit;
        }

        public static Bounds? RendererBounds(GameObject root)
        {
            if (root == null)
                return null;

            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds? bounds = null;
            foreach (var r in renderers)
            {
                if (!r.enabled || r.forceRenderingOff)
                    continue;
                if (bounds.HasValue)
                {
                    var b = bounds.Value;
                    b.Encapsulate(r.bounds);
                    bounds = b;
                }
                else
                    bounds = r.bounds;
            }
            return bounds;
        }
    }
}
