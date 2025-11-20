using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Beakstorm.UI.Menus
{
    public class LoadAllMapsToDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private AssetLabelReference label;

        public static IResourceLocation SelectedMapLocation;

        private IList<IResourceLocation> _keys;

        private void Start()
        {
            _keys ??= new List<IResourceLocation>();
            _keys.Clear();
            
            var keys = Addressables.LoadResourceLocationsAsync(label).WaitForCompletion();

            dropdown.options ??= new();
            dropdown.options.Clear();
            
            foreach (IResourceLocation key in keys)
            {
                if (key.ResourceType != typeof(GameObject))
                    continue;
                
                _keys.Add(key);
                string displayName = Path.GetFileName(key.PrimaryKey);
                
                dropdown.options.Add(new (displayName));
            }
            
            SelectedMapLocation = _keys[dropdown.value];
            dropdown.onValueChanged.AddListener(OnSelectMap);
        }

        private void OnSelectMap(int value)
        {
            if (value < _keys.Count)
                SelectedMapLocation = _keys[value];
        }
    }
}
