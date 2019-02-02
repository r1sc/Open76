using UnityEngine;

namespace Assets.Scripts.I76Types
{
    public static class I76VectorExtensions
    {
        public static Vector3 ToVector3(this I76Vector3 vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }

        public static Vector4 ToVector4(this I76Vector4 vector4)
        {
            return new Vector4(vector4.x, vector4.y, vector4.z, vector4.w);
        }
    }

    public class I76Vector4 : I76Vector3
    {
        public float w;

        public I76Vector4(float x, float y, float z, float w) : base(x, y, z)
        {
            this.w = w;
        }
    }

    public class I76Vector3
    {
        public float x, y, z;

        public I76Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return x + ", " + y + ", " + z;
        }
    }

    public class I76Vector2
    {
        public float x, y;

        public I76Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
