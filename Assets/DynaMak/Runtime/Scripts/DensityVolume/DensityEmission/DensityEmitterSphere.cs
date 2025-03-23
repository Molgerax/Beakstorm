using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynaMak.Volumes
{
    public class DensityEmitterSphere : DensityEmitterBase
    {
        #region Serialize Fields

        [Header("Emission Settings")] [SerializeField] [Range(0f, 5f)]
        protected float _radius = 1f;

        #endregion

        #region Private Fields

        #endregion

        #region Shader Property IDs

        private int radiusID = Shader.PropertyToID("_Radius");

        #endregion

        // -------------------------------

        #region Override Functions

        protected override void SetOtherComputeValues()
        {
            _computeShader.SetFloat(radiusID, _radius);
        }

        #endregion


        #region Editor

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }

        #endregion
    }
}