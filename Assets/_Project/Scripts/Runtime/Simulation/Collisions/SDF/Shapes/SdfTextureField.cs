using System;
using Beakstorm.ComputeHelpers;
using Beakstorm.Core.Attributes;
using Beakstorm.Rendering;
using Beakstorm.SceneManagement;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfTextureField : AbstractSdfShape, IComparable
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private ComputeShader combineSdfCs;
        [SerializeField] private GameObject parent;
        
        [SerializeField]
        [PowerOfTwo(4, 64)] private int resolution = 32;

        [SerializeField] private bool allMeshChildren;
        
        [Header("Saving")]
        [SerializeField] private Texture3D textureAsset;
        
        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.Third;
        
        public Texture3D InitializeFromScript(ComputeShader cs, ComputeShader combineSdfCs, SdfMaterialType materialType, int resolution, GameObject parent, bool allMeshChildren, bool noLongerReadable = true)
        {
            if (!this.cs)
                this.cs = cs;
            
            if (!this.combineSdfCs)
                this.combineSdfCs = combineSdfCs;
            
            this.resolution = resolution;
            this.parent = parent;
            this.allMeshChildren = allMeshChildren;
            this.materialType = materialType;
            
            BakeToObject(noLongerReadable);
            return textureAsset;
        }

        public bool GetBounds(out Vector3 min, out Vector3 max)
        {
            min = _cachedBounds.min;
            max = _cachedBounds.max;

            return _cachedBounds.size.magnitude > 0;
        }
        
        private void BakeToObject(bool noLongerReadable = true)
        {
#if UNITY_EDITOR
            Init();
            var result = BakeTexture3D.RenderTextureToTexture3D(_sdfTexture, noLongerReadable);
            textureAsset = result ? result : null;
            Release();
#endif
        }
        

        private GameObject Target => parent ? parent : gameObject;
        
        private RenderTexture _sdfTexture;
        private MeshCollider[] _meshColliders;

        [SerializeField, HideInInspector] private Bounds _cachedBounds;
        private Vector3 _cachedPos;

        private bool _initialized;

        public override bool IsValid
        {
            get
            {
                if (textureAsset)
                    return true;
                
                if (!_initialized)
                    return false;
                if (!_sdfTexture || !_sdfTexture.IsCreated())
                    return false;
                return true;
            }
        }

        private Bounds MovedBounds
        {
            get
            {
                Bounds b = _cachedBounds;
                b.center += (Target.transform.position - _cachedPos);
                return b;
            }
        }

        private UniTask _task;
        
        protected override SdfShapeType Type() => SdfShapeType.Texture;
     
        public Vector3Int Resolution => Vector3Int.one * resolution;

        private Vector3Int _startVoxel;

        public Texture SdfTexture => textureAsset ? textureAsset : _sdfTexture;
        public void SetStartVoxel(Vector3Int v) => _startVoxel = v;

        
        protected override void OnEnable()
        {
            base.OnEnable();
            _initialized = false;
            if (textureAsset)
            {
                _initialized = true;
                return;
            }

            Init();
        }

        private void Init()
        {
            Release();
            
            if (!cs)
                return;
            
            _sdfTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
            _sdfTexture.volumeDepth = resolution;
            _sdfTexture.dimension = TextureDimension.Tex3D;
            _sdfTexture.enableRandomWrite = true;
            _sdfTexture.name = gameObject.name + "_SDF";
            _sdfTexture.Create();
            
            Bake();
        }

        private void Release()
        {
            if (_sdfTexture)
            {
                _sdfTexture.Release();
                CoreUtils.Destroy(_sdfTexture);
            }
            _sdfTexture = null;
            _initialized = false;
        }

        [ContextMenu("Bake")]
        public void Bake()
        {
            if (!cs)
                return;
            
            if (!allMeshChildren)
            {
                if (Target.TryGetComponent(out MeshFilter meshFilter) && Target.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    Bounds bounds = CalculateBounds(meshRenderer.bounds);
                    float voxelSize = GetVoxelSize(bounds);
                    BakeSingleMesh(_sdfTexture, meshFilter, bounds, voxelSize);

                    _cachedBounds = bounds;
                    _cachedPos = Target.transform.position;

                    _initialized = true;
                }
                return;
            }


            _meshColliders = Target.GetComponentsInChildren<MeshCollider>();
            if (_meshColliders == null || _meshColliders.Length == 0)
            {
                Debug.LogError($"SDF Texture {name} contains no mesh colliders, skipping.");
                return;
            }
            
            Bounds allBounds = new Bounds();
            bool init = false;
            foreach (MeshCollider meshCollider in _meshColliders)
            {
                if (meshCollider.isTrigger)
                    continue;
                
                if (!init)
                {
                    allBounds = meshCollider.bounds;
                    init = true;
                }

                allBounds.Encapsulate(meshCollider.bounds);
            }

            allBounds = CalculateBounds(allBounds);
            float allVoxelSize = GetVoxelSize(allBounds);

            _cachedBounds = allBounds;
            _cachedPos = Target.transform.position;
            
            
            RenderTexture tempSdf = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
            tempSdf.volumeDepth = resolution;
            tempSdf.dimension = TextureDimension.Tex3D;
            tempSdf.enableRandomWrite = true;
            tempSdf.name = gameObject.name + "_SDF";
            tempSdf.Create();

            combineSdfCs.SetInt(PropertyIDs.Resolution, resolution);
            combineSdfCs.SetTexture(0, PropertyIDs.TextureWrite, tempSdf);
            combineSdfCs.DispatchExact(0, Resolution);
            
            foreach (MeshCollider meshCollider in _meshColliders)
            {
                if (meshCollider.isTrigger)
                    continue;
                
                BakeSingleMesh(_sdfTexture, meshCollider, allBounds, allVoxelSize);
                
                combineSdfCs.SetTexture(1, PropertyIDs.TextureRead, tempSdf);
                combineSdfCs.SetTexture(1, PropertyIDs.TextureWrite, _sdfTexture);
                combineSdfCs.DispatchExact(1, Resolution);
            }
            
            tempSdf.Release();

            _initialized = true;
        }

        private void BakeSingleMesh(RenderTexture texture, MeshFilter filter, Bounds bounds, float voxelSize)
        {
            MeshToSdfStatic.InputArgs args = new MeshToSdfStatic.InputArgs();
            args.Offset = 0;
            args.Quality = MeshToSdfStatic.FloodFillQuality.Ultra;
            args.FillIterations = 64;
            args.FloodMode = MeshToSdfStatic.FloodMode.Linear;
            args.Resolution = Resolution;
            args.DistanceMode = MeshToSdfStatic.DistanceMode.Signed;
            args.Bounds = bounds;
            args.VoxelSize = voxelSize;

            MeshToSdfStatic meshToSdf = new MeshToSdfStatic(cs, texture, args, filter);
            meshToSdf.UpdateSDF();
            meshToSdf.Dispose();
        }
        
        private void BakeSingleMesh(RenderTexture texture, MeshCollider meshCollider, Bounds bounds, float voxelSize)
        {
            MeshToSdfStatic.InputArgs args = new MeshToSdfStatic.InputArgs();
            args.Offset = 0;
            args.Quality = MeshToSdfStatic.FloodFillQuality.Ultra;
            args.FillIterations = 64;
            args.FloodMode = MeshToSdfStatic.FloodMode.Linear;
            args.Resolution = Resolution;
            args.DistanceMode = MeshToSdfStatic.DistanceMode.Signed;
            args.Bounds = bounds;
            args.VoxelSize = voxelSize;

            MeshToSdfStatic meshToSdf = new MeshToSdfStatic(cs, texture, args, meshCollider);
            meshToSdf.UpdateSDF();
            meshToSdf.Dispose();
        }

        protected override void OnDisable()
        {
            Release();
            base.OnDisable();
        }

        private void Update()
        {
            Bounds bounds = MovedBounds;
            float3 pos = bounds.center;
            float3 scale = bounds.size;
            float3 data = new float3(_startVoxel.x, _startVoxel.y, _startVoxel.z) + 0.5f;
            float3 res = new float3(Resolution.x, Resolution.y, Resolution.z) + 0.5f;
            
            _boundsMin = bounds.min;
            _boundsMax = bounds.max;

            _sdfData = new AbstractSdfData(scale, res, 0, pos, data, GetTypeData());
        }

        private Bounds CalculateBounds(Bounds b)
        {
            Bounds bounds = b;
            //bounds.size += Vector3.one;
            
            int longestAxis = LongestAxis(bounds.size);
            float voxelSize = (bounds.size[longestAxis]) / Resolution[longestAxis];
            bounds = new Bounds(bounds.center, ((Vector3)Resolution + Vector3.one * 2) * voxelSize);
            
            return bounds;
        }

        private float GetVoxelSize(Bounds bounds)
        {
            Bounds b = bounds;// CalculateBounds(bounds);
            int longestAxis = LongestAxis(b.size);
            return (b.size[longestAxis]) / resolution;
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
            Bounds b = MovedBounds;
            Gizmos.color = new(1, 0, 0, 0.5f);
            Gizmos.DrawWireCube(b.center, b.size);
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

        public int CompareTo(object obj)
        {
            var a = this;
            var b = obj as SdfTextureField;
            if (b == null)
                return 0;

            if (a.resolution < b.resolution || !a.IsValid)
                return 1;
            if (a.resolution > b.resolution || !b.IsValid)
                return -1;
            return 0;
        }


        private class PropertyIDs
        {
            public static readonly int TextureRead = Shader.PropertyToID("_TextureRead");
            public static readonly int TextureWrite = Shader.PropertyToID("_TextureWrite");
            public static readonly int Resolution = Shader.PropertyToID("_Resolution");
        }
    }
}
