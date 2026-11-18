using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class VisemeMouthEstimator
    {
        private static readonly VRC_AvatarDescriptor.Viseme[] k_visemes =
        {
            VRC_AvatarDescriptor.Viseme.aa,
            VRC_AvatarDescriptor.Viseme.oh,
            VRC_AvatarDescriptor.Viseme.ou,
            VRC_AvatarDescriptor.Viseme.ih,
            VRC_AvatarDescriptor.Viseme.E,
            VRC_AvatarDescriptor.Viseme.FF,
        };

        public static readonly Vector3 Offset = new Vector3(0f, -0.003f, 0.002f);

        public static bool HasVisemeMesh(VRCAvatarDescriptor descriptor)
        {
            return descriptor != null
                && descriptor.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape
                && descriptor.VisemeSkinnedMesh != null
                && descriptor.VisemeSkinnedMesh.sharedMesh != null;
        }

        public static bool TryEstimate(VRCAvatarDescriptor descriptor, Transform avatarRoot, out Vector3 rootLocal, out int usedVisemes)
        {
            rootLocal = default;
            usedVisemes = 0;
            if (!HasVisemeMesh(descriptor) || avatarRoot == null)
                return false;

            var smr = descriptor.VisemeSkinnedMesh;
            var mesh = smr.sharedMesh;
            var skinner = new MeshSkinner(smr, mesh);
            var fronts = new List<Vector3>();

            foreach (var viseme in k_visemes)
            {
                int index = GetShapeIndex(descriptor, mesh, viseme);
                if (index < 0)
                    continue;

                var stats = VisemeStats.Compute(skinner, mesh, index, avatarRoot);
                if (stats != null)
                    fronts.Add(stats.frontPoint);
            }

            if (fronts.Count == 0)
                return false;

            rootLocal = new Vector3(Median(fronts.Select(v => v.x)), Median(fronts.Select(v => v.y)), Median(fronts.Select(v => v.z))) + Offset;
            usedVisemes = fronts.Count;
            return true;
        }

        public static int GetShapeIndex(VRCAvatarDescriptor descriptor, Mesh mesh, VRC_AvatarDescriptor.Viseme viseme)
        {
            var names = descriptor.VisemeBlendShapes;
            if (names == null || (int)viseme >= names.Length || string.IsNullOrEmpty(names[(int)viseme]))
                return -1;

            return mesh.GetBlendShapeIndex(names[(int)viseme]);
        }

        private static float Median(IEnumerable<float> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0)
                return 0f;
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : 0.5f * (sorted[mid - 1] + sorted[mid]);
        }

        internal sealed class MeshSkinner
        {
            private readonly Matrix4x4[] _skinMatrices;
            private readonly BoneWeight[] _weights;
            private readonly Matrix4x4 _localToWorld;

            public bool IsSkinned => _skinMatrices != null;

            public MeshSkinner(SkinnedMeshRenderer smr, Mesh mesh)
            {
                _localToWorld = smr.transform.localToWorldMatrix;

                var bones = smr.bones;
                var bindposes = mesh.bindposes;
                var weights = mesh.boneWeights;
                if (bones == null || bindposes == null || weights == null || weights.Length != mesh.vertexCount || bones.Length == 0)
                    return;

                int count = Mathf.Min(bones.Length, bindposes.Length);
                _skinMatrices = new Matrix4x4[count];
                for (int i = 0; i < count; i++)
                    _skinMatrices[i] = bones[i] != null ? bones[i].localToWorldMatrix * bindposes[i] : _localToWorld;
                _weights = weights;
            }

            public Vector3 ToWorld(int vertexIndex, Vector3 meshPosition)
            {
                if (_skinMatrices == null)
                    return _localToWorld.MultiplyPoint3x4(meshPosition);

                var w = _weights[vertexIndex];
                Vector3 result = Vector3.zero;
                float total = 0f;
                Accumulate(ref result, ref total, w.boneIndex0, w.weight0, meshPosition);
                Accumulate(ref result, ref total, w.boneIndex1, w.weight1, meshPosition);
                Accumulate(ref result, ref total, w.boneIndex2, w.weight2, meshPosition);
                Accumulate(ref result, ref total, w.boneIndex3, w.weight3, meshPosition);
                return total > 1e-6f ? result / total : _localToWorld.MultiplyPoint3x4(meshPosition);
            }

            private void Accumulate(ref Vector3 result, ref float total, int bone, float weight, Vector3 p)
            {
                if (weight <= 0f || bone < 0 || bone >= _skinMatrices.Length)
                    return;
                result += _skinMatrices[bone].MultiplyPoint3x4(p) * weight;
                total += weight;
            }
        }

        internal sealed class VisemeStats
        {
            public int count;
            public int strongCount;
            public Vector3 weightedCentroid;
            public Vector3 strongCentroid;
            public Vector3 bboxCenter;
            public Vector3 frontPoint;

            public static VisemeStats Compute(MeshSkinner skinner, Mesh mesh, int shapeIndex, Transform root)
            {
                int frame = mesh.GetBlendShapeFrameCount(shapeIndex) - 1;
                if (frame < 0)
                    return null;

                int n = mesh.vertexCount;
                var deltas = new Vector3[n];
                var normals = new Vector3[n];
                var tangents = new Vector3[n];
                mesh.GetBlendShapeFrameVertices(shapeIndex, frame, deltas, normals, tangents);

                float maxMag = 0f;
                for (int i = 0; i < n; i++)
                    maxMag = Mathf.Max(maxMag, deltas[i].magnitude);
                if (maxMag < 1e-7f)
                    return null;

                var vertices = mesh.vertices;
                float minThreshold = maxMag * 0.02f;
                float strongThreshold = maxMag * 0.3f;

                var stats = new VisemeStats();
                Vector3 weighted = Vector3.zero;
                float weightSum = 0f;
                var strong = new List<Vector3>();
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                for (int i = 0; i < n; i++)
                {
                    float mag = deltas[i].magnitude;
                    if (mag < minThreshold)
                        continue;

                    Vector3 local = root.InverseTransformPoint(skinner.ToWorld(i, vertices[i]));
                    stats.count++;
                    weighted += local * mag;
                    weightSum += mag;

                    if (mag >= strongThreshold)
                    {
                        strong.Add(local);
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }

                if (stats.count == 0 || strong.Count == 0)
                    return null;

                stats.weightedCentroid = weighted / weightSum;
                stats.strongCount = strong.Count;
                stats.strongCentroid = strong.Aggregate(Vector3.zero, (a, v) => a + v) / strong.Count;
                stats.bboxCenter = (min + max) * 0.5f;

                var front = strong.OrderByDescending(v => v.z).Take(Mathf.Max(1, strong.Count / 20)).ToList();
                stats.frontPoint = front.Aggregate(Vector3.zero, (a, v) => a + v) / front.Count;
                return stats;
            }
        }
    }
}
