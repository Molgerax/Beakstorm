using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Beakstorm.AssetManagement
{
    public class InitializeAddressables : MonoBehaviour
    {
        private void Awake()
        {
            Addressables.InitializeAsync().WaitForCompletion();
            
            Debug.LogError("Wake up!");
        }
    }
}
