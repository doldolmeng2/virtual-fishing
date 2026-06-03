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
        private readonly List<Vector2> cachedOutline = new();
        private Vector2 cachedCenter;

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

            CacheOutline(outline);

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

        public bool TryGetClosestInsetShorePoint(
            Vector3 worldReferencePosition,
            Vector3 worldStartPosition,
            float inset,
            out Vector3 worldShorePoint)
        {
            List<Vector2> outline = GetCachedOutline();
            if (outline.Count < 3)
            {
                worldShorePoint = default;
                return false;
            }

            Vector2 localReference = WorldToLocal2D(worldReferencePosition);
            Vector2 localStart = WorldToLocal2D(worldStartPosition);
            Vector2 closest = FindClosestPointOnOutline(outline, localReference);
            Vector2 inward = cachedCenter - closest;
            if (inward.sqrMagnitude < 0.0001f)
            {
                inward = localStart - closest;
            }

            if (inward.sqrMagnitude < 0.0001f)
            {
                inward = Vector2.up;
            }

            Vector2 localPoint = closest + inward.normalized * Mathf.Max(0f, inset);
            if (!ContainsPoint(localPoint, outline))
            {
                localPoint = Vector2.Lerp(closest, cachedCenter, 0.08f);
            }

            worldShorePoint = Local2DToWorld(localPoint, worldStartPosition.y);
            return true;
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            List<Vector2> outline = GetCachedOutline();
            return outline.Count >= 3 && ContainsPoint(WorldToLocal2D(worldPoint), outline);
        }

        public Vector3 ClampWorldPointInside(Vector3 worldPoint, float inset)
        {
            List<Vector2> outline = GetCachedOutline();
            if (outline.Count < 3)
            {
                return worldPoint;
            }

            Vector2 localPoint = WorldToLocal2D(worldPoint);
            if (ContainsPoint(localPoint, outline))
            {
                return worldPoint;
            }

            Vector2 closest = FindClosestPointOnOutline(outline, localPoint);
            Vector2 inward = cachedCenter - closest;
            if (inward.sqrMagnitude < 0.0001f)
            {
                inward = cachedCenter - localPoint;
            }

            if (inward.sqrMagnitude < 0.0001f)
            {
                inward = Vector2.up;
            }

            Vector2 clamped = closest + inward.normalized * Mathf.Max(0.02f, inset);
            return Local2DToWorld(clamped, worldPoint.y);
        }

        private void EnsureDefaultShape()
        {
            if (controlPoints != null && controlPoints.Length >= 4)
            {
                return;
            }

            controlPoints = CreateDefaultControlPoints();
        }

        private List<Vector2> GetCachedOutline()
        {
            if (cachedOutline.Count >= 3)
            {
                return cachedOutline;
            }

            EnsureDefaultShape();
            CacheOutline(BuildSmoothOutline(controlPoints, smoothingIterations));
            return cachedOutline;
        }

        private void CacheOutline(IReadOnlyList<Vector2> outline)
        {
            cachedOutline.Clear();
            for (int i = 0; i < outline.Count; i++)
            {
                cachedOutline.Add(outline[i]);
            }

            cachedCenter = cachedOutline.Count >= 3
                ? CalculateCenter(cachedOutline)
                : Vector2.zero;
        }

        private Vector2 WorldToLocal2D(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return new Vector2(local.x, local.z);
        }

        private Vector3 Local2DToWorld(Vector2 localPoint, float worldY)
        {
            Vector3 world = transform.TransformPoint(new Vector3(localPoint.x, 0f, localPoint.y));
            world.y = worldY;
            return world;
        }

        private static Vector2 FindClosestPointOnOutline(IReadOnlyList<Vector2> outline, Vector2 point)
        {
            Vector2 bestPoint = outline[0];
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 start = outline[i];
                Vector2 end = outline[(i + 1) % outline.Count];
                Vector2 candidate = ClosestPointOnSegment(start, end, point);
                float distance = (candidate - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = candidate;
                }
            }

            return bestPoint;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 start, Vector2 end, Vector2 point)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f)
            {
                return start;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return start + segment * t;
        }

        private static bool ContainsPoint(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 current = polygon[i];
                Vector2 previous = polygon[j];
                bool crosses = (current.y > point.y) != (previous.y > point.y);
                if (crosses)
                {
                    float xAtY = (previous.x - current.x) * (point.y - current.y) / (previous.y - current.y) + current.x;
                    if (point.x < xAtY)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
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
