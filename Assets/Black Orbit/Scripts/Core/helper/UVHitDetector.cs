namespace Black_Orbit.Scripts.Core.helper
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// Utility to get UV coordinates on the visual mesh where a RaycastHit occurred.
    /// </summary>
    public static class UVHitDetector
    {
        // Simple cache of mesh data to avoid re-reading arrays every time
        private static readonly Dictionary<Mesh, MeshData> _meshDataCache = new Dictionary<Mesh, MeshData>();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// Returns the interpolated UV coordinate on the rendered mesh at the hit point.
        /// Works with MeshFilter and SkinnedMeshRenderer (baked). Ignores collider UVs.
        /// </summary>
        public static Vector2 GetHitUV(RaycastHit hit)
        {
            var col = hit.collider;
            if (col == null)
                return Vector2.zero;

            Mesh mesh;
            Transform transform;

            // Try MeshFilter first
            if (col.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
            {
                mesh = filter.sharedMesh;
                transform = filter.transform;
            }
            // Fallback to skinned mesh bake
            else if (col.TryGetComponent<SkinnedMeshRenderer>(out var skinned) && skinned.sharedMesh != null)
            {
                mesh = new Mesh();
                skinned.BakeMesh(mesh);
                transform = skinned.transform;
            }
            else
            {
                return Vector2.zero;
            }

            // Get cached vertex/uv/triangle arrays
            var data = GetMeshData(mesh);

            // Ray for local intersection
            var ray = new Ray(hit.point + hit.normal * 0.01f, -hit.normal);
            var localToWorld = transform.localToWorldMatrix;
            float closestDist = float.MaxValue;
            Vector2 bestUV = Vector2.zero;

            // For each triangle, test intersection
            var tris = data.Triangles;
            var verts = data.Vertices;
            var uvs = data.UVs;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = localToWorld.MultiplyPoint3x4(verts[tris[i]]);
                Vector3 v1 = localToWorld.MultiplyPoint3x4(verts[tris[i + 1]]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(verts[tris[i + 2]]);

                if (RayIntersectsTriangle(ray, v0, v1, v2, out Vector3 hitPoint, out Vector3 bary))
                {
                    float dist = (ray.origin - hitPoint).sqrMagnitude;
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestUV = uvs[tris[i]] * bary.x + uvs[tris[i + 1]] * bary.y + uvs[tris[i + 2]] * bary.z;
                    }
                }
            }

            return bestUV;
        }

        private static MeshData GetMeshData(Mesh mesh)
        {
            lock (_cacheLock)
            {
                if (!_meshDataCache.TryGetValue(mesh, out var data))
                {
                    data = new MeshData(mesh);
                    _meshDataCache[mesh] = data;
                }
                return data;
            }
        }

        private static bool RayIntersectsTriangle(
            Ray ray,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            out Vector3 hitPoint,
            out Vector3 barycentric)
        {
            hitPoint = Vector3.zero;
            barycentric = Vector3.zero;
            
            // Moller-Trumbore intersection
            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 p = Vector3.Cross(ray.direction, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < Mathf.Epsilon) return false;
            float invDet = 1f / det;
            Vector3 tVec = ray.origin - v0;
            float u = Vector3.Dot(tVec, p) * invDet;
            if (u < 0f || u > 1f) return false;
            Vector3 q = Vector3.Cross(tVec, e1);
            float v = Vector3.Dot(ray.direction, q) * invDet;
            if (v < 0f || u + v > 1f) return false;
            float t = Vector3.Dot(e2, q) * invDet;
            if (t <= Mathf.Epsilon) return false;

            hitPoint = ray.origin + ray.direction * t;
            barycentric = new Vector3(1f - u - v, u, v);
            return true;
        }

        private class MeshData
        {
            public readonly Vector3[] Vertices;
            public readonly Vector2[] UVs;
            public readonly int[] Triangles;

            public MeshData(Mesh mesh)
            {
                Vertices = mesh.vertices;
                UVs = mesh.uv;
                Triangles = mesh.triangles;
            }
        }
    }
}
