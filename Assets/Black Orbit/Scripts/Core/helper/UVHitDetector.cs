namespace Black_Orbit.Scripts.Core.helper
{
    using UnityEngine;
    using System.Collections.Generic;

    public static class UVHitDetector
    {
        private static readonly Dictionary<Object, MeshData> _meshDataCache = new Dictionary<Object, MeshData>();
        private static Mesh _bakedMeshCache;

        public static Vector2 GetHitUV(RaycastHit hit)
        {
            if (hit.textureCoord != Vector2.zero)
                return hit.textureCoord;

            Collider collider = hit.collider;

            if (collider.GetComponentInParent<SkinnedMeshRenderer>() != null)
                return GetSkinnedMeshUV(hit);

            if (collider is MeshCollider)
                return GetMeshColliderUV(hit);

            if (collider != null)
                return GetPrimitiveColliderUV(hit);

            if (collider.GetComponent<MeshRenderer>() != null)
                return GetMeshRendererUV(hit);

            return Vector2.zero;
        }

        private static Vector2 GetMeshRendererUV(RaycastHit hit)
        {
            MeshRenderer renderer = hit.collider.GetComponent<MeshRenderer>();
            if (renderer == null)
                return Vector2.zero;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return Vector2.zero;

            if (!_meshDataCache.TryGetValue(renderer, out MeshData meshData))
            {
                meshData = new MeshData(filter.sharedMesh);
                _meshDataCache[renderer] = meshData;
            }

            Ray ray = new Ray(hit.point + hit.normal * 0.01f, -hit.normal);
            return RaycastMeshForUV(ray, renderer.transform, meshData);
        }

        private static Vector2 GetSkinnedMeshUV(RaycastHit hit)
        {
            SkinnedMeshRenderer skinnedMesh = hit.collider.GetComponentInParent<SkinnedMeshRenderer>();
            if (skinnedMesh == null) return Vector2.zero;

            if (!_meshDataCache.TryGetValue(hit.collider, out MeshData meshData))
            {
                if (_bakedMeshCache == null) _bakedMeshCache = new Mesh();
                skinnedMesh.BakeMesh(_bakedMeshCache);
                meshData = new MeshData(_bakedMeshCache);
                _meshDataCache[hit.collider] = meshData;
            }

            return CalculateUVFromBarycentric(meshData, hit);
        }

        private static Vector2 GetMeshColliderUV(RaycastHit hit)
        {
            MeshCollider meshCollider = (MeshCollider)hit.collider;
            if (meshCollider.sharedMesh == null) return Vector2.zero;

            if (!_meshDataCache.TryGetValue(hit.collider, out MeshData meshData))
            {
                meshData = new MeshData(meshCollider.sharedMesh);
                _meshDataCache[hit.collider] = meshData;
            }

            return CalculateUVFromBarycentric(meshData, hit);
        }

        private static Vector2 GetPrimitiveColliderUV(RaycastHit hit)
        {
            Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);

            switch (hit.collider)
            {
                case BoxCollider box:
                {
                    // Получаем локальные координаты точки попадания
                    Vector3 localPoint = box.transform.InverseTransformPoint(hit.point) - box.center;
                    Vector3 localNormal = box.transform.InverseTransformDirection(hit.normal).normalized;
                    Vector3 scaledSize = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;

                    // Определяем главную ось нормали
                    float absX = Mathf.Abs(localNormal.x);
                    float absY = Mathf.Abs(localNormal.y);
                    float absZ = Mathf.Abs(localNormal.z);

                    Vector2 uv = Vector2.zero;
                    bool isXMax = absX >= absY && absX >= absZ;
                    bool isYMax = absY >= absX && absY >= absZ;

                    if (isXMax)
                    {
                        // Боковые грани (лево/право)
                        uv.x = Mathf.InverseLerp(-scaledSize.z, scaledSize.z, localPoint.z);
                        uv.y = Mathf.InverseLerp(-scaledSize.y, scaledSize.y, localPoint.y);
                        if (localNormal.x > 0) uv.x = 1 - uv.x; // Инверсия для правой грани
                    }
                    else if (isYMax)
                    {
                        // Верх/низ
                        uv.x = Mathf.InverseLerp(-scaledSize.x, scaledSize.x, localPoint.x);
                        uv.y = Mathf.InverseLerp(-scaledSize.z, scaledSize.z, localPoint.z);
                        if (localNormal.y < 0) uv.y = 1 - uv.y; // Инверсия для нижней грани
                    }
                    else
                    {
                        // Перед/зад
                        uv.x = Mathf.InverseLerp(-scaledSize.x, scaledSize.x, localPoint.x);
                        uv.y = Mathf.InverseLerp(-scaledSize.y, scaledSize.y, localPoint.y);
                        if (localNormal.z < 0) uv.x = 1 - uv.x; // Инверсия для задней грани
                    }

                    // Коррекция вертикали для всех граней
                    uv.y = 1 - uv.y; // Инвертируем V-координату для всех случаев

                    return uv;
                }
                case SphereCollider sphere:
                {
                    Vector3 localDir = localHitPoint.normalized;
                    float u = 0.5f + Mathf.Atan2(localDir.z, localDir.x) / (2 * Mathf.PI);
                    float v = 0.5f - Mathf.Asin(localDir.y) / Mathf.PI;
                    return new Vector2(u, v);
                }
                case CapsuleCollider capsule:
                {
                    float u = Mathf.InverseLerp(-capsule.radius, capsule.radius, localHitPoint.x);
                    float v = Mathf.InverseLerp(-capsule.height * 0.5f, capsule.height * 0.5f, localHitPoint.y);
                    return new Vector2(u, v);
                }
                default:
                    return Vector2.zero;
            }

        }
        
        private static float Remap(float value, float min, float max)
        {
            return Mathf.InverseLerp(min, max, value);
        }
        
        private static Vector2 CalculateUVFromBarycentric(MeshData meshData, RaycastHit hit)
        {
            if (meshData.UVs.Length == 0) return Vector2.zero;

            int triangleIndex = hit.triangleIndex;
            Vector3 barycentric = hit.barycentricCoordinate;

            int index0 = meshData.Triangles[triangleIndex * 3 + 0];
            int index1 = meshData.Triangles[triangleIndex * 3 + 1];
            int index2 = meshData.Triangles[triangleIndex * 3 + 2];

            Vector2 uv0 = meshData.UVs[index0];
            Vector2 uv1 = meshData.UVs[index1];
            Vector2 uv2 = meshData.UVs[index2];

            return barycentric.x * uv0 + barycentric.y * uv1 + barycentric.z * uv2;
        }

        private static Vector2 RaycastMeshForUV(Ray ray, Transform transform, MeshData meshData)
        {
            float closestDist = float.MaxValue;
            Vector2 resultUV = Vector2.zero;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;

            for (int i = 0; i < meshData.Triangles.Length; i += 3)
            {
                Vector3 p0 = localToWorld.MultiplyPoint3x4(meshData.Vertices[meshData.Triangles[i]]);
                Vector3 p1 = localToWorld.MultiplyPoint3x4(meshData.Vertices[meshData.Triangles[i + 1]]);
                Vector3 p2 = localToWorld.MultiplyPoint3x4(meshData.Vertices[meshData.Triangles[i + 2]]);

                if (RayIntersectsTriangle(ray, p0, p1, p2, out Vector3 hitPoint, out Vector3 barycentric))
                {
                    float dist = (ray.origin - hitPoint).sqrMagnitude;
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        Vector2 uv0 = meshData.UVs[meshData.Triangles[i]];
                        Vector2 uv1 = meshData.UVs[meshData.Triangles[i + 1]];
                        Vector2 uv2 = meshData.UVs[meshData.Triangles[i + 2]];
                        resultUV = barycentric.x * uv0 + barycentric.y * uv1 + barycentric.z * uv2;
                    }
                }
            }

            return resultUV;
        }

        private static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hitPoint, out Vector3 barycentric)
        {
            hitPoint = Vector3.zero;
            barycentric = Vector3.zero;

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -Mathf.Epsilon && a < Mathf.Epsilon)
                return false;

            float f = 1.0f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.direction, q);

            if (v < 0.0f || u + v > 1.0f)
                return false;

            float t = f * Vector3.Dot(edge2, q);

            if (t > Mathf.Epsilon)
            {
                hitPoint = ray.origin + ray.direction * t;
                barycentric = new Vector3(1 - u - v, u, v);
                return true;
            }

            return false;
        }

        private class MeshData
        {
            public int[] Triangles { get; }
            public Vector2[] UVs { get; }
            public Vector3[] Vertices { get; }

            public MeshData(Mesh mesh)
            {
                Triangles = mesh.triangles;
                UVs = mesh.uv;
                Vertices = mesh.vertices;
            }
        }
    }
}
