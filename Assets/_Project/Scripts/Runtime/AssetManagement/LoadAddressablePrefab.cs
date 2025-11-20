using Beakstorm.UI.Menus;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Beakstorm.AssetManagement
{
    public class LoadAddressablePrefab : MonoBehaviour
    {
        [SerializeField] private string addressableName;
        [SerializeField] private bool onAwake;
        
        private GameObject _loaded;

        private void Awake()
        {
            if (onAwake)
            {
                if (LoadAllMapsToDropdown.SelectedMapLocation != null)
                {
                    LoadByLocation(LoadAllMapsToDropdown.SelectedMapLocation);
                }
                else
                {
                    LoadByKey(addressableName);
                }
            }
        }

        public void LoadByKey(string key)
        {
            if (_loaded)
                return;
            
            _loaded = Addressables.InstantiateAsync(key).WaitForCompletion();
        }

        public void LoadByLocation(IResourceLocation location)
        {
            if (_loaded)
                return;
           
            _loaded = Addressables.InstantiateAsync(location).WaitForCompletion();
        }


        private void OnDestroy()
        {
            if (_loaded)
                Addressables.ReleaseInstance(_loaded);
        }
    }
}
