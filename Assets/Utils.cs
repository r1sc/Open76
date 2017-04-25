using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

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


        public static T RandomElement<T>(T[] array)
        {
            return array[Random.Range(0, array.Length)];
        }

        public static T RandomElement<T>(IList<T> array)
        {
            return array[Random.Range(0, array.Count)];
        }
    }
}
