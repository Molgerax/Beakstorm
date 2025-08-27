using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfTextureField : AbstractSdfShape
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        [SerializeField] private int resolution = 32;

        [SerializeField] private RenderTexture sdfTexture;
        
        protected override SdfShapeType Type() => SdfShapeType.Texture;
     
        public Vector3Int Resolution => Vector3Int.one * resolution;

        private Vector3Int _startVoxel;

        public RenderTexture SdfTexture => sdfTexture;
        public void SetStartVoxel(Vector3Int v) => _startVoxel = v;

        protected override void OnEnable()
        {
            base.OnEnable();
            Init();
        }

        private void Reset()
        {
            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Init()
        {
            sdfTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
            sdfTexture.volumeDepth = resolution;
            sdfTexture.dimension = TextureDimension.Tex3D;
            sdfTexture.enableRandomWrite = true;
            sdfTexture.Create();
            
            Bake();
        }

        [ContextMenu("Bake")]
        public void Bake()
        {
            MeshToSdfStatic.InputArgs args = new MeshToSdfStatic.InputArgs();
            args.Offset = 0;
            args.Quality = MeshToSdfStatic.FloodFillQuality.Ultra;
            args.FillIterations = 64;
            args.FloodMode = MeshToSdfStatic.FloodMode.Linear;
            args.Resolution = Resolution;
            args.DistanceMode = MeshToSdfStatic.DistanceMode.Signed;

            MeshToSdfStatic meshToSdf = new MeshToSdfStatic(cs, sdfTexture, args, meshFilter);
            meshToSdf.UpdateSDF();
            meshToSdf.Dispose();
        }

        protected override void OnDisable()
        {
            if (sdfTexture)
                sdfTexture.Release();
            sdfTexture = null;
            
            base.OnDisable();
        }

        private void Update()
        {
            var bounds = CalculateBounds();
            float3 pos = bounds.center;
            float3 scale = bounds.size;
            float3 data = new float3(_startVoxel.x, _startVoxel.y, _startVoxel.z) + 0.5f;
            float3 res = new float3(Resolution.x, Resolution.y, Resolution.z) + 0.5f; 
            
            
            _sdfData = new AbstractSdfData(scale, res, 0, pos, data, GetTypeData());
        }

        private Bounds CalculateBounds()
        {
            Bounds bounds = meshRenderer.bounds;
            bounds.size += Vector3.one;
            
            int longestAxis = LongestAxis(bounds.size);
            float voxelSize = (bounds.size[longestAxis]) / Resolution[longestAxis];
            bounds = new Bounds(bounds.center, (Vector3)Resolution * voxelSize);
            
            _boundsMin = bounds.min;
            _boundsMax = bounds.max;
            return bounds;
        }
        private int LongestAxis(Vector3 v)
        {
            if (v.x >= v.y && v.x >= v.z)
                return 0;
            if (v.y >= v.x && v.y >= v.z)
                return 1;
            return 2;
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new(1, 0, 0, 0.5f);
            var bounds = meshRenderer.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        
        
        
        public static float3 GetLargest(float3 value)
        {
            float3 firstTest = math.step(value.yzx, value);
            float3 secondTest = math.step(value.zxy, value);
            return firstTest * secondTest;
        }
        
        public new static bool TestSdf(float3 pos, AbstractSdfData data, out float dist, out Vector3 normal)
        {
            normal = Vector3.up;
            
            float3x3 rot = new float3x3(data.XAxis, data.YAxis, data.ZAxis);
    
            float3 q = math.mul(rot, pos - data.Translate);
            float3 diff = math.abs(q) - data.Data;
    
            dist = math.length(math.max(diff, 0)) + math.min(math.max(diff.x, math.max(diff.y, diff.z)), 0);
    
            float3 norm = (GetLargest(diff) + math.max(diff, 0)) * math.sign(q);
            if (math.dot(norm, norm) == 0)
                norm = new float3(0,1,0);
            normal = math.mul(math.transpose(rot), math.normalize(norm));

            return dist <= 0;
        }
    }
}
