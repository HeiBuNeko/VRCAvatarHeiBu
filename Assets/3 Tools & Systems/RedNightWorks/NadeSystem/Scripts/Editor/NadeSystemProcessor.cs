using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using System.Linq;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace RedNightWorks.NadeSystem
{
    public class NadeSystemProcessor : MonoBehaviour
    {
        public static void MainProcess(BuildContext ctx)
        {
            Debug.Log("NadeSystemProcessor.MainProcess: Nade System Initialized");

            NadeSystemSettings nadeSystem = ctx.AvatarRootObject.GetComponentInChildren<NadeSystemSettings>();
            if (nadeSystem == null)
            {
                Debug.Log("NadeSystemProcessor.MainProcess: nadeSystem is null");
                return;
            }

            var audioClips = nadeSystem.audioClips;
            Debug.Log(audioClips.Length);
            if (audioClips == null || audioClips.Length != 16)
            {
                Debug.LogWarning("NadeSystemProcessor.MainProcess: Invalid audioClips array. Expected 16 clips.");
                DestroyImmediate(nadeSystem);
                return;
            }

            //MA MergeAnimator取得
            var mergeAnimator = nadeSystem.gameObject.GetComponent<ModularAvatarMergeAnimator>();
            if (mergeAnimator == null)
            {
                Debug.Log("NadeSystemProcessor.MainProcess: mergeAnimator is null");
                DestroyImmediate(nadeSystem);
                return;
            }

            //AnimationController取得
            var animatorController = mergeAnimator.animator as AnimatorController;
            if (animatorController == null)
            {
                Debug.Log("NadeSystemProcessor.MainProcess: animatorController is null");
                DestroyImmediate(nadeSystem);
                return;
            }

            var rightHandStates = FindStatesByNamePrefix(animatorController, "NadeSoundSelect", "SetRightHandSound ");
            var leftHandStates = FindStatesByNamePrefix(animatorController, "NadeSoundSelect", "SetLeftHandSound ");
            var headStates = FindStatesByNamePrefix(animatorController, "NaderareSoundSelect", "SetHeadSound ");

            if (rightHandStates.Count != 16 || leftHandStates.Count != 16 || headStates.Count != 16)
            {
                Debug.LogWarning("NadeSystemProcessor.MainProcess: Not enough states found. Expected 16 states for each hand and head.");
                DestroyImmediate(nadeSystem);
                return;
            }

            // ステートにオーディオクリップを割り当て
            AssignAudioClipsToStates(rightHandStates, audioClips);
            AssignAudioClipsToStates(leftHandStates, audioClips);
            AssignAudioClipsToStates(headStates, audioClips);

            for(int i = 0; i < 16; i++)
            {
                var audioClip = audioClips[i];
                if (audioClip != null)
                {
                    AudioClipImportSettings(audioClip); // オーディオクリップのインポート設定を適用
                    Debug.Log($"NadeSystemProcessor.MainProcess: Processing audio clip '{audioClip.name}' at index {i}");
                    CreateSoundItem(audioClip.name, i, "RNW/Nade/HandsSound", nadeSystem.nadeSoundListTarget);
                    CreateSoundItem(audioClip.name, i, "RNW/Nade/HeadSound", nadeSystem.naderareSoundListTarget);
                }
            }

            DestroyImmediate(nadeSystem);
        }

        private static void AudioClipImportSettings(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                Debug.LogWarning("NadeSystemProcessor.AudioClipImportSettings: AudioClip is null.");
                return;
            }

            // オーディオクリップのインポート設定を行う
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(audioClip)) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"NadeSystemProcessor.AudioClipImportSettings: Could not find AudioImporter for {audioClip.name}.");
                return ;
            }

            // 既に設定されている場合は何もしない
            if (importer.forceToMono == true &&
                importer.loadInBackground == true &&
                importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming &&
                importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.Vorbis)
            {
                return;
            }

            importer.forceToMono = true; // モノラルに変換
            importer.loadInBackground = true; // バックグラウンドで読み込み
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.Streaming, // ストリーミング
                compressionFormat = AudioCompressionFormat.Vorbis, // Vorbis形式で圧縮
            };

            importer.SaveAndReimport();
        }

        /// <summary>
        /// 指定したプレハブを指定したゲームオブジェクトの下にインスタンス化します。
        /// </summary>
        /// <param name="prefab">インスタンス化するプレハブ</param>
        /// <param name="parent">親となるゲームオブジェクト</param>
        private static void CreateSoundItem(string name, int val, string param, GameObject parent)
        {
            if (parent == null)
            {
                Debug.LogWarning("NadeSystemProcessor.InstantiatePrefab: Prefab or parent is null.");
                return;
            }
            var soundItem = new GameObject(name);
            soundItem.transform.parent = parent.transform;
            soundItem.AddComponent<ModularAvatarMenuItem>();
            soundItem.GetComponent<ModularAvatarMenuItem>().isSaved = false;
            soundItem.GetComponent<ModularAvatarMenuItem>().Control.parameter = new VRCExpressionsMenu.Control.Parameter();
            soundItem.GetComponent<ModularAvatarMenuItem>().Control.parameter.name = param;
            soundItem.GetComponent<ModularAvatarMenuItem>().Control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
            soundItem.GetComponent<ModularAvatarMenuItem>().Control.value = val;
        }

        /// <summary>
        /// 指定されたステートのリストにオーディオクリップを割り当てます。
        /// </summary>
        /// <param name="states">対象のAnimatorStateのリスト</param>
        /// <param name="clips">割り当てるAudioClipの配列</param>
        private static void AssignAudioClipsToStates(System.Collections.Generic.List<AnimatorState> states, AudioClip[] clips)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (i >= clips.Length) // クリップ配列の範囲外アクセスを防止
                {
                    Debug.LogWarning($"NadeSystemProcessor.AssignAudioClipsToStates: Not enough audio clips for all states. Stopping at index {i}.");
                    break;
                }

                var state = states[i];
                var playAudioBehaviour = state.behaviours.OfType<VRC_AnimatorPlayAudio>().FirstOrDefault();

                if (playAudioBehaviour != null)
                {
                    var audioClip = clips[i];
                    if (audioClip == null)
                    {
                        continue;
                    }
                    playAudioBehaviour.Clips = new AudioClip[] { audioClip }; // 配列を更新
                    Debug.Log($"NadeSystemProcessor.MainProcess: Updated audio clip for state '{state.name}' => {playAudioBehaviour.Clips[0].name}");
                }
            }
        }

        /// <summary>
        /// AnimatorControllerの指定したレイヤーから、名前に特定のプレフィックスを持つすべてのステートを検索します。
        /// </summary>
        /// <param name="controller">検索対象のAnimatorController</param>
        /// <param name="layerName">検索対象のレイヤー名</param>
        /// <param name="stateNamePrefix">ステート名のプレフィックス</param>
        /// <returns>見つかったAnimatorStateのリスト</returns>
        public static System.Collections.Generic.List<AnimatorState> FindStatesByNamePrefix(AnimatorController controller, string layerName, string stateNamePrefix)
        {
            var foundStates = new System.Collections.Generic.List<AnimatorState>();
            if (controller == null || string.IsNullOrEmpty(layerName) || string.IsNullOrEmpty(stateNamePrefix))
            {
                Debug.LogWarning("NadeSystemProcessor.FindStatesByNamePrefix: Invalid parameters provided.");
                return foundStates;
            }

            var targetLayer = System.Array.Find(controller.layers, layer => layer.name == layerName);
            if (targetLayer == null)
            {
                Debug.LogWarning($"NadeSystemProcessor.FindStatesByNamePrefix: Layer '{layerName}' not found in the AnimatorController.");
                return foundStates;
            }

            // ステートマシン内を再帰的に検索する関数
            System.Action<AnimatorStateMachine> findStatesRecursive = null;
            findStatesRecursive = (stateMachine) =>
            {
                // ステートマシン直下のステートを検索
                foreach (var childState in stateMachine.states)
                {
                    if (childState.state.name.StartsWith(stateNamePrefix))
                    {
                        foundStates.Add(childState.state);
                    }
                }
                // サブステートマシン内を再帰的に検索
                foreach (var childMachine in stateMachine.stateMachines)
                {
                    findStatesRecursive(childMachine.stateMachine);
                }
            };

            findStatesRecursive(targetLayer.stateMachine);
            return foundStates;
        }
    }
}