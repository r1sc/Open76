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
        private Transform _worldTransform;

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

        public List<Road> GetRoadsAroundPoint(Vector3 worldPoint)
        {
            Bounds pointArea = new Bounds(worldPoint, new Vector3(100f, 100f, 100f));
            List<Road> closestRoads = new List<Road>();

            int roadCount = Roads.Count;
            for (int i = 0; i < roadCount; ++i)
            {
                Road road = Roads[i];
                if (pointArea.Intersects(road.Bounds))
                {
                    closestRoads.Add(road);
                }
            }

            return closestRoads;
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
            var roadMaterial = CacheManager.Instance.GetTextureMaterial(roadTextureFilename, false);
            meshRenderer.material = roadMaterial;

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            Road roadEntity = roadGo.AddComponent<Road>();
            Vector3[] midPoints = new Vector3[parsedRoad.RoadSegments.Length];
            roadEntity.Segments = midPoints;

            var uvIdx = 0;
            foreach (var roadSegment in parsedRoad.RoadSegments)
            {
                vertices.Add(roadSegment.Left);
                vertices.Add(roadSegment.Right);

                uvs.Add(new Vector2(0, uvIdx));
                uvs.Add(new Vector2(1, uvIdx));

                Vector3 midPoint = (roadSegment.Left + roadSegment.Right) * 0.5f;
                roadEntity.Segments[uvIdx] = midPoint - new Vector3(worldMiddle.x, 0f, worldMiddle.y);

                uvIdx += 1;
            }

            var indices = new List<int>();
            var idx = 0;
            for (int i = 0; i < (vertices.Count - 2) / 2; i++)
            {
                indices.Add(idx + 2);
                indices.Add(idx + 1);
                indices.Add(idx);

                indices.Add(idx + 2);
                indices.Add(idx + 3);
                indices.Add(idx + 1);
                idx += 2;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.uv = uvs.ToArray();
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
        }
    }

    public class Road : MonoBehaviour
    {
        public Vector3[] Segments { get; set; }
        public Bounds Bounds { get; private set; }

        public void UpdateBounds(Vector2 worldMiddle)
        {
            Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;
            bounds.center -= new Vector3(worldMiddle.x, 0f, worldMiddle.y);
            Bounds = bounds;
        }
    }
}
