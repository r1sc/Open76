using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    static class Utils
    {
        public static Vector3 GetPlaneNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 AB = b - a;
            Vector3 AC = c - a;

            //Calculate the normal
            return Vector3.Normalize(Vector3.Cross(AB, AC));
        }
    }
}
