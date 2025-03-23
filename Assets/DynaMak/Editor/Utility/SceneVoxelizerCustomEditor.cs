using System;
using DynaMak.Volumes;
using DynaMak.Volumes.Voxelizer;
using UnityEngine;
using UnityEditor;

namespace DynaMak.Editors
{
    [CustomEditor(typeof(SceneVoxelizer))]
    public class SceneVoxelizerCustomEditor : Editor
    {
        /*
        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmos(SceneVoxelizer sceneVoxelizer, GizmoType gizmoType)
        {
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(sceneVoxelizer.VoxelTexture.Center, sceneVoxelizer.VoxelTexture.Bounds * 2);
            
            
            for (int y = 0; y < sceneVoxelizer.VoxelTexture.Resolution.y; y += 1)
            { 
                DrawSingleLineX(sceneVoxelizer.VoxelTexture, y, sceneVoxelizer.VoxelTexture.Resolution.z);
            }    
            
            for (int x = 0; x < sceneVoxelizer.VoxelTexture.Resolution.x; x += 1)
            {
                for (int z = 0; z < 1; z += 2)
                {
                    DrawSingleLineY(sceneVoxelizer.VoxelTexture, x, z);
                }
            }  
            
            for (int x = 0; x < sceneVoxelizer.VoxelTexture.Resolution.x; x += 1)
            {
                for (int y = 0; y < 1; y += 2)
                {
                    DrawSingleLineZ(sceneVoxelizer.VoxelTexture, x, y);
                }
            }  
            
            
        }
        */



        private static void DrawSingleCell(VolumeTexture volume, int x, int y, int z)
        {
            Vector3 cellSize = new Vector3(2*volume.Bounds.x / volume.Resolution.x, 2*volume.Bounds.y / volume.Resolution.y, 2*volume.Bounds.z / volume.Resolution.z);
            Vector3 offset = new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(volume.Center - volume.Bounds + cellSize * 0.5f + offset, cellSize);
        }
        
        private static void DrawSingleCell(Vector3Int res, Vector3 center, Vector3 bounds, int x, int y, int z)
        {
            Vector3 cellSize = new Vector3(2*bounds.x / res.x, 2*bounds.y / res.y, 2*bounds.z / res.z);
            Vector3 offset = new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center - bounds + cellSize * 0.5f + offset, cellSize);
        }

        private static void DrawSingleLineX(VolumeTexture volume, int y, int z)
        {
            Vector3 cellSize = new Vector3(2*volume.Bounds.x / volume.Resolution.x, 2*volume.Bounds.y / volume.Resolution.y, 2*volume.Bounds.z / volume.Resolution.z);
            Vector3 offset = new Vector3(0, y * cellSize.y, z * cellSize.z);
            
            Gizmos.color = Color.yellow;
            
            Gizmos.DrawLine(volume.Center - volume.Bounds + offset, volume.Center - volume.Bounds + offset + Vector3.right * volume.Bounds.x * 2);
        }
        
        private static void DrawSingleLineY(VolumeTexture volume, int x, int z)
        {
            Vector3 cellSize = new Vector3(2*volume.Bounds.x / volume.Resolution.x, 2*volume.Bounds.y / volume.Resolution.y, 2*volume.Bounds.z / volume.Resolution.z);
            Vector3 offset = new Vector3(x * cellSize.x, 0, z * cellSize.z);
            
            Gizmos.color = Color.yellow;
            
            Gizmos.DrawLine(volume.Center - volume.Bounds + offset, volume.Center - volume.Bounds + offset + Vector3.up * volume.Bounds.y * 2);
        }
        
        private static void DrawSingleLineZ(VolumeTexture volume, int x, int y)
        {
            Vector3 cellSize = new Vector3(2*volume.Bounds.x / volume.Resolution.x, 2*volume.Bounds.y / volume.Resolution.y, 2*volume.Bounds.z / volume.Resolution.z);
            Vector3 offset = new Vector3(x * cellSize.x, y * cellSize.y, 0);
            
            Gizmos.color = Color.yellow;
            
            Gizmos.DrawLine(volume.Center - volume.Bounds + offset, volume.Center - volume.Bounds + offset + Vector3.forward * volume.Bounds.z * 2);
        }
    }
}