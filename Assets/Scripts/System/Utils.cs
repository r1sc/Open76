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

        public static float GroundHeightAtPoint(Vector3 position)
        {
            Vector3 rayPoint = position;
            rayPoint.y += 1000f;

            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(rayPoint, Vector3.down), out hitInfo, LayerMask.GetMask("Terrain")))
            {
                return hitInfo.point.y;
            }

            Debug.Log("Failed to raycast against terrain.");
            return position.y;
        }

        // Find the closest point on a line to a point.
        public static Vector3 GetClosestPointOnLineSegment(Vector3 lineStartPoint, Vector3 lineEndPoint, Vector3 origin)
        {
            Vector3 ap = new Vector3(origin.x - lineStartPoint.x, origin.y - lineStartPoint.y, origin.z - lineStartPoint.z); // Vector from A to P   
            Vector3 ab = new Vector3(lineEndPoint.x - lineStartPoint.x, lineEndPoint.y - lineStartPoint.y, lineEndPoint.z - lineStartPoint.z); // Vector from A to B  

            float magnitudeAb = ab.sqrMagnitude;
            if (magnitudeAb < float.Epsilon)
            {
                return lineStartPoint;
            }

            float distance = (ap.x * ab.x + ap.y * ab.y + ap.z * ab.z) / magnitudeAb; // The normalized "distance" from A to the closest point  

            //Check if origin projection is over vectorAB 
            if (distance < 0f)
            {
                return lineStartPoint;
            }

            if (distance > 1f)
            {
                return lineEndPoint;
            }

            return new Vector3(lineStartPoint.x + ab.x * distance, lineStartPoint.y + ab.y * distance, lineStartPoint.z + ab.z * distance);
        }
    }
}
