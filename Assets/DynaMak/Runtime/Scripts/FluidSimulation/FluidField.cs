using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynaMak.Volumes.FluidSimulation
{
    public class FluidField : VolumeComponent
    {
        #region Serialize Fields

        [Header("Field Settings")]
        [SerializeField] private Vector3Int resolution = Vector3Int.one * 32;
        [SerializeField] private bool enable;

        [Header("Operator Settings")] 
        [SerializeField] private FluidFieldOperator[] staticFluidFieldOperators;

        #endregion

        #region Public Fields

        public VolumeTexture FluidTexture;
        
        #endregion
        
        // -------------------

        #region Override Functions
        public override VolumeTexture GetVolumeTexture()
        {
            return FluidTexture;
        }
        
        public override Vector3 VolumeCenter => GetVolumeTexture().IsInitialized ? base.VolumeCenter : transform.position;
        public override Vector3 VolumeBounds => GetVolumeTexture().IsInitialized ? base.VolumeBounds : transform.localScale;
        public override Vector3Int VolumeResolution => GetVolumeTexture().IsInitialized ? base.VolumeResolution : resolution;
        
        #endregion


        #region Mono Methods

        private void Awake()
        {
            FluidTexture = new VolumeTexture(RenderTextureFormat.ARGBHalf, resolution, transform.position,
                transform.localScale);
            FluidTexture.Initialize();

            
            if (staticFluidFieldOperators is null || staticFluidFieldOperators.Length == 0)
            {
                staticFluidFieldOperators = GetComponents<FluidFieldOperator>();
            }
        }


        private void Update()
        {
            if (enable)
            {
                ApplyDynamicOperators();
                ApplyStaticOperators();
            }
        }


        private void OnDisable()
        {
            //FluidTexture.Release();
        }

        private void OnDestroy()
        {
            FluidTexture.Release();
        }

        private void Reset()
        {
            staticFluidFieldOperators = GetComponents<FluidFieldOperator>();
        }

        #endregion



        #region Dynamic Operator Subscription

        [SerializeField, HideInInspector] private List<FluidFieldOperator> dynamicFluidOperators = new List<FluidFieldOperator>();
        
        public void AddOperator(FluidFieldOperator fieldOperator)
        {
            dynamicFluidOperators.Add(fieldOperator);
        }

        public void RemoveOperator(FluidFieldOperator fieldOperator)
        {
            dynamicFluidOperators.Remove(fieldOperator);
        }

        public void ApplyDynamicOperators()
        {
            if(dynamicFluidOperators is null) return;
            
            for (int i = dynamicFluidOperators.Count - 1; i >= 0; i--)
            {
                if (dynamicFluidOperators[i] is null)
                {
                    dynamicFluidOperators.RemoveAt(i);
                    continue;
                }
                
                dynamicFluidOperators[i].ApplyOperation(FluidTexture);
            }
        }

        #endregion
        
        
        
        #region Private Methods

        private void ApplyStaticOperators()
        {
            if(staticFluidFieldOperators is null) return;
            
            for (int i = 0; i < staticFluidFieldOperators.Length; i++)
            {
                if(staticFluidFieldOperators[i]) staticFluidFieldOperators[i].ApplyOperation(FluidTexture);
            }
        }

        #endregion
    }
}