using UnityEditor;
using UnityEngine;


namespace DynaMak.Volumes.Editor
{
    [CustomEditor(typeof(VolumeComponent), true)]
    public class VolumeComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            VolumeComponent volumeComponent = target as VolumeComponent;
            if(volumeComponent is null) return;
            
        }


        #region Gizmos

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void DrawGizmosNotSelected(VolumeComponent volume, GizmoType gizmoType)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 1f);
            Gizmos.DrawWireCube(volume.VolumeCenter, volume.VolumeBounds);
        }
        

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(VolumeComponent volume, GizmoType gizmoType)
        {
            if(volume.GetVolumeTexture() is null) return;
            
            Gizmos.color = new Color(0f, 0f, 1f, 1f);
            Gizmos.DrawWireCube(volume.VolumeCenter, volume.VolumeBounds);

            Gizmos.color = Color.green;
            GizmosDrawResolution(volume.VolumeCenter, volume.VolumeBounds, volume.VolumeResolution);
            //GizmosDrawGrid(volume.VolumeCenter, volume.VolumeBounds.x * Vector3.right, volume.VolumeBounds.z * Vector3.forward, 
            //   new Vector2Int(volume.VolumeResolution.x, volume.VolumeResolution.z));
        }



        static void GizmosDrawGrid(Vector3 center, Vector3 right, Vector3 up, Vector2Int resolution)
        {
            float rightStep = right.magnitude / resolution.x;
            float upStep = up.magnitude / resolution.y;

            Vector3 cellSize = right.normalized * rightStep + up.normalized * upStep;
            
            Vector3 startPos = center - right * 0.5f - up * 0.5f;
            
            for (int x = 0; x <= resolution.x; x++)
            {
                for (int y = 0; y <= resolution.y; y++)
                {
                    Vector3 offset = right.normalized * rightStep * x + up.normalized * upStep * y + cellSize * 0.5f;
                    Gizmos.DrawWireCube(startPos + offset, cellSize);
                }
            }
        }
        
        static void GizmosDrawResolution(Vector3 center, Vector3 size, Vector3Int resolution)
        {
            float xStep = size.x / resolution.x;
            float yStep = size.y / resolution.y;
            float zStep = size.z / resolution.z;

            Vector3 cellSize = new Vector3(0, size.y, size.z);
            Vector3 startPos = center - Vector3.right * size.x * 0.5f;
            
            for (int x = 0; x <= resolution.x; x++)
            {
                Vector3 offset = Vector3.right * xStep * x;
                Gizmos.DrawWireCube(startPos + offset, cellSize);
            }
            
            cellSize = new Vector3(size.x, 0, size.z);
            startPos = center - Vector3.up * size.y * 0.5f;
            
            for (int y = 0; y <= resolution.y; y++)
            {
                Vector3 offset = Vector3.up * yStep * y;
                Gizmos.DrawWireCube(startPos + offset, cellSize);
            }
            
            cellSize = new Vector3(size.x, size.y, 0);
            startPos = center - Vector3.forward * size.z * 0.5f;
            
            for (int z = 0; z <= resolution.z; z++)
            {
                Vector3 offset = Vector3.forward * zStep * z;
                Gizmos.DrawWireCube(startPos + offset, cellSize);
            }
        }
        #endregion
    }
}