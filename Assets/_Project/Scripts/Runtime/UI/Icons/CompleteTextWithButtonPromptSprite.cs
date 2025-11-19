using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Beakstorm.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;

namespace Beakstorm.UI.Icons
{
    public static class CompleteTextWithButtonPromptSprite
    {
        // capture multiple counts of {Player/Bar} and {Player/Jump}
        // matches lazily to support multiple phrases in one line
        private static string ACTION_PATTERN = @"\{(.*?)\}";
        private static Regex REGEX = new Regex(ACTION_PATTERN, RegexOptions.IgnoreCase);
        
        public static Sprite GetSpriteFromBinding(string actionName, ButtonIcons icons, out Vector4 uv)
        {
            InputBinding dynamicBinding = PlayerInputs.Instance.GetBinding(actionName);
            TMP_SpriteAsset spriteAsset = icons.GetAssetByDevice(PlayerInputs.LastActiveDevice);
        
            string stringButtonName = dynamicBinding.effectivePath;
            stringButtonName = RenameInput(stringButtonName, spriteAsset.name);
            Debug.LogFormat("ActionNeeded: {0}", stringButtonName);

            
            int index = spriteAsset.GetSpriteIndexFromName(stringButtonName);
            Debug.Log($"Index: {index}");
            uv = Vector4.zero;

            if (spriteAsset.spriteGlyphTable.Count <= index || index < 0)
                return null;
            return spriteAsset.spriteGlyphTable[index].sprite;
            
            
            GlyphRect rect = spriteAsset.spriteCharacterTable[index].glyph.glyphRect;

            uv.x = (float) rect.x / spriteAsset.spriteSheet.width;
            uv.y = (float) rect.y / spriteAsset.spriteSheet.height;
            uv.z = (float) rect.width / spriteAsset.spriteSheet.width;
            uv.w = (float) rect.height / spriteAsset.spriteSheet.height;

            return null;//spriteAsset.spriteSheet;
        }

        public static string ReplaceActiveBindings(string textWithActions, PlayerInputs inputs,
            ButtonIcons spriteAssets)
        {
            return ReplaceBindings(textWithActions, inputs, spriteAssets);
        }

        public static string ReplaceBindings(string textWithActions, PlayerInputs inputs,
            ButtonIcons spriteAssets)
        {
            MatchCollection matches = REGEX.Matches(textWithActions);

            // original template
            var replacedText = textWithActions;

            foreach (Match match in matches)
            {
                var withBraces = match.Groups[0].Captures[0].Value;
                var innerPart = match.Groups[1].Captures[0].Value;
                Debug.LogFormat("{0} has {1}", withBraces, innerPart);

                var tagText = GetSpriteTags(innerPart, inputs, spriteAssets);

                replacedText = replacedText.Replace(withBraces, tagText);
            }

            return replacedText;
        }



        /// <summary>
        /// Looks up the InputBinding based on device type and returns the TextMeshPro sprite tag
        /// </summary>
        public static string GetSpriteTag(string actionName, PlayerInputs inputs,
            ButtonIcons spriteAssets)
        {
            InputBinding dynamicBinding = inputs.GetBinding(actionName);
            TMP_SpriteAsset spriteAsset = spriteAssets.GetAssetByDevice(PlayerInputs.LastActiveDevice);

            Debug.LogFormat("Retrieving sprite tag for: {0} with path {1}", dynamicBinding.action,
                dynamicBinding.effectivePath);
            string stringButtonName = dynamicBinding.effectivePath;
            stringButtonName = RenameInput(stringButtonName, spriteAsset.name);

            return $"<sprite name=\"{stringButtonName}\">";
            return $"<sprite=\"{spriteAsset.name}\" name=\"{stringButtonName}\">";
        }
        
        public static string GetSpriteTags(string actionName, PlayerInputs inputs,
            ButtonIcons spriteAssets)
        {
            string output = String.Empty;

            List<InputBinding> bindings = inputs.GetBindings(actionName);

            foreach (InputBinding binding in bindings)
            {
                string bin = GetSpriteTag(binding, inputs, spriteAssets);

                if (string.IsNullOrEmpty(output))
                    output = bin;
                else
                    output = output + "/" + bin;
            }
            
            return output;
        }
        
        public static string GetSpriteTag(InputBinding binding, PlayerInputs inputs,
            ButtonIcons spriteAssets)
        {
            TMP_SpriteAsset spriteAsset = spriteAssets.GetAssetByDevice(PlayerInputs.LastActiveDevice);

            Debug.LogFormat("Retrieving sprite tag for: {0} with path {1}", binding.action,
                binding.effectivePath);
            string stringButtonName = binding.effectivePath;
            stringButtonName = RenameInput(stringButtonName, spriteAsset.name);

            return $"<sprite name=\"{stringButtonName}\">";
        }

        private static string RenameInput(string stringButtonName, string prefix)
        {
            stringButtonName = stringButtonName.Replace("Interact:", String.Empty);

            //stringButtonName = stringButtonName.Replace("<Keyboard>/", "Keyboard_");
            //stringButtonName = stringButtonName.Replace("<Gamepad>/", "Gamepad_");
            
            stringButtonName = stringButtonName.Replace("<", String.Empty);
            stringButtonName = stringButtonName.Replace(">/", "_");

            stringButtonName = Regex.Replace(stringButtonName, @"^[^_]*_", "", RegexOptions.IgnorePatternWhitespace);

            prefix = Regex.Replace(prefix, @"^[^_]*_", "");
            
            stringButtonName = prefix + "_" + stringButtonName;
            
            return stringButtonName;
        }
    }
}