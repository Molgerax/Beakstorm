using System.Collections.Generic;
using UnityEngine;

namespace DynaMak.Volumes.SDF
{
    [System.Serializable]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class SDFColliderVolume : VolumeComponent
    {

        #region Serialize Fields

        [Header("SDF Settings")] 
        [SerializeField] private Vector3Int sdfResolution = Vector3Int.one * 16;
        [SerializeField] private bool enable;

        [Header("References")] 
        [SerializeField] private ComputeShader _computeShader;

        #endregion


        #region Private Fields

        private Vector3 sdfCenter;
        private Vector3 sdfBounds;

        private List<BoxCollider> _boxColliders;
        private List<SphereCollider> _sphereColliders;
        private List<CapsuleCollider> _capsuleColliders;
        private List<TorusCollider> _torusColliders;

        private Box[] _boxes;
        private Sphere[] _spheres;
        private Line[] _lines;
        private Torus[] _tori;

        #endregion

        #region Public Fields

        public SDFVolume SDFVolume;

        #endregion

        // -------------------------
        
        #region Override Functions
        public override VolumeTexture GetVolumeTexture()
        {
            return SDFVolume;
        }
        
        public override Vector3 VolumeCenter => GetVolumeTexture().IsInitialized ? base.VolumeCenter : transform.position;
        public override Vector3 VolumeBounds => GetVolumeTexture().IsInitialized ? base.VolumeBounds : transform.localScale;
        public override Vector3Int VolumeResolution => GetVolumeTexture().IsInitialized ? base.VolumeResolution : sdfResolution;
        
        #endregion
        
        // -------------------------

        #region Mono Methods

        void Awake()
        {
            _sphereColliders = new List<SphereCollider>();
            _boxColliders = new List<BoxCollider>();
            _capsuleColliders = new List<CapsuleCollider>();
            _torusColliders = new List<TorusCollider>();

            SDFVolume = new SDFVolume(_computeShader, sdfResolution, sdfCenter, sdfBounds);
            SDFVolume.Initialize();
            
            UpdateTransforms();
        }

        private void Update()
        {
            if (enable) Recalculate();
        }


        private void OnTriggerEnter(Collider other)
        {
            TorusCollider t = other.GetComponent<TorusCollider>();
            if (t) _torusColliders.Add(t);
            else
            {
                if (other.GetType() == typeof(BoxCollider)) _boxColliders.Add((BoxCollider) other);
                if (other.GetType() == typeof(SphereCollider)) _sphereColliders.Add((SphereCollider) other);
                if (other.GetType() == typeof(CapsuleCollider)) _capsuleColliders.Add((CapsuleCollider) other);
            }

            Recalculate();
        }

        private void OnTriggerExit(Collider other)
        {
            TorusCollider t = other.GetComponent<TorusCollider>();
            if (t) _torusColliders.Remove(t);
            else
            {
                if (other.GetType() == typeof(BoxCollider)) _boxColliders.Remove((BoxCollider) other);
                if (other.GetType() == typeof(SphereCollider)) _sphereColliders.Remove((SphereCollider) other);
                if (other.GetType() == typeof(CapsuleCollider)) _capsuleColliders.Remove((CapsuleCollider) other);
            }

            Recalculate();
        }


        private void OnDestroy()
        {
            SDFVolume.Release();
        }

        private void OnDisable()
        {
            //SDFVolume.Release();
        }

        #endregion

        #region Public Functions

        [ContextMenu("Recalculate")]
        public void Recalculate()
        {
            UpdateTransforms();
            CollidersToSDF();
            SDFVolume.ComputeSDF();
        }

        public void CollidersToSDF()
        {
            ListsToArrays();

            SDFVolume.SetBoxes(_boxes);
            SDFVolume.SetSpheres(_spheres);
            SDFVolume.SetLines(_lines);
            SDFVolume.SetTori(_tori);
        }

        #endregion

        #region Private Functions

        void UpdateTransforms()
        {
            sdfCenter = transform.position;
            sdfBounds = transform.localScale;
            SDFVolume.SetTransforms(sdfCenter, sdfBounds);
        }

        void ListsToArrays()
        {
            int len = _boxColliders.Count;
            _boxes = new Box[len];
            for (int i = 0; i < len; i++)
            {
                Vector3 scaledSize = new Vector3(_boxColliders[i].size.x * _boxColliders[i].transform.lossyScale.x,
                    _boxColliders[i].size.y * _boxColliders[i].transform.lossyScale.y,
                    _boxColliders[i].size.z * _boxColliders[i].transform.lossyScale.z);
                _boxes[i] = new Box(_boxColliders[i].bounds.center, scaledSize * 0.5f, _boxColliders[i].transform.right,
                    _boxColliders[i].transform.up);
            }

            len = _sphereColliders.Count;
            _spheres = new Sphere[len];
            for (int i = 0; i < len; i++)
            {
                if (_sphereColliders[i])
                {
                    _spheres[i] = new Sphere(_sphereColliders[i].bounds.center, _sphereColliders[i].bounds.extents.y);
                }
                else
                {
                    _sphereColliders.Remove(_sphereColliders[i]);
                }
            }

            len = _capsuleColliders.Count;
            _lines = new Line[len];
            for (int i = 0; i < len; i++)
            {
                float radius = _capsuleColliders[i].radius * Mathf.Max(_capsuleColliders[i].transform.localScale.x,
                    _capsuleColliders[i].transform.localScale.z);
                Vector3 height = _capsuleColliders[i].transform.up * Mathf.Max(0,
                    _capsuleColliders[i].height * _capsuleColliders[i].transform.localScale.y * 0.5f - radius);
                _lines[i] = new Line(_capsuleColliders[i].bounds.center - height,
                    _capsuleColliders[i].bounds.center + height, radius);
            }

            len = _torusColliders.Count;
            _tori = new Torus[len];
            for (int i = 0; i < len; i++)
            {
                _tori[i] = new Torus(_torusColliders[i].center, _torusColliders[i].normal, _torusColliders[i].radius,
                    _torusColliders[i].thickness);
            }
        }

        #endregion
    }
}