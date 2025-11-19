using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.UI.Icons
{
    [CreateAssetMenu(menuName = "Beakstorm/Input/ButtonIcons")]
    public class ButtonIcons : ScriptableObject
    {
        [SerializeField] public List<TMP_SpriteAsset> spriteAssets;
        
        public TMP_SpriteAsset GetAssetByDevice(InputDevice device)
        {
            if (device == null)
                return spriteAssets[0];
        
            List<string> names = new List<string>();

            Type deviceType = device.GetType();
            while (deviceType.IsSubclassOf(typeof(InputDevice)))
            {
                if (deviceType != typeof(InputDevice))
                    names.Add(deviceType.Name);

                deviceType = deviceType.BaseType;
            }

            for (int i = 0; i < names.Count; i++)
            {
                string currentName = names[i];

                for (var index = 0; index < spriteAssets.Count; index++)
                {
                    TMP_SpriteAsset asset = spriteAssets[index];
                    if (asset.name.Contains(currentName))
                    {
                        return asset;
                    }
                }
            }
            
            return spriteAssets[0];
        }
    }
}