using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.I76Types
{
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
