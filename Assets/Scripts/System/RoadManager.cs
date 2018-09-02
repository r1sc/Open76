using System;
using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.System
{
    public class RoadManager
    {
        public readonly List<Road> Roads;
        private readonly Transform _worldTransform;
        private List<Road> _pointBuffer;

        private static RoadManager _instance;

        public static RoadManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RoadManager();
                }

                return _instance;
            }
        }

        public Road[] GetRoadsAroundPoint(Vector3 worldPoint)
        {
            Bounds pointArea = new Bounds(worldPoint, new Vector3(100f, 100f, 100f));
            _pointBuffer.Clear();

            int roadCount = Roads.Count;
            for (int i = 0; i < roadCount; ++i)
            {
                Road road = Roads[i];
                if (pointArea.Intersects(road.Bounds))
                {
                    _pointBuffer.Add(road);
                }
            }

            return _pointBuffer.ToArray();
        }

        public void CreateRoadObject(MsnMissionParser.Road parsedRoad, Vector2 worldMiddle)
        {
            var roadGo = new GameObject("Road");
            roadGo.transform.parent = _worldTransform;
            var meshCollider = roadGo.AddComponent<MeshCollider>();
            var meshFilter = roadGo.AddComponent<MeshFilter>();
            var meshRenderer = roadGo.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            string roadTextureFilename;
            switch (parsedRoad.SegmentType)
            {
                case MsnMissionParser.RoadSegmentType.PavedHighway:
                    roadTextureFilename = "r2ayr_51";
                    break;
                case MsnMissionParser.RoadSegmentType.DirtTrack:
                    roadTextureFilename = "r2dnr_37";
                    break;
                case MsnMissionParser.RoadSegmentType.RiverBed:
                    roadTextureFilename = "r2wnr_39";
                    break;
                case MsnMissionParser.RoadSegmentType.FourLaneHighway:
                    roadTextureFilename = "r2ayr_51";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CacheManager cacheManager = CacheManager.Instance;
            Material roadMaterial = cacheManager.GetTextureMaterial(roadTextureFilename, false);
            meshRenderer.material = roadMaterial;

            var mesh = new Mesh();
            Vector3[] vertices = new Vector3[parsedRoad.RoadSegments.Length * 2];
            Vector2[] uvs = new Vector2[parsedRoad.RoadSegments.Length * 2];
            Road roadEntity = roadGo.AddComponent<Road>();
            Vector2[] midPoints = new Vector2[parsedRoad.RoadSegments.Length];
            roadEntity.Segments = midPoints;

            var uvIdx = 0;
            var vertexIndex = 0;
            foreach (var roadSegment in parsedRoad.RoadSegments)
            {
                vertices[vertexIndex] = roadSegment.Left;
                vertices[vertexIndex + 1] = roadSegment.Right;

                uvs[vertexIndex] = new Vector2(0, uvIdx);
                uvs[vertexIndex + 1] = new Vector2(1, uvIdx);

                Vector3 midPoint = (roadSegment.Left + roadSegment.Right) * 0.5f;
                roadEntity.Segments[uvIdx] = new Vector2(midPoint.x - worldMiddle.x, midPoint.z - worldMiddle.y);

                uvIdx += 1;
                vertexIndex += 2;
            }

            int[] indices = new int[(vertices.Length - 2) * 3];
            var idx = 0;
            var indexCount = 0;
            for (int i = 0; i < indices.Length; i += 6)
            {
                indices[i] = idx + 2;
                indices[i + 1] = idx + 1;
                indices[i + 2] = idx;

                indices[i + 3] = idx + 2;
                indices[i + 4] = idx + 3;
                indices[i + 5] = idx + 1;
                idx += 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            roadEntity.UpdateBounds(worldMiddle);
            Roads.Add(roadEntity);
        }

        private RoadManager()
        {
            Roads = new List<Road>();
            _worldTransform = GameObject.Find("World").transform;
            _pointBuffer = new List<Road>();
        }
    }

    public class Road : MonoBehaviour
    {
        public Vector2[] Segments;
        public Bounds Bounds { get; private set; }

        public void UpdateBounds(Vector2 worldMiddle)
        {
            Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;
            bounds.center -= new Vector3(worldMiddle.x, 0f, worldMiddle.y);
            Bounds = bounds;
        }
    }
}
