using System;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    [DefaultExecutionOrder(-100)]
    public class SdfSphereManager : MonoBehaviour
    {
        [SerializeField, Range(0, 10)] private float sdfGrowBounds = 1f;
        
        public static SdfSphereManager Instance;

        public List<SdfSphere> Spheres = new List<SdfSphere>(16);
        private BVH<SdfSphere, float4> _bvh;

        private SdfSphere[] _spheres = new SdfSphere[16];
        private Node[] _nodeList;
        private float4[] _dataArray;
        private BVHItem[] _bvhItems;
        
        public GraphicsBuffer NodeBuffer;
        public GraphicsBuffer SdfBuffer;

        public int NodeCount => _sphereCount;
        public float SdfGrowBounds => sdfGrowBounds;
        
        private int _bufferSize = 16;
        private int _sphereCount;
        private bool _updateArray = false;

        public Node[] NodeList => _nodeList;
        public BVHItem[] BvhItems => _bvhItems;
        public int BufferSize => _bufferSize;

        private void Awake()
        {
            Instance = this;

            _bufferSize = 16;
            _nodeList = new Node[4];
            
            InitializeBuffers(true);
        }

        private void Update()
        {
            UpdateArray();
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

            ResizeNodeBuffer();
            
            NodeBuffer.SetData(_nodeList);
            SdfBuffer.SetData(_dataArray);
        }

        private void InitializeBuffers(bool node = false)
        {
            _spheres = new SdfSphere[_bufferSize];
            _dataArray = new float4[_bufferSize];
            _bvhItems = new BVHItem[_bufferSize];

            SdfBuffer?.Dispose();
            SdfBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);

            if (node)
            {
                NodeBuffer?.Dispose();
                NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _nodeList.Length, sizeof(float) * 8);
            }
        }
        
        private void ResizeBuffers()
        {
            if (Spheres.Count > _bufferSize)
            {
                _bufferSize = Mathf.NextPowerOfTwo(Spheres.Count);
                InitializeBuffers();
            }
        }
        
        private void ResizeNodeBuffer()
        {
            int nodeCount = _nodeList.Length;
            if (NodeBuffer.count != nodeCount)
            {
                NodeBuffer?.Dispose();
                NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, nodeCount, sizeof(float) * 8);
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
            if (!_updateArray)
                return;
            ResizeBuffers();
            for (int i = 0; i < _sphereCount; i++)
            {
                _spheres[i] = Spheres[i];
            }
            _updateArray = false;
        }
        
        public void AddSphere(SdfSphere sphere)
        {
            if (!Spheres.Contains(sphere))
            {
                Spheres.Add(sphere);
                _updateArray = true;
            }
        }

        public void RemoveSphere(SdfSphere sphere)
        {
            if (Spheres.Contains(sphere))
            {
                Spheres.Remove(sphere);
                _updateArray = true;
            }
        }
    }
}
