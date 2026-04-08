using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    internal static class DoorVisualProxyUtility
    {
        private const float UniformScaleEpsilon = 0.0001f;

        public static bool ShouldUseVisualProxy(Transform sourceRoot)
        {
            return sourceRoot != null &&
                   HasVisibleMeshHierarchy(sourceRoot) &&
                   !IsUniformScale(sourceRoot.lossyScale);
        }

        public static Transform CreateVisualProxy(Transform sourceRoot, List<Renderer> hiddenRenderers)
        {
            if (sourceRoot == null)
                return null;

            GameObject proxyRoot = new GameObject($"{sourceRoot.name}_VisualProxy");
            proxyRoot.transform.SetPositionAndRotation(sourceRoot.position, sourceRoot.rotation);
            proxyRoot.transform.localScale = Vector3.one;

            Matrix4x4 rootInverseMatrix = proxyRoot.transform.worldToLocalMatrix;
            MeshFilter[] meshFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter sourceFilter = meshFilters[i];
                if (sourceFilter == null || sourceFilter.sharedMesh == null)
                    continue;

                MeshRenderer sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
                if (sourceRenderer == null)
                    continue;

                GameObject proxyChild = new GameObject(sourceFilter.name);
                proxyChild.transform.SetParent(proxyRoot.transform, false);

                MeshFilter proxyFilter = proxyChild.AddComponent<MeshFilter>();
                MeshRenderer proxyRenderer = proxyChild.AddComponent<MeshRenderer>();

                proxyFilter.sharedMesh = BakeMeshToProxySpace(
                    sourceFilter.sharedMesh,
                    rootInverseMatrix * sourceFilter.transform.localToWorldMatrix);
                proxyRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                proxyRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                proxyRenderer.receiveShadows = sourceRenderer.receiveShadows;
                proxyRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                proxyRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                proxyRenderer.motionVectorGenerationMode = sourceRenderer.motionVectorGenerationMode;
                proxyRenderer.allowOcclusionWhenDynamic = sourceRenderer.allowOcclusionWhenDynamic;

                hiddenRenderers?.Add(sourceRenderer);
                sourceRenderer.enabled = false;
            }

            return proxyRoot.transform;
        }

        public static void RestoreHiddenRenderers(List<Renderer> hiddenRenderers)
        {
            if (hiddenRenderers == null)
                return;

            for (int i = 0; i < hiddenRenderers.Count; i++)
            {
                if (hiddenRenderers[i] != null)
                    hiddenRenderers[i].enabled = true;
            }
        }

        private static bool HasVisibleMeshHierarchy(Transform sourceRoot)
        {
            MeshRenderer[] renderers = sourceRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].sharedMaterials != null && renderers[i].sharedMaterials.Length > 0)
                    return true;
            }

            return false;
        }

        private static bool IsUniformScale(Vector3 scale)
        {
            return Mathf.Abs(scale.x - scale.y) <= UniformScaleEpsilon &&
                   Mathf.Abs(scale.x - scale.z) <= UniformScaleEpsilon;
        }

        private static Mesh BakeMeshToProxySpace(Mesh sourceMesh, Matrix4x4 localToProxyMatrix)
        {
            Mesh bakedMesh = Object.Instantiate(sourceMesh);
            bakedMesh.name = $"{sourceMesh.name}_DoorProxy";

            Vector3[] vertices = sourceMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = localToProxyMatrix.MultiplyPoint3x4(vertices[i]);
            bakedMesh.vertices = vertices;

            Vector3[] normals = sourceMesh.normals;
            if (normals != null && normals.Length == vertices.Length)
            {
                Matrix4x4 normalMatrix = localToProxyMatrix.inverse.transpose;
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
                bakedMesh.normals = normals;
            }

            Vector4[] tangents = sourceMesh.tangents;
            if (tangents != null && tangents.Length == vertices.Length)
            {
                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector3 tangentDirection = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                    tangentDirection = localToProxyMatrix.MultiplyVector(tangentDirection).normalized;
                    tangents[i] = new Vector4(tangentDirection.x, tangentDirection.y, tangentDirection.z, tangents[i].w);
                }

                bakedMesh.tangents = tangents;
            }

            bakedMesh.RecalculateBounds();
            return bakedMesh;
        }
    }
}
