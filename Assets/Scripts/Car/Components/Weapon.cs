using Assets.Fileparsers;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.Car.Components
{
    public class Weapon
    {
        public bool RearFacing;
        public I76Sprite OnSprite;
        public I76Sprite OffSprite;
        public readonly Gdf Gdf;
        public AudioClip FireSound;
        public int Index;
        public int Ammo;
        public int Health; // TODO: Damage.
        public bool Firing;
        public float LastFireTime;
        public readonly Transform Transform;
        public readonly WaitForSeconds BurstWait;
        public readonly WaitForSeconds ReloadWait;
        public GameObject ProjectilePrefab;

        private static Mesh _cubeMesh;

        static Weapon()
        {
            CreateCubeMesh();
        }

        public Weapon(Gdf gdf, Transform transform)
        {
            Gdf = gdf;
            Health = gdf.Health;
            Ammo = gdf.AmmoCount;

            LoadProjectile();
            Transform = transform;

            if (gdf.FireAmount > 1)
            {
                BurstWait = new WaitForSeconds(gdf.FiringRate);
                ReloadWait = new WaitForSeconds(gdf.BurstRate);
            }
        }

        private void LoadProjectile()
        {
            CacheManager.GeoMeshCacheEntry meshCacheEntry = CacheManager.Instance.ImportMesh(Gdf.Projectile.Name + ".geo", null, 0);

            GameObject obj = Object.Instantiate(CacheManager.Instance.ProjectilePrefab);
            obj.SetActive(false);
            obj.gameObject.name = meshCacheEntry.GeoMesh.Name;
            
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                if (meshCacheEntry.Mesh != null && meshCacheEntry.Mesh.vertexCount > 0)
                {
                    meshFilter.sharedMesh = meshCacheEntry.Mesh;
                }
                else
                {
                    meshFilter.mesh = _cubeMesh;
                }

                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                if (meshCacheEntry.Materials != null && meshCacheEntry.Materials.Length > 0)
                {
                    renderer.materials = meshCacheEntry.Materials;
                }
                else
                {
                    renderer.material = CacheManager.Instance.GetTextureMaterial(Gdf.Projectile.Name + ".map", true);
                }
            }

            MeshCollider collider = obj.GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = meshCacheEntry.Mesh;
            }

            ProjectilePrefab = obj.gameObject;
        }

        private static void CreateCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.Clear();
            
            Vector3 p0 = new Vector3(-.5f, -.5f, .5f);
            Vector3 p1 = new Vector3(.5f, -.5f, .5f);
            Vector3 p2 = new Vector3(.5f, -.5f, -.5f);
            Vector3 p3 = new Vector3(-.5f, -.5f, -.5f);

            Vector3 p4 = new Vector3(-.5f, .5f, .5f);
            Vector3 p5 = new Vector3(.5f, .5f, .5f);
            Vector3 p6 = new Vector3(.5f, .5f, -.5f);
            Vector3 p7 = new Vector3(-.5f, .5f, -.5f);

            Vector3[] vertices = 
            {
	            p0, p1, p2, p3,
	            p7, p4, p0, p3,
	            p4, p5, p1, p0,
	            p6, p7, p3, p2,
	            p5, p6, p2, p1,
	            p7, p6, p5, p4
            };

            Vector3 up = Vector3.up;
            Vector3 down = Vector3.down;
            Vector3 front = Vector3.forward;
            Vector3 back = Vector3.back;
            Vector3 left = Vector3.left;
            Vector3 right = Vector3.right;

            Vector3[] normals = 
            {
	            down, down, down, down,
	            left, left, left, left,
	            front, front, front, front,
	            back, back, back, back,
	            right, right, right, right,
	            up, up, up, up
            };
            
            Vector2 uv1 = new Vector2(0f, 0f);
            Vector2 uv2 = new Vector2(1f, 0f);
            Vector2 uv3 = new Vector2(0f, 1f);
            Vector2 uv4 = new Vector2(1f, 1f);

            Vector2[] uvs =
            {
                uv4, uv3, uv1, uv2,
                uv4, uv3, uv1, uv2,
                uv4, uv3, uv1, uv2,
                uv4, uv3, uv1, uv2,
                uv4, uv3, uv1, uv2,
                uv4, uv3, uv1, uv2
            };

            int[] triangles = 
            {
	            3, 1, 0,
                3, 2, 1,
	            3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
                3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
	            3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
                3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
	            3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
                3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
	            3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
                3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
	            3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
                3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5
            };

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            _cubeMesh = mesh;
        }
    }
}
