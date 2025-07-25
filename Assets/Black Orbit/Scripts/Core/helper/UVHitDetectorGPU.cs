using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Black_Orbit.Scripts.Core.Helper
{
    /// <summary> GPU-based UV coordinate detection for raycast hits. </summary>
    public static class UVHitDetectorGPU
    {
        private const string Kernel = "FindUV";
        private static readonly ComputeShader Shader = Resources.Load<ComputeShader>("UVHitFinder");
        private static readonly int KernelId = Shader.FindKernel(Kernel);

        // Mesh data cache
        private sealed class MeshCache : IDisposable
        {
            public readonly GraphicsBuffer VertexBuffer; // RAW vertex buffer
            public readonly GraphicsBuffer IndexBuffer; // RAW index buffer
            public readonly int Stride, PosOffset, UvOffset, TriangleCount;
            public readonly bool UvIsHalf; // TexCoord0 = Float16?
            public readonly bool Is16BitIndex;
            public readonly bool PosHalf;

            public MeshCache(Mesh mesh)
            {
                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

                PosOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
                PosHalf = mesh.GetVertexAttributeFormat(VertexAttribute.Position) == VertexAttributeFormat.Float16;
                UvOffset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
                int stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
                Stride = mesh.GetVertexBufferStride(stream);
                VertexBuffer = mesh.GetVertexBuffer(stream);
                IndexBuffer = mesh.GetIndexBuffer();
                TriangleCount = IndexBuffer.count / 3; // ✅ без Read/Write
                UvIsHalf = mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0) == VertexAttributeFormat.Float16;
                Is16BitIndex = mesh.indexFormat == IndexFormat.UInt16;
            }

            public void Dispose()
            {
                VertexBuffer?.Dispose();
                IndexBuffer?.Dispose();
            }
        }

        private static readonly Dictionary<Mesh, MeshCache> Cache = new();

        // Result buffers
        private static ComputeBuffer _bestDist; // uint asuint(float)
        private static ComputeBuffer _bestUV; // float2
        private static readonly uint[] DistInit = { 0x7F7FFFFF }; // float.MaxValue
        private static readonly Vector2[] UvInit = { Vector2.zero };
        static readonly int MaxDistanceCompute = UnityEngine.Shader.PropertyToID("_MaxDistance");
        static readonly int PosOffsetCompute = UnityEngine.Shader.PropertyToID("_PosOffset");
        static readonly int PosHalfCompute = UnityEngine.Shader.PropertyToID("_PosHalf");
        static readonly int UVOffsetCompute = UnityEngine.Shader.PropertyToID("_UVOffset");
        static readonly int TriangleCountCompute = UnityEngine.Shader.PropertyToID("_TriangleCount");
        static readonly int UVIsHalfCompute = UnityEngine.Shader.PropertyToID("_UVIsHalf");
        static readonly int Is16BitIndexCompute = UnityEngine.Shader.PropertyToID("_Is16BitIndex");
        static readonly int RayOriginCompute = UnityEngine.Shader.PropertyToID("_RayOrigin");
        static readonly int RayDirectionCompute = UnityEngine.Shader.PropertyToID("_RayDirection");
        static readonly int BestDistCompute = UnityEngine.Shader.PropertyToID("_BestDist");
        static readonly int BestUVCompute = UnityEngine.Shader.PropertyToID("_BestUV");
        static readonly int VBufferCompute = UnityEngine.Shader.PropertyToID("_VBuffer");
        static readonly int IndicesCompute = UnityEngine.Shader.PropertyToID("_Indices");
        static readonly int StrideCompute = UnityEngine.Shader.PropertyToID("_Stride");

        /// <summary> Get UV coordinates at the raycast hit point using GPU. </summary>
        public static Vector2 GetHitUV(in RaycastHit hit, in Ray ray)
        {
            if (hit.collider == null) return Vector2.zero;

            Mesh mesh;
            Transform transform;
            if (hit.collider.TryGetComponent(out MeshFilter mf))
            {
                mesh = mf.sharedMesh;
                transform = mf.transform;
            }
            else if (hit.collider.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                mesh = new Mesh();
                smr.BakeMesh(mesh);
                transform = smr.transform;
            }
            else
            {
                return Vector2.zero;
            }

            if (mesh is null || mesh.vertexCount == 0)
            {
                return Vector2.zero;
            }

            if (!Cache.TryGetValue(mesh, out MeshCache cache) || cache.VertexBuffer == null)
            {
                Cache[mesh] = cache = new MeshCache(mesh);
            }


            if (cache.VertexBuffer == null || cache.IndexBuffer == null)
            {
                Debug.LogError("Mesh buffers are not initialized!");
                return Vector2.zero;
            }

            if (cache.UvOffset < 0)
            {
                Debug.LogWarning("Mesh does not contain UV coordinates!");
                return Vector2.zero;
            }

            _bestDist ??= new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
            _bestUV ??= new ComputeBuffer(1, sizeof(float) * 2);
            _bestDist.SetData(DistInit);
            _bestUV.SetData(UvInit);

            // Важно: правильное преобразование луча в локальное пространство меша
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector3 rayOrigin = worldToLocal.MultiplyPoint(ray.origin);
            Vector3 rayDir = worldToLocal.MultiplyVector(ray.direction).normalized;

            Shader.SetBuffer(KernelId, VBufferCompute, cache.VertexBuffer);
            Shader.SetBuffer(KernelId, IndicesCompute, cache.IndexBuffer);
            Shader.SetInt(StrideCompute, cache.Stride);

            float hitDistance = math.dot(hit.point - ray.origin, ray.direction);

            Shader.SetFloat(MaxDistanceCompute, hitDistance);
            Shader.SetInt(PosOffsetCompute, cache.PosOffset);
            Shader.SetInt(PosHalfCompute, cache.PosHalf ? 1 : 0);
            Shader.SetInt(UVOffsetCompute, cache.UvOffset);
            Shader.SetInt(TriangleCountCompute, cache.TriangleCount);
            Shader.SetInt(UVIsHalfCompute, cache.UvIsHalf ? 1 : 0);
            Shader.SetInt(Is16BitIndexCompute, cache.Is16BitIndex ? 1 : 0);
            Shader.SetVector(RayOriginCompute, new Vector4(rayOrigin.x, rayOrigin.y, rayOrigin.z, 0));
            Shader.SetVector(RayDirectionCompute, new Vector4(rayDir.x, rayDir.y, rayDir.z, 0));
            Shader.SetBuffer(KernelId, BestDistCompute, _bestDist);
            Shader.SetBuffer(KernelId, BestUVCompute, _bestUV);

            Shader.Dispatch(KernelId, (cache.TriangleCount + 63) / 64, 1, 1);

            // --- СИНХРОННО считаем 1 элемент ---
            Vector2[] uvOut = { Vector2.zero };
            _bestUV.GetData(uvOut); // микроскопический stall, терпимо

            return uvOut[0];
        }
    }
}
