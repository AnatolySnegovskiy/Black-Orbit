using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Black_Orbit.Scripts.Core.Helper
{
    /// <summary> GPU‑поиск UV‑координаты попадания. </summary>
    public static class UVHitDetectorGPU
    {
        private const string KERNEL = "FindUV";
        private static readonly ComputeShader Shader =
            Resources.Load<ComputeShader>("UVHitFinder");

        private static readonly int Kid = Shader.FindKernel(KERNEL);

        // ---------- кеш -----------------------------------------------------
        private sealed class MeshCache : IDisposable
        {
            public readonly GraphicsBuffer VBuf;   // RAW vertex buffer
            public readonly GraphicsBuffer IBuf;   // index buffer
            public readonly int Stride, PosOff, UvOff, TriCount;

            public MeshCache(Mesh m)
            {
                // гарантируем RAW‑доступ
                m.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

                // позиция и UV могут быть в разных стримах → берём их смещения
                PosOff = m.GetVertexAttributeOffset(VertexAttribute.Position);   // :contentReference[oaicite:5]{index=5}
                UvOff  = m.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
                int stream = m.GetVertexAttributeStream(VertexAttribute.Position);
                Stride = m.GetVertexBufferStride(stream);                        // :contentReference[oaicite:6]{index=6}

                VBuf     = m.GetVertexBuffer(stream);                            // :contentReference[oaicite:7]{index=7}
                IBuf     = m.GetIndexBuffer();
                TriCount = IBuf.count / 3;
            }
            public void Dispose()
            { VBuf?.Dispose(); IBuf?.Dispose(); }
        }

        private static readonly Dictionary<Mesh, MeshCache> Cache = new();

        // ---------- одноразовые буферы результата --------------------------
        private static ComputeBuffer _bestDist;   // uint   (asuint(float))
        private static ComputeBuffer _bestUV;     // float2
        private static readonly uint[]   DistInit = { 0x7F7FFFFF };  // float.MaxValue
        private static readonly Vector2[] UvInit  = { Vector2.zero };

        // ---------- публичное API ------------------------------------------
        public static Vector2 GetHitUV(in RaycastHit hit)
        {
            if (hit.collider == null) return Vector2.zero;

            // --- достаём Mesh ------------------------------------------------
            Mesh mesh;
            Transform tr;
            if (hit.collider.TryGetComponent(out MeshFilter mf))
            { mesh = mf.sharedMesh; tr = mf.transform; }
            else if (hit.collider.TryGetComponent(out SkinnedMeshRenderer sm))
            { mesh = new Mesh();  sm.BakeMesh(mesh);  tr = sm.transform; }
            else return Vector2.zero;

            if (mesh == null || mesh.vertexCount == 0) return Vector2.zero;

            if (!Cache.TryGetValue(mesh, out var c) || c.VBuf == null)
            { Cache[mesh] = c = new MeshCache(mesh); }                           // :contentReference[oaicite:8]{index=8}

            // --- буферы результата ------------------------------------------
            _bestDist ??= new ComputeBuffer(1, sizeof(uint),  ComputeBufferType.Raw);
            _bestUV   ??= new ComputeBuffer(1, sizeof(float)*2);
            _bestDist.SetData(DistInit);
            _bestUV  .SetData(UvInit);

            // --- параметры луча в локальных координатах ---------------------
            float3 ro = tr.InverseTransformPoint(hit.point + hit.normal * 0.01f);
            float3 rd = tr.InverseTransformDirection(-hit.normal).normalized;

            // --- заполняем шейдер -------------------------------------------
            Shader.SetBuffer(Kid, "_VBuffer",   c.VBuf);
            Shader.SetBuffer(Kid, "_Indices",   c.IBuf);
            Shader.SetInt   ("_Stride",         c.Stride);
            Shader.SetInt   ("_PosOffset",      c.PosOff);
            Shader.SetInt   ("_UVOffset",       c.UvOff);
            Shader.SetInt   ("_TriangleCount",  c.TriCount);
            Shader.SetVector("_RayOrigin",      (Vector3)ro);
            Shader.SetVector("_RayDirection",   (Vector3)rd);
            Shader.SetBuffer(Kid, "_BestDist",  _bestDist);
            Shader.SetBuffer(Kid, "_BestUV",    _bestUV);

            Shader.Dispatch(Kid, Mathf.CeilToInt(c.TriCount / 64f), 1, 1);

            // --- читаем результат -------------------------------------------
            uint[] distBits = new uint[1];
            _bestDist.GetData(distBits);
            if (distBits[0] == 0x7F7FFFFF) return Vector2.zero;

            Vector2[] uv = new Vector2[1];
            _bestUV.GetData(uv);
            return uv[0];
        }
    }
}
