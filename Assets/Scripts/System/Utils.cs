using System.Collections.Generic;
using UnityEngine;

namespace Assets.System
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

        public static bool EndsWithFast(this string text, string subText)
        {
            int length = text.Length;
            int subLength = subText.Length;

            int offset = length - subLength;
            int lengthMin1 = length - 1;
            for (int i = offset; i < lengthMin1; ++i)
            {
                if (text[i] != subText[i - offset])
                {
                    return false;
                }
            }

            return true;
        }

        public static T RandomElement<T>(T[] array)
        {
            return array[Random.Range(0, array.Length)];
        }

        public static T RandomElement<T>(IList<T> array)
        {
            return array[Random.Range(0, array.Count)];
        }

        public static float GroundHeightAtPoint(float x, float z)
        {
            Vector3 rayPoint;
            rayPoint.x = x;
            rayPoint.y = 1000f;
            rayPoint.z = z;

            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(rayPoint, Vector3.down), out hitInfo, LayerMask.GetMask("Terrain")))
            {
                return hitInfo.point.y;
            }

            Debug.Log("Failed to raycast against terrain.");
            return 0.0f;
        }
    }
}
