using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.MarchingCubes
{
    [System.Serializable]
    public class MarchingCubes
    {
        #region Defines

        private const int ThreadBlockSize = 2;

        #endregion

        #region Initialize Fields

        private ComputeShader _ComputeShader;

        private float _surfaceLevel;
        private bool _fillEdge = false;
        private bool _invert;

        private Vector3Int _marchResolution;
        private Vector3 _marchCenter;
        private Vector3 _marchBounds;

        #endregion

        // --------------------

        #region Constructor

        public MarchingCubes(ComputeShader marchCompute, float surfaceLevel, Vector3Int resolution, Vector3 center,
            Vector3 bounds, bool fillEdge = false, bool invert = false)
        {
            _ComputeShader = marchCompute;
            _marchResolution = resolution;
            _marchCenter = center;
            _marchBounds =  bounds;
            _surfaceLevel = surfaceLevel;
            _fillEdge = fillEdge;
            _invert = invert;

            InitializeBuffers();
        }


        #endregion



        // -------------------

        #region Shader Property IDs

        private int volumeTextureID = Shader.PropertyToID("_VolumeTexture");
        private int marchCenterID = Shader.PropertyToID("_MarchCenter");
        private int marchBoundsID = Shader.PropertyToID("_MarchBounds");
        private int marchResolutionID = Shader.PropertyToID("_MarchResolution");
        
        private int triangleBufferID = Shader.PropertyToID("_TriangleBuffer");

        private int surfaceLevelID = Shader.PropertyToID("_SurfaceLevel");
        private int fillEdgeID = Shader.PropertyToID("_FillEdge");
        private int invertID = Shader.PropertyToID("_Invert");

        

        #endregion

        //----------------

        #region Structs

        struct Triangle
        {
            private Vector3 v0, v1, v2, n0, n1, n2;
        }


        #endregion

        //----------------

        #region Private Fields

        public ComputeBuffer triangleBuffer, triangleCountBuffer;

        private int marchKernel;

        private int[] triCountArray = {0};
        private int numTris, numVoxels, maxTriangleCount;

        Triangle[] emptyTriBuffer = new Triangle[1];

        private Bounds drawBounds;

        private MaterialPropertyBlock _propBlock;

        #endregion

        // ------------------------------------

        #region Public Methods

        public void MarchTexture(VolumeTexture volumeTexture)
        {
            _ComputeShader.SetVolume(marchKernel, volumeTexture, volumeTextureID, marchCenterID, marchBoundsID, marchResolutionID);

            _marchCenter = volumeTexture.Center;
            _marchBounds = volumeTexture.Bounds;
            
            SetComputeValues();
            DispatchCompute();
        }

        public void MarchTexture(VolumeTexture volumeTexture, float surfaceLevel, bool fillEdge = false, bool invert = false)
        {
            _ComputeShader.SetVolume(marchKernel, volumeTexture, volumeTextureID, marchCenterID, marchBoundsID, marchResolutionID);

            _marchCenter = volumeTexture.Center;
            _marchBounds = volumeTexture.Bounds;
            
            _surfaceLevel = surfaceLevel;
            _fillEdge = fillEdge;
            _invert = invert;

            SetComputeValues();
            DispatchCompute();
        }
        
        public void MarchTexture(VolumeTexture volumeTexture, Vector3Int overrideResolution, float surfaceLevel, bool fillEdge = false, bool invert = false)
        {
            _ComputeShader.SetVolume(marchKernel, volumeTexture, volumeTextureID, marchCenterID, marchBoundsID, marchResolutionID);
            _ComputeShader.SetInts(marchResolutionID, overrideResolution.x, overrideResolution.y, overrideResolution.z);

            _marchResolution = overrideResolution;
            _marchCenter = volumeTexture.Center;
            _marchBounds = volumeTexture.Bounds;
            
            _surfaceLevel = surfaceLevel;
            _fillEdge = fillEdge;
            _invert = invert;

            SetComputeValues();
            DispatchCompute();
        }

        public void DrawCubesProcedural(Material proceduralMat)
        {
            _propBlock.SetBuffer(triangleBufferID, triangleBuffer);

            drawBounds.center = _marchCenter;
            drawBounds.extents = _marchBounds;

            Graphics.DrawProcedural(proceduralMat, drawBounds, MeshTopology.Triangles, 3, numTris, null, _propBlock);
        }

        #endregion

        // ------------------------------------

        #region Private Methods

        public void InitializeBuffers()
        {
            SetComputeValues();

            marchKernel = _ComputeShader.FindKernel("March");

            _propBlock = new MaterialPropertyBlock();
            numVoxels = (_marchResolution.x - 1) * (_marchResolution.y - 1) * (_marchResolution.z - 1);
            maxTriangleCount = numVoxels * 5;

            ReleaseBuffers();

            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 6, ComputeBufferType.Append);
            triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }

        void DispatchCompute()
        {
            triangleBuffer.SetCounterValue(0);
            triangleBuffer.SetData(emptyTriBuffer);

            _ComputeShader.SetBuffer(marchKernel, triangleBufferID, triangleBuffer);
            _ComputeShader.Dispatch(marchKernel, (_marchResolution.x - 1) / ThreadBlockSize,
                (_marchResolution.y - 1) / ThreadBlockSize, (_marchResolution.z - 1) / ThreadBlockSize);

            //Counter
            ComputeBuffer.CopyCount(triangleBuffer, triangleCountBuffer, 0);
            triangleCountBuffer.GetData(triCountArray);
            numTris = triCountArray[0];
        }


        void SetComputeValues()
        {
            _ComputeShader.SetFloat(surfaceLevelID, _surfaceLevel);
            _ComputeShader.SetBool(fillEdgeID, _fillEdge);
            _ComputeShader.SetBool(invertID, _invert);
        }

        public void ReleaseBuffers()
        {
            if (triangleBuffer != null) triangleBuffer.Release();
            if (triangleCountBuffer != null) triangleCountBuffer.Release();
        }

        #endregion
    }
}