using System;
using System.Collections.Generic;
using Beakstorm.ComputeHelpers;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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

        [Header("Sdf Atlas")] 
        [SerializeField] private ComputeShader atlasCompute;
        [SerializeField] private Vector3Int atlasResolution;
        
        private RenderTexture sdfAtlasTexture;

        private MaterialPropertyBlock _propBlock;
        
        public static SdfShapeManager Instance;

        public static List<AbstractSdfShape> Shapes = new List<AbstractSdfShape>(16);
        private static List<SdfTextureField> _textureFields = new(16);

        private AbstractSdfShape[] _shapes = new AbstractSdfShape[16];
        private Node[] _nodeList;
        private AbstractSdfData[] _dataArray;
        private BVHItem[] _bvhItems;
        
        public GraphicsBuffer NodeBuffer;
        public GraphicsBuffer SdfBuffer;

        private int _nodeCount;
        public int NodeCount => _nodeCount;
        public float SdfGrowBounds => sdfGrowBounds;

        public RenderTexture SdfAtlasTexture => sdfAtlasTexture;
        
        private int _bufferSize = 16;
        private int _shapeCount;
        private static bool _updateArray = false;
        private bool _initialized = false;
        
        public Node[] NodeList => _nodeList;
        public BVHItem[] BvhItems => _bvhItems;
        public int BufferSize => _bufferSize;

        public void SetShaderProperties(ComputeShader cs, int kernelId)
        {
            cs.SetBuffer(kernelId, PropertyIDs.NodeBuffer,NodeBuffer);
            cs.SetBuffer(kernelId, PropertyIDs.SdfBuffer, SdfBuffer);
            cs.SetInt(PropertyIDs.NodeCount, NodeCount);
            
            
            cs.SetTexture(kernelId, PropertyIDs.SdfAtlasTexture, SdfAtlasTexture);
            cs.SetInts(PropertyIDs.SdfAtlasResolution, atlasResolution);
        }
        
        public void SetShaderProperties(MaterialPropertyBlock propertyBlock)
        {
            propertyBlock.SetBuffer(PropertyIDs.NodeBuffer,NodeBuffer);
            propertyBlock.SetBuffer(PropertyIDs.SdfBuffer, SdfBuffer);
            propertyBlock.SetInt(PropertyIDs.NodeCount, NodeCount);
            
            propertyBlock.SetTexture(PropertyIDs.SdfAtlasTexture, SdfAtlasTexture);
            propertyBlock.SetVector(PropertyIDs.SdfAtlasResolution, (Vector3)atlasResolution);
        }
        
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
            
            InitAtlas();
            
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
                return;
            
            UpdateSdfAtlas();
            
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

        public float DistanceToBoundingBox(float3 pos, float3 bmin, float3 bmax)
        {
            float3 c = (bmax + bmin) / 2;
            float3 b = (bmax - bmin) / 2;

            float3 q = math.abs(pos - c) - b;
            return (float)math.length(math.max(q, 0.0)) + math.min(math.max(q.x, math.max(q.y, q.z)), 0);
        }


        public int TestBvh(Vector3 pos, int nodeOffset, out float dist, out Vector3 normal, bool gradientNormal = false)
        {
            int hits = 0;
            dist = Single.PositiveInfinity;
            normal = Vector3.up;

            float distX,distY,distZ;
            distX = distY = distZ = dist;

            
            if (_nodeList == null || _dataArray == null)
                return -1;
            
            int[] stack = new int[32];
            uint stackIndex = 0;
            stack[stackIndex++] = nodeOffset + 0;

            int limiter = 0;
    
            while (stackIndex > 0 && limiter < 1024)
            {
                stackIndex = math.min(31, stackIndex);
                limiter++;
        
                Node node = _nodeList[stack[--stackIndex]];
                bool isLeaf = node.ItemCount > 0;

                if (isLeaf)
                {
                    for (int i = 0; i < node.ItemCount; i++)
                    {
                        AbstractSdfData data = _dataArray[node.StartIndex + i];
                        TestSdf(pos, data, out float testDist, out Vector3 testNormal);

                        TestSdf(pos + Vector3.right * 0.01f, data, out float testDistX, out _);
                        TestSdf(pos + Vector3.up * 0.01f, data, out float testDistY, out _);
                        TestSdf(pos + Vector3.forward * 0.01f, data, out float testDistZ, out _);

                        distX = math.min(distX, testDistX);
                        distY = math.min(distY, testDistY);
                        distZ = math.min(distZ, testDistZ);
                        
                        
                        if (testDist < dist)
                        {
                            dist = testDist;
                            normal = testNormal;
                        }
                        
                        if (testDist <= 0)
                            hits++;
                    }
                }
                else
                {
                    int childIndexA = nodeOffset + node.StartIndex + 0;
                    int childIndexB = nodeOffset + node.StartIndex + 1;
                    Node childA = _nodeList[childIndexA];
                    Node childB = _nodeList[childIndexB];

                    float dstA = DistanceToBoundingBox(pos, childA.BoundsMin, childA.BoundsMax);
                    float dstB = DistanceToBoundingBox(pos, childB.BoundsMin, childB.BoundsMax);
						
                    // We want to look at closest child node first, so push it last
                    bool isNearestA = dstA <= dstB;
                    float dstNear = isNearestA ? dstA : dstB;
                    float dstFar = isNearestA ? dstB : dstA;
                    int childIndexNear = isNearestA ? childIndexA : childIndexB;
                    int childIndexFar = isNearestA ? childIndexB : childIndexA;

                    if (dstFar < math.max(0,dist)) stack[stackIndex++] = childIndexFar;
                    if (dstNear < math.max(0,dist)) stack[stackIndex++] = childIndexNear;
                }
            }

            if (gradientNormal)
                normal = Vector3.Normalize(new(distX - dist, distY - dist, distZ - dist));
            return hits;
        }
        
        
        public bool TestSdf(Vector3 pos, AbstractSdfData data, out float dist, out Vector3 normal)
        {
            uint id = data.Type & 0x0F;

            if (id == (uint) SdfShapeType.Box)
                return SdfBox.TestSdf(pos, data, out dist, out normal);
            if (id == (uint) SdfShapeType.Sphere)
                return SdfSphere.TestSdf(pos, data, out dist, out normal);

            return AbstractSdfShape.TestSdf(pos, data, out dist, out normal);
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
            
            if (SdfAtlasTexture)
                SdfAtlasTexture.Release();
            sdfAtlasTexture = null;
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
                
                if (shape is SdfTextureField textureField)
                    _textureFields.Add(textureField);
            }
        }

        public static void RemoveShape(AbstractSdfShape shape)
        {
            if (Shapes.Contains(shape))
            {
                Shapes.Remove(shape);
                _updateArray = true;
                
                if (shape is SdfTextureField textureField)
                    _textureFields.Remove(textureField);
            }
        }
        
        private void InitAtlas()
        {
            sdfAtlasTexture = new RenderTexture(atlasResolution.x, atlasResolution.y, 0, RenderTextureFormat.ARGBFloat);
            sdfAtlasTexture.volumeDepth = atlasResolution.z;
            sdfAtlasTexture.dimension = TextureDimension.Tex3D;
            sdfAtlasTexture.enableRandomWrite = true;
            sdfAtlasTexture.name = "ShapeManager_SDFAtlas";
            sdfAtlasTexture.Create();
        }

        private void SortTextureFields()
        {
            _textureFields.Sort();
        }

        private void UpdateSdfAtlas()
        {
            ClearSdfAtlas();
            SortTextureFields();

            int i = 0;
            UpdateSdfAtlasRecursive(ref i, Vector3Int.zero, atlasResolution);
        }

        private void UpdateSdfAtlasRecursive(ref int textureIndex, Vector3Int startVoxel, Vector3Int currentResolution)
        {
            if (textureIndex >= _textureFields.Count)
                return;

            if (currentResolution.sqrMagnitude <= 8)
                return;
            
            SdfTextureField field = _textureFields[textureIndex];
            if (field.Resolution == currentResolution)
            {
                SetTexture(field, startVoxel);
                textureIndex++;
                return;
            }
            if (currentResolution.x != currentResolution.y || currentResolution.x != currentResolution.z)
            {
                if (currentResolution.x > currentResolution.y && currentResolution.x > currentResolution.z)
                {
                    currentResolution.x /= 2;
                    UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 0, 0) * currentResolution, currentResolution);
                    UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(1, 0, 0) * currentResolution, currentResolution);
                }
                else if (currentResolution.y > currentResolution.x && currentResolution.y > currentResolution.z)
                {
                    currentResolution.y /= 2;
                    UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 0, 0) * currentResolution, currentResolution);
                    UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 1, 0) * currentResolution, currentResolution);
                }
                //if (currentResolution.z > currentResolution.x || currentResolution.z > currentResolution.y)
                else 
                {
                    currentResolution.z /= 2;
                    UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 0, 0) * currentResolution, currentResolution);
                    UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 0, 1) * currentResolution, currentResolution);
                }
            }
            else
            {
                currentResolution /= 2;
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 0, 0) * currentResolution, currentResolution);
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(1, 0, 0) * currentResolution, currentResolution);
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 1, 0) * currentResolution, currentResolution);
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(1, 1, 0) * currentResolution, currentResolution);
                
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 0, 1) * currentResolution, currentResolution);
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(1, 0, 1) * currentResolution, currentResolution);
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(0, 1, 1) * currentResolution, currentResolution);
                UpdateSdfAtlasRecursive(ref textureIndex, startVoxel + new Vector3Int(1, 1, 1) * currentResolution, currentResolution);
            }
        }

        private void SetTexture(SdfTextureField field, Vector3Int startVoxel)
        {
            if (CanFitInAtlas(startVoxel, field.Resolution, atlasResolution))
            {
                field.SetStartVoxel(startVoxel);
                int kernelId = atlasCompute.FindKernel("Transfer");
                atlasCompute.SetTexture(kernelId, PropertyIDs.SdfAtlasTexture, sdfAtlasTexture);
                atlasCompute.SetInts(PropertyIDs.SdfAtlasResolution, atlasResolution);
                atlasCompute.SetTexture(kernelId, PropertyIDs.SourceTexture, field.SdfTexture);
                atlasCompute.SetInts(PropertyIDs.SourceResolution, field.Resolution);
                atlasCompute.SetInts(PropertyIDs.TransferOffset, startVoxel);
                atlasCompute.DispatchExact(kernelId, atlasResolution);
            }
        }
        
        private bool CanFitInAtlas(Vector3Int startVoxel, Vector3Int res, Vector3Int totalRes)
        {
            Vector3Int sum = startVoxel + res;
            if (sum.x > totalRes.x)
                return false;
            if (sum.y > totalRes.y)
                return false;
            if (sum.z > totalRes.z)
                return false;
            return true;
        }

        private void ClearSdfAtlas()
        {
            int kernelId = atlasCompute.FindKernel("Clear");
            atlasCompute.SetTexture(kernelId, PropertyIDs.SdfAtlasTexture, sdfAtlasTexture);
            atlasCompute.SetInts(PropertyIDs.SdfAtlasResolution, atlasResolution);
            atlasCompute.DispatchExact(kernelId, atlasResolution);
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
            
            _propBlock.SetBuffer(PropertyIDs.NodeBuffer, NodeBuffer);
            _propBlock.SetBuffer(PropertyIDs.SdfBuffer, SdfBuffer);
            _propBlock.SetInt(PropertyIDs.NodeCount, NodeCount);
            
            _propBlock.SetTexture(PropertyIDs.SdfAtlasTexture, SdfAtlasTexture);
            _propBlock.SetVector(PropertyIDs.SdfAtlasResolution, (Vector3)atlasResolution);

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
        
        private static class PropertyIDs
        {
            public static readonly int NodeBuffer = Shader.PropertyToID("_NodeBuffer");
            public static readonly int SdfBuffer = Shader.PropertyToID("_SdfBuffer");
            public static readonly int NodeCount = Shader.PropertyToID("_NodeCount");
            
            public static readonly int SdfAtlasTexture = Shader.PropertyToID("_SdfAtlasTexture");
            public static readonly int SdfAtlasResolution = Shader.PropertyToID("_SdfAtlasResolution");
            
            public static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");
            public static readonly int SourceResolution = Shader.PropertyToID("_SourceResolution");
            public static readonly int TransferOffset = Shader.PropertyToID("_TransferOffset");
        }
        

        [ContextMenu("Open SDF Atlas")]
        private void OpenAtlasProperties()
        {
            #if UNITY_EDITOR
            if (sdfAtlasTexture)
                UnityEditor.EditorUtility.OpenPropertyEditor(sdfAtlasTexture);
            #endif
        }
    }
}
