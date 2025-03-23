using System;
using System.Reflection;
using DynaMak.Particles;
using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [System.Serializable]
    public abstract class DynaPropertyBinderBase : MonoBehaviour, IDynaProperty, IArrayElementTitle
    {
        #region Serialize Fields

        /// <summary>
        /// Name of the property inside the compute shader
        /// </summary>
        [SerializeField] public string PropertyName;
        
        public string Name => PropertyName;
        public void SetPropertyName(string propertyName)
        {
            PropertyName = propertyName;
            SetPropertyIDs();
        }
        
        
        /// <summary>
        /// ID of the property
        /// </summary>
        [SerializeField, HideInInspector] protected int _propertyID;

        
        /// <summary>
        /// Set true to subscribe to <see cref="particlesToSubscribe"/> on OnEnable / OnDisable.
        /// Will only display and enable this option, if there are no <see cref="DynaParticleComponent"/> on the same GameObject.
        /// </summary>
        [SerializeField] private bool subscribeToParticles = false;
        
        /// <summary>
        /// <see cref="DynaParticleComponent"/> to subscribe to.
        /// </summary>
        [SerializeField] private DynaParticleComponent particlesToSubscribe;

        public bool SubscribeToParticles => subscribeToParticles;
        public DynaParticleComponent ParticlesToSubscribe => particlesToSubscribe;

        
        /// <summary>
        /// Property used for editor gizmos, to show the transforms of attached components.
        /// </summary>
        public abstract Component ValueAsComponent { get; }

        #endregion
        
        

        #region Mono Methods

        private void Awake()
        {
            SetPropertyIDs();
            Initialize();
        }

        private void OnDestroy()
        {
            Release();
        }


        private void OnEnable()
        {
            if(subscribeToParticles)
                SubscribeToParticle();
        }
        
        private void OnDisable()
        {
            if(subscribeToParticles )
                UnsubscribeFromParticle();
        }

        #endregion


        #region Subscription Functions

        /// <summary>
        /// Subscribes to a particle component. Dynamically subscribed PropertyBinders are executed after the static PropertyBinders
        /// are called and can overwrite their values.
        /// </summary>
        /// <param name="dynaParticleComponent">DynaParticleComponent to subscribe to.</param>
        public void SubscribeToParticle(DynaParticleComponent dynaParticleComponent)
        {
            if(dynaParticleComponent is null) return;
            
            dynaParticleComponent.AddProperty(this);
        }
        
        /// <summary>
        /// Subscribes to <see cref="particlesToSubscribe"/>. Dynamically subscribed PropertyBinders are executed after the static PropertyBinders
        /// are called and can overwrite their values.
        /// </summary>
        public void SubscribeToParticle() => SubscribeToParticle(particlesToSubscribe);
        
        
        
        
        /// <summary>
        /// Unsubscribes from a particle component.
        /// </summary>
        /// <param name="dynaParticleComponent">DynaParticleComponent to unsubscribe from.</param>
        public void UnsubscribeFromParticle(DynaParticleComponent dynaParticleComponent)
        {
            if(dynaParticleComponent is null) return;

            dynaParticleComponent.RemoveProperty(this);
        }
        
        /// <summary>
        /// Unsubscribes from <see cref="particlesToSubscribe"/>.
        /// </summary>
        public void UnsubscribeFromParticle() => UnsubscribeFromParticle(particlesToSubscribe);
        
        

        #endregion

        
        
        #region Overridable Functions

        /// <summary>
        /// Sets property IDs according to name 
        /// </summary>
        protected virtual void SetPropertyIDs()
        {
            _propertyID = Shader.PropertyToID(PropertyName);
        }
        
        
        /// <summary>
        /// Sets the property represented by this object inside the target compute shader and kernel.
        /// </summary>
        /// <param name="cs">Compute shader to set property in</param>
        /// <param name="kernelIndex">ID of the kernel to write property in (needed for buffers, textures, etc.)</param>
        public abstract void SetProperty(ComputeShader cs, int kernelIndex);
        

        
        /// <summary>
        /// Initializes possible buffers, etc.
        /// </summary>
        public virtual void Initialize() { }
        
        /// <summary>
        /// Release possible Buffers, etc.
        /// </summary>
        public virtual void Release() {}

        #endregion
        
        
        
        #region Editor
        
        /// <summary>
        /// Key used for the HLSL lookup table in <see cref="DynaParticleFileReader"/>
        /// </summary>
        public virtual string[] DictKeys => null;

        /// <summary>
        /// The offset between symbols in the HLSL file. Default types always have an offset of 1, macros usually 2.
        /// Example:
        /// float | _Name               has an offset of 1.
        /// TRANSFORM | ( | _Name | )   has an offset of 2, because there is a ( symbol in-between
        /// </summary>
        public virtual int DictParsingOffset => 1;


        protected void OnValidate()
        {
            //SetPropertyIDs();
        }

        #endregion
    }


    [System.Serializable]
    public abstract class DynaPropertyBinderBase<T> : DynaPropertyBinderBase
    {
        public T Value;

        public void SetValue(T value) => Value = value;

        public override Component ValueAsComponent
        {
            get
            {
                if (Value == null) return null;
                if(Value.GetType().IsSubclassOf(typeof(Component))) return Value as Component;
                return null;
            }
        }
    }
}
