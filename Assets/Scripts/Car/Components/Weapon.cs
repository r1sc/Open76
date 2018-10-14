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
        public int Health; // TODO: Handle damage to the weapon.
        public int WeaponGroupOffset;
        public bool Firing;
        public float LastFireTime;
        public readonly Transform Transform;
        public readonly WaitForSeconds BurstWait;
        public readonly WaitForSeconds ReloadWait;
        public GameObject ProjectilePrefab;

        private static Mesh _quadMesh;

        static Weapon()
        {
            CreateQuadMesh();
        }

        public Weapon(Gdf gdf, Transform transform)
        {
            Gdf = gdf;
            Health = gdf.Health;
            Ammo = gdf.AmmoCount;
            WeaponGroupOffset = gdf.WeaponGroup;

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
                    meshFilter.mesh = _quadMesh;
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

        private static void CreateQuadMesh()
        {
            const float width = 0.05f;
            const float length = 0.25f;

            Mesh mesh = new Mesh();

            Vector3[] vertices =
            {
                new Vector3(-width, 0f, -length),
                new Vector3(-width, 0f, length),
                new Vector3(width, 0f, -length),
                new Vector3(width, 0f, length)
            };

            Vector2[] uvs =
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };

            int[] triangles =
            {
                0, 1, 2,
                1, 3, 2,
                2, 1, 0,
                2, 3, 1
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            _quadMesh = mesh;
        }
    }
}
