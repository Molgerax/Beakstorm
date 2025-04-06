using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    [DefaultExecutionOrder(-100)]
    [ExecuteAlways]
    public class SdfShapeManager : MonoBehaviour
    {
        [SerializeField, Range(0, 10)] private float sdfGrowBounds = 1f;
        [SerializeField, Range(0, 8)] private int visualizeBounds = 0;

        [SerializeField] private bool renderSdf = false;
        [SerializeField] private Material material;
        [SerializeField] private Mesh mesh;
        
        private MaterialPropertyBlock _propBlock;
        
        public static SdfShapeManager Instance;

        public static List<AbstractSdfShape> Shapes = new List<AbstractSdfShape>(16);

        private AbstractSdfShape[] _shapes = new AbstractSdfShape[16];
        private Node[] _nodeList;
        private AbstractSdfData[] _dataArray;
        private BVHItem[] _bvhItems;
        
        public GraphicsBuffer NodeBuffer;
        public GraphicsBuffer SdfBuffer;

        private int _nodeCount;
        public int NodeCount => _nodeCount;
        public float SdfGrowBounds => sdfGrowBounds;
        
        private int _bufferSize = 16;
        private int _shapeCount;
        private static bool _updateArray = false;
        private bool _initialized = false;
        
        public Node[] NodeList => _nodeList;
        public BVHItem[] BvhItems => _bvhItems;
        public int BufferSize => _bufferSize;

        private void OnEnable()
        {
            if (Instance)
            {
                this.enabled = false;
                return;
            }
                
            Instance = this;

            _bufferSize = 16;
            _nodeList = new Node[4];
            
            InitializeBuffers(true);
            ResizeBuffers();
            FillArray();
            
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
                return;
            
            UpdateArray();
            ResizeBuffers();
            ConstructBvh();

            RenderPreview();
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;
            
            Instance = null;
            
            ReleaseBuffers();
            _initialized = false;
        }

        private void ConstructBvh()
        {
            if (_shapes == null || _shapes.Length == 0 || SdfBuffer == null)
                return;

            if (Shapes.Count == 0)
                return;
            
            _nodeCount = BVH<AbstractSdfShape, AbstractSdfData>.ConstructBVH(_shapes, _shapeCount, ref _bvhItems, ref _nodeList, ref _dataArray);

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
            FillArray();
            _updateArray = false;
        }

        private void FillArray()
        {
            for (int i = 0; i < Mathf.Min(_shapes.Length, Shapes.Count); i++)
            {
                _shapes[i] = Shapes[i];
            }
        }
        
        public static void AddShape(AbstractSdfShape shape)
        {
            if (!Shapes.Contains(shape))
            {
                Shapes.Add(shape);
                _updateArray = true;
            }
        }

        public static void RemoveShape(AbstractSdfShape shape)
        {
            if (Shapes.Contains(shape))
            {
                Shapes.Remove(shape);
                _updateArray = true;
            }
        }
        
        
        private void RenderPreview()
        {
            if (!renderSdf || !_initialized)
                return;
                
            if (_nodeList == null || _nodeList.Length == 0)
                return;
        
            if (!material || !mesh)
                return;
            
            _propBlock ??= new();
            
            _propBlock.SetBuffer("_NodeBuffer", NodeBuffer);
            _propBlock.SetBuffer("_SdfBuffer", SdfBuffer);
            _propBlock.SetInt("_NodeCount", NodeCount);

            Bounds bounds = new Bounds();
            bounds.Encapsulate(_nodeList[0].BoundsMin);
            bounds.Encapsulate(_nodeList[0].BoundsMax);

            RenderParams rp = new RenderParams(material)
            {
                worldBounds = bounds,
                //instanceID = GetInstanceID(),
                layer = gameObject.layer,
                matProps = _propBlock,
            };
            
            Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size));
        }

        private void OnDrawGizmosSelected()
        {
            if (_nodeList == null || _nodeList.Length == 0 || !_initialized)
                return;
            
            DrawNodeGizmos(_nodeList[0], 0);
        }

        private void DrawNodeGizmos(Node node, int depth)
        {
            if (depth > 8)
                return;
            
            bool isLeaf = node.ItemCount > 0;

            Color col = Color.HSVToRGB(depth / 8.0f, 1f, 1f);
            col.a = visualizeBounds == depth ? 1f : 0.1f;
            Gizmos.color = col;
            
            Gizmos.DrawWireCube(node.CalculateBoundsCenter(), node.CalculateBoundsSize());
            if (isLeaf)
            {
                //Gizmos.DrawWireCube(node.CalculateBoundsCenter(), node.CalculateBoundsSize());
            }
            else
            {
                if (node.StartIndex + 0 < _nodeCount) DrawNodeGizmos(_nodeList[node.StartIndex + 0], depth + 1);
                if (node.StartIndex + 1 < _nodeCount) DrawNodeGizmos(_nodeList[node.StartIndex + 1], depth + 1);
            }
        }
    }
}
