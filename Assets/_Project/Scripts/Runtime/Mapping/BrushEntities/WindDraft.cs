using System;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("wind_draft", category:"trigger", type: BrushType.Liquid)]
    public class WindDraft : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity
    {
        [SerializeField] private float strength = 10;
        [SerializeField] private bool startsActive = true;
        
        private bool _isActive = false;
        public bool IsActive => _isActive && isActiveAndEnabled;

        public Vector3 Force => IsActive ? transform.right * strength : Vector3.zero;

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _isActive = startsActive;
            SetProperty();
        }

        public void Trigger()
        {
            _isActive = !_isActive;
        }
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            SetProperty();
        }

        private void SetProperty()
        {
            if (TryGetComponent(out MeshRenderer meshRenderer))
            {
                _propertyBlock ??= new();
                meshRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat("_Speed", strength);
                meshRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
