using UnityEngine;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class DefaultMenuSelectable : MonoBehaviour
    {
        [SerializeField] private Selectable selectable;

        private void OnEnable()
        {
            if (selectable)
                selectable.Select();
        }

        private void Reset()
        {
            if (!selectable)
                selectable = GetComponent<Selectable>();
        }
    }
}
