using DynaMak.Volumes.FluidSimulation;
using UnityEngine;
using UnityEditor;

namespace DynaMak.FluidSimulation.FluidAddOperators.Editor
{
    [CustomEditor(typeof(FluidFieldAddSphere))]
    public class FluidFieldAddSphereEditor : UnityEditor.Editor
    {

        #region Gizmos

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosNotSelected(FluidFieldAddSphere adder, GizmoType gizmoType)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(adder.transform.position, adder.Radius);

            Handles.color = new Color(0,0,0,0.25f);
            Handles.ArrowHandleCap(0, adder.transform.position, adder.transform.rotation, adder.Strength, EventType.Repaint);
        }
        
        
        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmosSelected(FluidFieldAddSphere adder, GizmoType gizmoType)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.DrawWireSphere(adder.transform.position, adder.Radius);

            Handles.color = new Color(0,0,0,1f);
            Handles.ArrowHandleCap(0, adder.transform.position, adder.transform.rotation, adder.Strength, EventType.Repaint);
        }
        #endregion
    }
}