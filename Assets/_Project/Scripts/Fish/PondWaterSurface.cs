using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Core.Fish
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class PondWaterSurface : MonoBehaviour
    {
        [SerializeField, Min(1)] private int smoothingIterations = 3;
        [SerializeField, Min(0.1f)] private float uvScale = 12f;
        [SerializeField] private Vector2[] controlPoints = CreateDefaultControlPoints();

        private Mesh generatedMesh;

        private void Reset()
        {
            controlPoints = CreateDefaultControlPoints();
            RebuildMesh();
        }

        private void OnEnable()
        {
            EnsureDefaultShape();
            RebuildMesh();
        }

        private void OnValidate()
        {
            smoothingIterations = Mathf.Max(1, smoothingIterations);
            uvScale = Mathf.Max(0.1f, uvScale);
            EnsureDefaultShape();
            RebuildMesh();
        }

        private void OnDestroy()
        {
            if (generatedMesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }
        }

        [ContextMenu("Rebuild Pond Mesh")]
        public void RebuildMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshCollider meshCollider = GetComponent<MeshCollider>();

            if (meshFilter == null || meshCollider == null)
            {
                return;
            }

            List<Vector2> outline = BuildSmoothOutline(controlPoints, smoothingIterations);
            if (outline.Count < 3)
            {
                return;
            }

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh
                {
                    name = "Pond_Water_RuntimeMesh"
                };
                generatedMesh.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                generatedMesh.Clear();
            }

            Vector2 center2D = CalculateCenter(outline);
            Vector3[] vertices = new Vector3[outline.Count + 1];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[outline.Count * 3];

            vertices[0] = new Vector3(center2D.x, 0f, center2D.y);
            uvs[0] = center2D / uvScale;

            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 point = outline[i];
                vertices[i + 1] = new Vector3(point.x, 0f, point.y);
                uvs[i + 1] = point / uvScale;

                int triangleStart = i * 3;
                int nextIndex = (i + 1) % outline.Count;
                triangles[triangleStart] = 0;
                triangles[triangleStart + 1] = i + 1;
                triangles[triangleStart + 2] = nextIndex + 1;
            }

            generatedMesh.vertices = vertices;
            generatedMesh.uv = uvs;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            generatedMesh.RecalculateTangents();

            meshFilter.sharedMesh = generatedMesh;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = generatedMesh;
        }

        private void EnsureDefaultShape()
        {
            if (controlPoints != null && controlPoints.Length >= 4)
            {
                return;
            }

            controlPoints = CreateDefaultControlPoints();
        }

        private static Vector2 CalculateCenter(IReadOnlyList<Vector2> points)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                sum += points[i];
            }

            return sum / points.Count;
        }

        private static List<Vector2> BuildSmoothOutline(IReadOnlyList<Vector2> sourcePoints, int smoothingIterations)
        {
            List<Vector2> outline = new List<Vector2>(sourcePoints.Count);
            if (sourcePoints.Count < 4)
            {
                return outline;
            }

            for (int i = 0; i < sourcePoints.Count; i++)
            {
                outline.Add(sourcePoints[i]);
            }

            for (int iteration = 0; iteration < smoothingIterations; iteration++)
            {
                List<Vector2> smoothed = new List<Vector2>(outline.Count * 2);
                for (int i = 0; i < outline.Count; i++)
                {
                    Vector2 current = outline[i];
                    Vector2 next = outline[(i + 1) % outline.Count];

                    Vector2 q = Vector2.Lerp(current, next, 0.25f);
                    Vector2 r = Vector2.Lerp(current, next, 0.75f);
                    smoothed.Add(q);
                    smoothed.Add(r);
                }

                outline = smoothed;
            }

            return outline;
        }

        private static Vector2[] CreateDefaultControlPoints()
        {
            return new[]
            {
                new Vector2(-33f, -3f),
                new Vector2(-28f, 4f),
                new Vector2(-20f, 9f),
                new Vector2(-8f, 12f),
                new Vector2(5f, 12.5f),
                new Vector2(17f, 10f),
                new Vector2(27f, 5f),
                new Vector2(33f, 0f),
                new Vector2(30f, -5.5f),
                new Vector2(20f, -10f),
                new Vector2(8f, -13f),
                new Vector2(-4f, -14f),
                new Vector2(-16f, -13f),
                new Vector2(-27f, -9f),
                new Vector2(-33f, -5f)
            };
        }
    }
}
