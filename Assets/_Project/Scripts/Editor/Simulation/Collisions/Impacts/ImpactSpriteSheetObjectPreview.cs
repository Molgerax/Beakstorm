using UnityEditor;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.Impacts
{
    [CustomPreview(typeof(ImpactSpriteSheet))]
    public class ImpactSpriteSheetObjectPreview : ObjectPreview
    {
        private Editor _preview;

        public override void Initialize(Object[] targets)
        {
            base.Initialize(targets);
            
            if (targets.Length > 1)
                return;

            ImpactSpriteSheet sheet = (ImpactSpriteSheet) target;
            if (sheet._resultTexture)
                _preview = Editor.CreateEditor(sheet._resultTexture);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_preview != null)
                Object.DestroyImmediate(_preview);
        }

        public override bool HasPreviewGUI()
        {
            return _preview != null && _preview.HasPreviewGUI();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            GUI.Label(r, target.name);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);
            
            if (_preview != null)
                _preview.OnInteractivePreviewGUI(r, background);
        }
    }
}
