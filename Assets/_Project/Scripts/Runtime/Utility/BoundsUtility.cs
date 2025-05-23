using UnityEngine;

namespace Beakstorm.Utility
{
    public static class BoundsUtility
    {
        private static Vector3[] _corners = new Vector3[8];

        private static readonly Vector3[] CornerOffsets = new[]
        {
            new Vector3(-1, -1, -1),
            new Vector3(+1, -1, -1),
            new Vector3(-1, +1, -1),
            new Vector3(+1, +1, -1),
            new Vector3(-1, -1, +1),
            new Vector3(+1, -1, +1),
            new Vector3(-1, +1, +1),
            new Vector3(+1, +1, +1),
        };

        public static Rect BoundsInScreenSpace(Bounds bounds, Camera camera)
        {
            _corners ??= new Vector3[8];

            float minX = Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float maxY = Mathf.NegativeInfinity;

            for (int i = 0; i < 8; i++)
            {
                _corners[i] = camera.WorldToScreenPoint(GetBoundsCorner(bounds, i));

                minX = Mathf.Min(minX, _corners[i].x);
                minY = Mathf.Min(minY, _corners[i].y);
                maxX = Mathf.Max(maxX, _corners[i].x);
                maxY = Mathf.Max(maxY, _corners[i].y);
            }
            
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        public static Rect SphereInScreenSpace(Vector3 center, float radius, Camera camera)
        {
            float minX = Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float maxY = Mathf.NegativeInfinity;
            
            var transform = camera.transform;
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = Vector3.zero;
                if (i % 2 == 0) offset += right;
                else offset += up;
                if (i / 2 > 0) offset *= -1;
                
                _corners[i] = camera.WorldToScreenPoint(center + offset * radius);

                minX = Mathf.Min(minX, _corners[i].x);
                minY = Mathf.Min(minY, _corners[i].y);
                maxX = Mathf.Max(maxX, _corners[i].x);
                maxY = Mathf.Max(maxY, _corners[i].y);
            }
            
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
        
        private static Vector3 GetBoundsCorner(Bounds bounds, int index)
        {
            index %= 8;
            return new Vector3(
                bounds.center.x + bounds.extents.x * CornerOffsets[index].x,
                bounds.center.y + bounds.extents.y * CornerOffsets[index].y,
                bounds.center.z + bounds.extents.z * CornerOffsets[index].z
            );
        }
    }
}
