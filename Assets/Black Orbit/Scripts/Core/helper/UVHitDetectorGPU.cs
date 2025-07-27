using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Black_Orbit.Scripts.Core.Helper
{
    /// <summary>GPU‑based UV coordinate detection for raycast hits, учитывает static batching и повторно использует буферы.</summary>
    public static class UVHitDetectorGPU
    {
        #region Compute‑shader
        private const string Kernel = "FindUV";
        private static readonly ComputeShader Shader = Resources.Load<ComputeShader>("UVHitFinder");
        private static readonly int KernelId = Shader.FindKernel(Kernel);

        // Compute‑ID кэши
        private static readonly int MaxDistanceID  = UnityEngine.Shader.PropertyToID("_MaxDistance");
        private static readonly int PosOffsetID    = UnityEngine.Shader.PropertyToID("_PosOffset");
        private static readonly int PosHalfID      = UnityEngine.Shader.PropertyToID("_PosHalf");
        private static readonly int UvOffsetID     = UnityEngine.Shader.PropertyToID("_UVOffset");
        private static readonly int TriangleCntID  = UnityEngine.Shader.PropertyToID("_TriangleCount");
        private static readonly int UvIsHalfID     = UnityEngine.Shader.PropertyToID("_UVIsHalf");
        private static readonly int Is16BitID      = UnityEngine.Shader.PropertyToID("_Is16BitIndex");
        private static readonly int RayOriginID    = UnityEngine.Shader.PropertyToID("_RayOrigin");
        private static readonly int RayDirID       = UnityEngine.Shader.PropertyToID("_RayDirection");
        private static readonly int BestDistID     = UnityEngine.Shader.PropertyToID("_BestDist");
        private static readonly int BestUvID       = UnityEngine.Shader.PropertyToID("_BestUV");
        private static readonly int VBufferID      = UnityEngine.Shader.PropertyToID("_VBuffer");
        private static readonly int IndicesID      = UnityEngine.Shader.PropertyToID("_Indices");
        private static readonly int StrideID       = UnityEngine.Shader.PropertyToID("_Stride");
        #endregion

        #region Result buffers
        private static ComputeBuffer _bestDist; // uint (asuint(float))
        private static ComputeBuffer _bestUv;   // float2
        private static readonly uint[] DistInit = { 0x7F7F_FFFF };   // float.MaxValue
        private static readonly Vector2[] UvInit = { Vector2.zero };
        #endregion

        #region Mesh cache
        private readonly struct MeshKey : IEquatable<MeshKey>
        {
            public readonly Mesh Mesh;
            public readonly bool WorldSpace;     // true, если вершины уже в world space (static batching)

            public MeshKey(Mesh mesh, bool worldSpace)
            {
                Mesh = mesh;
                WorldSpace = worldSpace;
            }
            public bool Equals(MeshKey other) => Mesh == other.Mesh && WorldSpace == other.WorldSpace;
            public override bool Equals(object obj) => obj is MeshKey other && Equals(other);
            public override int GetHashCode() => unchecked(Mesh.GetHashCode() * 397) ^ (WorldSpace ? 1 : 0);
        }

        private sealed class MeshCache : IDisposable
        {
            public readonly GraphicsBuffer VertexBuffer;
            public readonly GraphicsBuffer IndexBuffer;
            public readonly int Stride, PosOffset, UvOffset, TriangleCount;
            public readonly bool UvIsHalf, Is16BitIndex, PosIsHalf, WorldSpace;

            public MeshCache(Mesh mesh, bool worldSpace)
            {
                WorldSpace = worldSpace;

                // Обеспечиваем raw‑доступ
                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                mesh.indexBufferTarget  |= GraphicsBuffer.Target.Raw;

                PosOffset   = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
                PosIsHalf   = mesh.GetVertexAttributeFormat(VertexAttribute.Position) == VertexAttributeFormat.Float16;
                UvOffset    = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
                int stream  = mesh.GetVertexAttributeStream(VertexAttribute.Position);
                Stride      = mesh.GetVertexBufferStride(stream);
                VertexBuffer = mesh.GetVertexBuffer(stream);
                IndexBuffer  = mesh.GetIndexBuffer();
                TriangleCount = IndexBuffer.count / 3;
                UvIsHalf      = mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0) == VertexAttributeFormat.Float16;
                Is16BitIndex  = mesh.indexFormat == IndexFormat.UInt16;
            }

            public void Dispose()
            {
                VertexBuffer?.Dispose();
                IndexBuffer?.Dispose();
            }
        }

        // Основной кэш   Mesh+WorldSpace → данные
        private static readonly Dictionary<MeshKey, MeshCache> Cache = new();
        // Для SkinnedMeshRenderer: instanceID → забэйканный меш, чтобы не городить миллион экземпляров
        private static readonly Dictionary<int, Mesh> BakedMeshes = new();
        #endregion

        #region Public API
        /// <summary>Возвращает UV координату в точке RaycastHit. Если не удалось определить — (0,0).</summary>
        public static Vector2 GetHitUV(in RaycastHit hit, in Ray worldRay)
        {
            if (hit.collider == null) return Vector2.zero;
            if (!TryExtractMesh(hit.collider, out var mesh, out var transform)) return Vector2.zero;
            if (mesh == null || mesh.vertexCount == 0) return Vector2.zero;

            bool worldSpace = transform.TryGetComponent(out Renderer r) && r.isPartOfStaticBatch;
            var key = new MeshKey(mesh, worldSpace);

            if (!TryGetOrCreateCache(key, mesh, worldSpace, out var mCache))
                return Vector2.zero;

            EnsureResultBuffers();

            Ray rayForGpu = mCache.WorldSpace ? worldRay : TransformRayToLocal(worldRay, transform);
            float hitDistance = math.dot(hit.point - worldRay.origin, worldRay.direction) + 0.05f;

            DispatchComputeShader(mCache, rayForGpu, hitDistance);
            return ReadResultUv();
        }
        #endregion

        #region Cache helpers
        private static bool TryGetOrCreateCache(MeshKey key, Mesh mesh, bool worldSpace, out MeshCache cache)
        {
            // Есть ли в словаре?
            if (Cache.TryGetValue(key, out cache))
            {
                // жив ли буфер? (может invalid после пересборки static batch)
                if (cache.VertexBuffer != null && cache.VertexBuffer.IsValid())
                    return true;

                cache.Dispose();
                Cache.Remove(key);
            }

            // Создаём заново
            cache = new MeshCache(mesh, worldSpace);
            Cache[key] = cache;
            return ValidateMeshCache(cache);
        }
        #endregion

        #region Extraction helpers
        private static bool TryExtractMesh(Collider col, out Mesh mesh, out Transform tr)
        {
            mesh = null; tr = null;

            if (col.TryGetComponent(out MeshFilter mf))
            {
                mesh = mf.sharedMesh; tr = mf.transform; return true;
            }

            if (col.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                int id = smr.GetInstanceID();
                if (!BakedMeshes.TryGetValue(id, out mesh) || mesh == null)
                {
                    mesh = new Mesh { name = $"Baked({smr.name})" };
                    BakedMeshes[id] = mesh;
                }
                smr.BakeMesh(mesh, true);
                tr = smr.transform;
                return true;
            }
            return false;
        }
        #endregion

        #region Validation / safety
        private static bool ValidateMeshCache(MeshCache c)
        {
            if (c.VertexBuffer == null || c.IndexBuffer == null)
            {
                Debug.LogError("[UVHitDetectorGPU] Mesh buffers are null");
                return false;
            }
            if (!c.VertexBuffer.IsValid() || !c.IndexBuffer.IsValid())
            {
                Debug.LogError("[UVHitDetectorGPU] Mesh buffers are invalid");
                return false;
            }
            if (c.UvOffset < 0)
            {
                Debug.LogWarning("[UVHitDetectorGPU] Mesh has no UV0");
                return false;
            }
            return true;
        }
        #endregion

        #region Result buffer helpers
        private static void EnsureResultBuffers()
        {
            _bestDist ??= new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
            _bestUv   ??= new ComputeBuffer(1, sizeof(float) * 2);
            _bestDist.SetData(DistInit);
            _bestUv.SetData(UvInit);
        }
        #endregion

        #region Math utils
        private static Ray TransformRayToLocal(Ray ray, Transform t)
        {
            Matrix4x4 m = t.worldToLocalMatrix;
            Vector3 o = m.MultiplyPoint(ray.origin);
            Vector3 d = m.MultiplyVector(ray.direction).normalized;
            return new Ray(o, d);
        }
        #endregion

        #region Dispatch
        private static void DispatchComputeShader(MeshCache c, Ray ray, float maxDist)
        {
            Shader.SetBuffer(KernelId, VBufferID,   c.VertexBuffer);
            Shader.SetBuffer(KernelId, IndicesID,   c.IndexBuffer);
            Shader.SetInt   (StrideID,      c.Stride);

            Shader.SetFloat (MaxDistanceID, maxDist);
            Shader.SetInt   (PosOffsetID,   c.PosOffset);
            Shader.SetInt   (PosHalfID,     c.PosIsHalf ? 1 : 0);
            Shader.SetInt   (UvOffsetID,    c.UvOffset);
            Shader.SetInt   (TriangleCntID, c.TriangleCount);
            Shader.SetInt   (UvIsHalfID,    c.UvIsHalf ? 1 : 0);
            Shader.SetInt   (Is16BitID,     c.Is16BitIndex ? 1 : 0);
            Shader.SetVector(RayOriginID,   new Vector4(ray.origin.x, ray.origin.y, ray.origin.z, 0));
            Shader.SetVector(RayDirID,      new Vector4(ray.direction.x, ray.direction.y, ray.direction.z, 0));
            Shader.SetBuffer(KernelId, BestDistID, _bestDist);
            Shader.SetBuffer(KernelId, BestUvID,   _bestUv);

            int tg = (c.TriangleCount + 63) / 64;
            Shader.Dispatch(KernelId, tg, 1, 1);
        }
        #endregion

        #region Read back
        private static Vector2 ReadResultUv()
        {
            Vector2[] res = { Vector2.zero };
            _bestUv.GetData(res);
            return res[0];
        }
        #endregion

        #region Cleanup
        static UVHitDetectorGPU()
        {
            SceneManager.sceneUnloaded += _ => ClearAll();
            Application.quitting += ClearAll;
        }

        private static void ClearAll()
        {
            foreach (var kv in Cache.Values) kv.Dispose();
            Cache.Clear();
            foreach (var m in BakedMeshes.Values) UnityEngine.Object.Destroy(m);
            BakedMeshes.Clear();
            _bestDist?.Dispose(); _bestDist = null;
            _bestUv?.Dispose();   _bestUv   = null;
        }
        #endregion
    }
}
