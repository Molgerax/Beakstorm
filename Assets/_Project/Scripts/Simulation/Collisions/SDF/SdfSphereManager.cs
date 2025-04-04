using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    [DefaultExecutionOrder(-100)]
    public class SdfSphereManager : MonoBehaviour
    {
        public static SdfSphereManager Instance;

        public List<SdfSphere> Spheres = new List<SdfSphere>(16);
        private SdfSphere[] _spheres = new SdfSphere[16];
        private BVH<SdfSphere, float4> _bvh;

        private float4[] _dataArray;
        private Node[] _nodeList;
        private BVHItem[] _bvhItems;
        
        public GraphicsBuffer NodeBuffer;
        public GraphicsBuffer SdfBuffer;

        private int _bufferSize = 16;
        private int _sphereCount;
        
        private void Awake()
        {
            Instance = this;

            _bufferSize = 16;
            _nodeList = new Node[32];
            
            InitializeBuffers();
        }

        private void Update()
        {
            ResizeBuffers();
            
            ConstructBvh();
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        private void ConstructBvh()
        {
            if (_spheres.Length == 0 || SdfBuffer == null)
                return;

            _bvh = new BVH<SdfSphere, float4>(_spheres, _sphereCount, ref _bvhItems, ref _nodeList, ref _dataArray);

            int nodeCount = _nodeList.Length;
            
            //Debug.Log($"SphereCount: {_sphereCount}, BufferSiz: {_bufferSize}, NodeCount: {_bvh.AllNodes.Nodes.Length}");

            if (NodeBuffer.count != nodeCount)
            {
                NodeBuffer?.Dispose();
                NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, sizeof(float) * 8);
            }
            
            NodeBuffer.SetData(_nodeList);
            SdfBuffer.SetData(_dataArray);
        }

        private void InitializeBuffers()
        {
            _spheres = new SdfSphere[_bufferSize];
            _dataArray = new float4[_bufferSize];
            _bvhItems = new BVHItem[_bufferSize];

            NodeBuffer?.Dispose();
            NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 8);
                
            SdfBuffer?.Dispose();
            SdfBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);
        }
        
        private void ResizeBuffers()
        {
            _bufferSize = Mathf.Max(1, _bufferSize);
            if (Spheres.Count > _bufferSize)
            {
                _bufferSize = Mathf.NextPowerOfTwo(Spheres.Count);
                
                InitializeBuffers();
            }
        }

        private void ReleaseBuffers()
        {
            NodeBuffer?.Dispose();
            NodeBuffer = null;
            SdfBuffer?.Dispose();
            SdfBuffer = null;
        }

        private void UpdateArray()
        {
            _sphereCount = Spheres.Count;
            for (int i = 0; i < _sphereCount; i++)
            {
                _spheres[i] = Spheres[i];
            }
        }
        
        public void AddSphere(SdfSphere sphere)
        {
            if (!Spheres.Contains(sphere))
            {
                Spheres.Add(sphere);
                UpdateArray();
            }
        }

        public void RemoveSphere(SdfSphere sphere)
        {
            if (Spheres.Contains(sphere))
            {
                Spheres.Remove(sphere);
                UpdateArray();
            }
        }
    }
}
