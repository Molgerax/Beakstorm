using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    [DefaultExecutionOrder(-100)]
    public class SdfShapeManager : MonoBehaviour
    {
        [SerializeField, Range(0, 10)] private float sdfGrowBounds = 1f;
        
        public static SdfShapeManager Instance;

        public List<AbstractSdfShape> Shapes = new List<AbstractSdfShape>(16);
        private BVH<AbstractSdfShape, AbstractSdfData> _bvh;

        private AbstractSdfShape[] _shapes = new AbstractSdfShape[16];
        private Node[] _nodeList;
        private AbstractSdfData[] _dataArray;
        private BVHItem[] _bvhItems;
        
        public GraphicsBuffer NodeBuffer;
        public GraphicsBuffer SdfBuffer;

        public int NodeCount => _shapeCount;
        public float SdfGrowBounds => sdfGrowBounds;
        
        private int _bufferSize = 16;
        private int _shapeCount;
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
            if (_shapes.Length == 0 || SdfBuffer == null)
                return;

            _bvh = new BVH<AbstractSdfShape, AbstractSdfData>(_shapes, _shapeCount, ref _bvhItems, ref _nodeList, ref _dataArray);

            ResizeNodeBuffer();
            
            NodeBuffer.SetData(_nodeList);
            SdfBuffer.SetData(_dataArray);
        }

        private void InitializeBuffers(bool node = false)
        {
            _shapes = new AbstractSdfShape[_bufferSize];
            _dataArray = new AbstractSdfData[_bufferSize];
            _bvhItems = new BVHItem[_bufferSize];

            SdfBuffer?.Dispose();
            SdfBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 16);

            if (node)
            {
                NodeBuffer?.Dispose();
                NodeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _nodeList.Length, sizeof(float) * 8);
            }
        }
        
        private void ResizeBuffers()
        {
            if (Shapes.Count > _bufferSize)
            {
                _bufferSize = Mathf.NextPowerOfTwo(Shapes.Count);
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
            _shapeCount = Shapes.Count;
            if (!_updateArray)
                return;
            ResizeBuffers();
            for (int i = 0; i < _shapeCount; i++)
            {
                _shapes[i] = Shapes[i];
            }
            _updateArray = false;
        }
        
        public void AddShape(AbstractSdfShape shape)
        {
            if (!Shapes.Contains(shape))
            {
                Shapes.Add(shape);
                _updateArray = true;
            }
        }

        public void RemoveShape(AbstractSdfShape shape)
        {
            if (Shapes.Contains(shape))
            {
                Shapes.Remove(shape);
                _updateArray = true;
            }
        }
    }
}
