using UnityEngine;

namespace DynaMak.Volumes
{
    public class DensityEmitterBox : DensityEmitterBase
    {
        #region Serialize Fields

        #endregion

        #region Private Fields

        #endregion

        #region Shader Property IDs

        private int boxBoundsID = Shader.PropertyToID("_BoxBounds");
        private int boxRightID = Shader.PropertyToID("_BoxAxisX");
        private int boxUpID = Shader.PropertyToID("_BoxAxisY");


        #endregion

        // -------------------------------

        #region Override Functions

        protected override void SetOtherComputeValues()
        {
            _computeShader.SetVector(boxBoundsID, transform.localScale);
            _computeShader.SetVector(boxRightID, transform.right);
            _computeShader.SetVector(boxUpID, transform.up);
        }

        #endregion


        #region Editor

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }

        #endregion
    }
}