using UnityEngine;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    [CreateAssetMenu(fileName = "UITransitionProfile", menuName = "Beakstorm/UI/UI Transition Profile")]
    public class UITransitionProfile : ScriptableObject
    {
        [SerializeField] private Selectable.Transition transition = Selectable.Transition.ColorTint;
        [SerializeField] private ColorBlock colors;
        [SerializeField] private SpriteState spriteState;

        public void Apply(Selectable selectable)
        {
            selectable.transition = transition;
            selectable.colors = colors;
            selectable.spriteState = spriteState;
        }
    }
}