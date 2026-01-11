using UnityEngine;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class SelectableTransitions : MonoBehaviour
    {
        [SerializeField] private UITransitionProfile profile;

        private void OnValidate()
        {
            Apply();
        }

        private void Awake()
        {
            Apply();
        }

        private void Apply()
        {
            if (profile && TryGetComponent(out Selectable selectable))
                profile.Apply(selectable);
        }
    }
}