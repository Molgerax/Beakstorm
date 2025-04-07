using System;
using DynaMak.Particles;
using UnityEngine;

namespace DynaMak.Meshes
{
    [CreateAssetMenu(menuName = "DynaMak/Mesh Assets/Fractured Mesh Asset", fileName = "FracturedMeshAsset")]
    public class FracturedMeshAsset : MeshAsset, IParticleCountInfo
    {
        #region Serialize Fields
        [SerializeField] protected Texture2D pivotTexture;
        [SerializeField] protected Texture2D indexTexture;
        [SerializeField] protected TextAsset boundsText;

        [Header("Values From Text")] 
        [SerializeField] protected int numberOfPieces;
        [SerializeField] protected int meshTriangles;
        [SerializeField] protected Vector3 boundsCenter;
        [SerializeField] protected Vector3 boundsSize;

        #endregion

        #region Property Getters

        public Texture PivotTexture => pivotTexture;
        public Texture IndexTexture => indexTexture;


        public int NumberOfPieces => numberOfPieces;
        public int MeshTriangles => meshTriangles; 
        
        public Bounds Bounds => new(BoundsCenter, BoundsSize);
        
        private Vector3 BoundsCenter => boundsCenter;
        private Vector3 BoundsSize => boundsSize;
        #endregion


        #region Overrides

        public override void Initialize()
        {
            base.Initialize();
            ReadTextToBounds();
            meshTriangles = _indexBuffer.count / 3;
        }

        #endregion

        
        #region Interface Implementations

        public int MaxParticleCount => numberOfPieces;
        public int RenderTriangleCount => meshTriangles;

        #endregion
        
        
        #region Read Text

        private void ReadTextToBounds()
        {
            if(boundsText == null) return;
            
            string[] textValues = boundsText.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (textValues.Length >= 6)
            {
                float[] values = new float[textValues.Length];

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = float.Parse(textValues[i], 
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                boundsCenter.x = values[0];
                boundsCenter.y = values[1];
                boundsCenter.z = values[2];
                boundsSize.x = values[3];
                boundsSize.y = values[4];
                boundsSize.z = values[5];

                if (values.Length == 7) numberOfPieces = int.Parse(textValues[6]);
            }
        }

        #endregion
    }
}
