using System;
using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.SDF
{
    [System.Serializable]
    public class SDFVolume : VolumeTexture
    {
        #region Defines
        protected const int ThreadBlockSize = 2;
        #endregion

        #region Initialize Fields
        
        protected ComputeShader _ComputeShader;

        #endregion

        // -------------------
        
        #region Constructor

        public SDFVolume(ComputeShader marchCompute, Vector3Int resolution, Vector3 center, Vector3 bounds) : base(RenderTextureFormat.ARGBHalf, resolution, center, bounds)
        {
            _ComputeShader = marchCompute;
        }
        
        #endregion
        
        // ------------------
        
        

        #region Shader Property IDs

        private int volumeTextureID = Shader.PropertyToID("_SDFVolume");

        private int volumeResolutionID = Shader.PropertyToID("_SDFResolution");
        private int volumeCenterID = Shader.PropertyToID("_SDFCenter");
        private int volumeBoundsID = Shader.PropertyToID("_SDFBounds");


        private int boxBufferID = Shader.PropertyToID("_BoxBuffer");
        private int sphereBufferID = Shader.PropertyToID("_SphereBuffer");
        private int lineBufferID = Shader.PropertyToID("_LineBuffer");
        private int torusBufferID = Shader.PropertyToID("_TorusBuffer");

        private int boxLengthID = Shader.PropertyToID("_BoxLength");
        private int sphereLengthID = Shader.PropertyToID("_SphereLength");
        private int lineLengthID = Shader.PropertyToID("_LineLength");
        private int torusLengthID = Shader.PropertyToID("_TorusLength");

        #endregion

        //----------------

        #region Private Fields
        
        private int sdfComputeKernel, sdfNormalKernel;
        private ComputeBuffer _BoxBuffer, _SphereBuffer, _LineBuffer, _TorusBuffer;
        
        #endregion

        // ------------------------------------

        #region Public Methods

        public void ComputeSDF()
        {
            DispatchCompute();
            DispatchNormals();
        }

        public void SetBoxes(Box[] boxes)
        {
            if (_BoxBuffer != null) _BoxBuffer.Release();
            _BoxBuffer = new ComputeBuffer(Math.Max(1, boxes.Length), sizeof(float) * 12, ComputeBufferType.Structured);
            if (boxes.Length > 0)
            {
                _BoxBuffer.SetData(boxes);
                _ComputeShader.SetBuffer(sdfComputeKernel, boxBufferID, _BoxBuffer);
            }

            _ComputeShader.SetInt(boxLengthID, boxes.Length);
        }

        public void SetSpheres(Sphere[] spheres)
        {
            if (_SphereBuffer != null) _SphereBuffer.Release();
            _SphereBuffer = new ComputeBuffer(Math.Max(1, spheres.Length), sizeof(float) * 4,
                ComputeBufferType.Structured);
            if (spheres.Length > 0)
            {
                _SphereBuffer.SetData(spheres);
                _ComputeShader.SetBuffer(sdfComputeKernel, sphereBufferID, _SphereBuffer);
            }

            _ComputeShader.SetInt(sphereLengthID, spheres.Length);
        }

        public void SetLines(Line[] lines)
        {
            if (_LineBuffer != null) _LineBuffer.Release();
            _LineBuffer = new ComputeBuffer(Math.Max(1, lines.Length), sizeof(float) * 7, ComputeBufferType.Structured);
            if (lines.Length > 0)
            {
                _LineBuffer.SetData(lines);
                _ComputeShader.SetBuffer(sdfComputeKernel, lineBufferID, _LineBuffer);
            }

            _ComputeShader.SetInt(lineLengthID, lines.Length);
        }

        public void SetTori(Torus[] tori)
        {
            if (_TorusBuffer != null) _TorusBuffer.Release();
            _TorusBuffer = new ComputeBuffer(Math.Max(1, tori.Length), sizeof(float) * 8, ComputeBufferType.Structured);
            if (tori.Length > 0)
            {
                _TorusBuffer.SetData(tori);
                _ComputeShader.SetBuffer(sdfComputeKernel, torusBufferID, _TorusBuffer);
            }

            _ComputeShader.SetInt(torusLengthID, tori.Length);
        }

        #endregion

        // ------------------------------------

        #region Private Methods

        public override void Initialize()
        {
            base.Initialize();
            
            sdfComputeKernel = _ComputeShader.FindKernel("RecalculateSDF");
            sdfNormalKernel = _ComputeShader.FindKernel("CalculateNormals");
            
            _BoxBuffer = new ComputeBuffer(1, sizeof(float) * 12, ComputeBufferType.Structured);
            _SphereBuffer = new ComputeBuffer(1, sizeof(float) * 4, ComputeBufferType.Structured);
            _LineBuffer = new ComputeBuffer(1, sizeof(float) * 7, ComputeBufferType.Structured);
            _TorusBuffer = new ComputeBuffer(1, sizeof(float) * 8, ComputeBufferType.Structured);
        }

        protected void DispatchCompute()
        {
            _ComputeShader.SetVolume(sdfComputeKernel, this, volumeTextureID, volumeCenterID, volumeBoundsID, volumeResolutionID);
            
            _ComputeShader.SetBuffer(sdfComputeKernel, boxBufferID, _BoxBuffer);
            _ComputeShader.SetBuffer(sdfComputeKernel, sphereBufferID, _SphereBuffer);
            _ComputeShader.SetBuffer(sdfComputeKernel, lineBufferID, _LineBuffer);
            _ComputeShader.SetBuffer(sdfComputeKernel, torusBufferID, _TorusBuffer);
            
            _ComputeShader.Dispatch(sdfComputeKernel, Resolution, ThreadBlockSize);
        }

        protected void DispatchNormals()
        {
            _ComputeShader.SetVolume(sdfNormalKernel, this, volumeTextureID, volumeCenterID, volumeBoundsID, volumeResolutionID);
            _ComputeShader.Dispatch(sdfNormalKernel, this.Resolution, ThreadBlockSize);
        }

        public override void Release()
        {
            base.Release();
            if (_BoxBuffer != null) _BoxBuffer.Release();
            if (_SphereBuffer != null) _SphereBuffer.Release();
            if (_LineBuffer != null) _LineBuffer.Release();
            if (_TorusBuffer != null) _TorusBuffer.Release();
        }

        #endregion
    }

    [Serializable]
    public struct AABB
    {
        private Vector3 center;
        private Vector3 bounds;

        public AABB(Vector3 c, Vector3 b)
        {
            center = c;
            bounds = b;
        }
    }

    [Serializable]
    public struct Box
    {
        private Vector3 center;
        private Vector3 bounds;
        private Vector3 rightAxis;
        private Vector3 upAxis;

        public Box(Vector3 c, Vector3 b, Vector3 right, Vector3 up)
        {
            center = c;
            bounds = b;
            rightAxis = right;
            upAxis = up;
        }
    }

    [Serializable]
    public struct Sphere
    {
        private Vector3 center;
        private float radius;

        public Sphere(Vector3 c, float r)
        {
            center = c;
            radius = r;
        }
    }

    [Serializable]
    public struct Line
    {
        private Vector3 pointA;
        private Vector3 pointB;
        private float radius;

        public Line(Vector3 pA, Vector3 pB, float r)
        {
            pointA = pA;
            pointB = pB;
            radius = r;
        }
    }

    [Serializable]
    public struct Torus
    {
        private Vector3 center;
        private Vector3 normal;
        private float radius;
        private float thickness;

        public Torus(Vector3 c, Vector3 n, float r, float t)
        {
            center = c;
            normal = n;
            radius = r;
            thickness = t;
        }
    }
}