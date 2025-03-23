using System;
using System.Collections.Generic;
using DynaMak.Properties;
using DynaMak.Utility;
using UnityEditor;
using UnityEngine;

namespace DynaMak.Particles
{
    public class DynaParticleEmitter : MonoBehaviour, IDynaPropertyUser
    {
        #region Serialize Fields

        [SerializeField] private bool enable;
        [SerializeField] private float particlesPerSecond;
        
        [SerializeField] private DynaParticleComponent particleComponent;
        public DynaParticleComponent ParticleComponent => particleComponent;
        
        
        [SerializeField]
        [ArrayElementTitle] private DynaPropertyBinderBase[] dynaProperties;

        #endregion

        #region Private Fields

        private float _emissionTimeCounter;

        #endregion


        #region Mono Methods

        private void Update()
        {
            if (enable)
            {
                int emissionCount = EmissionTimer(particlesPerSecond);
                
                if (emissionCount > 0)
                    Emit(emissionCount);
            }
        }

        #endregion
        

        #region Public Methods
        
        private void Emit(int count)
        {
            if(!particleComponent) return;
            
            SetProperties();
            particleComponent.DispatchEmit(count, false);
        }

        #endregion

        
        #region Private Methods

        private void SetProperties()
        {
            if(dynaProperties is null) return;
            if(!particleComponent) return;

            foreach (DynaPropertyBinderBase property in dynaProperties)
            {
                property.SetProperty(particleComponent.ComputeShader, particleComponent.EmitKernel);
            }
        }

        
        
        protected int EmissionTimer(float emissionPerSecond)
        {
            _emissionTimeCounter += Time.deltaTime;
            if (emissionPerSecond <= 0f) return 0;
            
            int o = Mathf.FloorToInt(_emissionTimeCounter * emissionPerSecond);
            if (_emissionTimeCounter * emissionPerSecond >= 1f)
            {
                _emissionTimeCounter = 0f;
            }
            return o;
        }


        #endregion

        
        
        
        
        #region Editor

#if UNITY_EDITOR
        
        
        
#endif
        #endregion
    }
}
