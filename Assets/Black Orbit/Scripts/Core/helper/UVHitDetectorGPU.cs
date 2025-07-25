using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Black_Orbit.Scripts.Core.Helper
{
    /// <summary> GPU-based UV coordinate detection for raycast hits. </summary>
    public static class UVHitDetectorGPU
    {
        private const string KERNEL = "FindUV";
        private static readonly ComputeShader Shader = Resources.Load<ComputeShader>("UVHitFinder");
        private static readonly int KernelId = Shader.FindKernel(KERNEL);

        // Mesh data cache
        private sealed class MeshCache : IDisposable
        {
            public readonly GraphicsBuffer VertexBuffer; // RAW vertex buffer
            public readonly GraphicsBuffer IndexBuffer;  // RAW index buffer
            public readonly int Stride, PosOffset, UvOffset, TriangleCount;
            public readonly bool UvIsHalf;               // TexCoord0 = Float16?
            public readonly bool Is16BitIndex;
            public readonly bool posHalf;

            public MeshCache(Mesh mesh)
            {
                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                mesh.indexBufferTarget  |= GraphicsBuffer.Target.Raw;

                PosOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
                posHalf = mesh.GetVertexAttributeFormat(VertexAttribute.Position)
                            == VertexAttributeFormat.Float16;
                UvOffset  = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
                int stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
                Stride     = mesh.GetVertexBufferStride(stream);
                VertexBuffer = mesh.GetVertexBuffer(stream);
                IndexBuffer  = mesh.GetIndexBuffer();
                TriangleCount = IndexBuffer.count / 3;          // ✅ без Read/Write
                UvIsHalf      = mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0)
                                == VertexAttributeFormat.Float16;
                Is16BitIndex  = mesh.indexFormat == IndexFormat.UInt16;
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
        private static ComputeBuffer _bestUV;   // float2
        private static readonly uint[] DistInit = { 0x7F7FFFFF }; // float.MaxValue
        private static readonly Vector2[] UvInit = { Vector2.zero };

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

            if (mesh == null || mesh.vertexCount == 0)
            {
                return Vector2.zero;
            }

            if (!Cache.TryGetValue(mesh, out var cache) || cache.VertexBuffer == null)
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
            
            Shader.SetBuffer(KernelId, "_VBuffer", cache.VertexBuffer);
            Shader.SetBuffer(KernelId, "_Indices", cache.IndexBuffer);
            Shader.SetInt("_Stride", cache.Stride);
            Shader.SetFloat("_MaxDistance", 10f);
            Shader.SetInt("_PosOffset", cache.PosOffset);
            Shader.SetInt("_PosHalf", cache.posHalf ? 1 : 0);
            Shader.SetInt("_UVOffset", cache.UvOffset);
            Shader.SetInt("_TriangleCount", cache.TriangleCount);
            Shader.SetInt("_UVIsHalf", cache.UvIsHalf ? 1 : 0);
            Shader.SetInt("_Is16BitIndex", cache.Is16BitIndex ? 1 : 0);
            Shader.SetVector("_RayOrigin", new Vector4(rayOrigin.x, rayOrigin.y, rayOrigin.z, 0));
            Shader.SetVector("_RayDirection", new Vector4(rayDir.x, rayDir.y, rayDir.z, 0));
            Shader.SetBuffer(KernelId, "_BestDist", _bestDist);
            Shader.SetBuffer(KernelId, "_BestUV", _bestUV);

            Shader.Dispatch(KernelId, (cache.TriangleCount + 63) / 64, 1, 1);

            // --- СИНХРОННО считаем 1 элемент ---
            Vector2[] uvOut = { Vector2.zero };
            _bestUV.GetData(uvOut);   // микроскопический stall, терпимо
            
            return uvOut[0];
        }
    }
}