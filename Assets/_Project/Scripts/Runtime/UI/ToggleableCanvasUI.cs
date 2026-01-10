using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.UI
{
    [RequireComponent(typeof(Canvas))]
    public class ToggleableCanvasUI : MonoBehaviour
    {
        private static HashSet<ToggleableCanvasUI> _canvases = new();

        private static bool _culled = false;

        public static bool Culled
        {
            get => _culled;
            set
            {
                if (_culled == value)
                    return;

                _culled = value;
                foreach (ToggleableCanvasUI ui in _canvases)
                {
                    ui.SetCulled(_culled);
                }
            }
        }
        
        [SerializeField, HideInInspector] private Canvas canvas;

        private void Reset()
        {
            canvas = GetComponent<Canvas>();
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            _canvases.Add(this);
            SetCulled(_culled);
        }

        private void OnDisable()
        {
            _canvases.Remove(this);
        }

        private void SetCulled(bool value)
        {
            canvas.enabled = !value;
        }
    }
}