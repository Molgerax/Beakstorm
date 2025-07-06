using UnityEditor;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.Impacts
{
    [CustomEditor(typeof(ImpactSpriteSheet))]
    public class ImpactSpriteSheetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ImpactSpriteSheet spriteSheet = (ImpactSpriteSheet) target;
            base.OnInspectorGUI();

            EditorGUILayout.LabelField("Max Frames", $"{spriteSheet.MaxFrames}");
            EditorGUILayout.LabelField("Sprite Count", $"{spriteSheet.ValidSpriteCount}");
            EditorGUILayout.LabelField("Resolution", $"{spriteSheet.MaxResolution}");
            

            if (GUILayout.Button("Generate"))
            {
                GenerateSpriteSheet(spriteSheet);
            }
        }


        private void GenerateSpriteSheet(ImpactSpriteSheet spriteSheet)
        {
            spriteSheet.GetDimensions();
            AddResultTexture(spriteSheet);
            
            int spriteIndex = 0;
            foreach (ImpactSprite sprite in spriteSheet.Sprites)
            {
                if (!sprite)
                    continue;
                if (!sprite.Sprite)
                    continue;

                CopyFrameFromTexture(spriteSheet, sprite, spriteIndex);
                spriteIndex++;
            }
            
            spriteSheet._resultTexture.Apply();
            AssetDatabase.SaveAssetIfDirty(spriteSheet);
        }


        private void AddResultTexture(ImpactSpriteSheet spriteSheet)
        {
            int width = spriteSheet.MaxFrames * spriteSheet.MaxResolution;
            int height = spriteSheet.ValidSpriteCount * spriteSheet.MaxResolution;

            CreateResultTexture(spriteSheet, width, height);

            if (!AssetDatabase.IsSubAsset(spriteSheet._resultTexture))
            {
                AssetDatabase.AddObjectToAsset(spriteSheet._resultTexture, spriteSheet);
                AssetDatabase.SaveAssetIfDirty(spriteSheet);
            }
        }


        private void CreateResultTexture(ImpactSpriteSheet spriteSheet, int width, int height)
        {
            if (spriteSheet._resultTexture == null)
            {
                foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(spriteSheet)))
                {
                    var subTexture = (Texture2D) o;
                    if (subTexture == null)
                        continue;

                    if (spriteSheet._resultTexture == null)
                        spriteSheet._resultTexture = subTexture;
                    else
                    {
                        AssetDatabase.RemoveObjectFromAsset(subTexture);
                        DestroyImmediate(subTexture);
                    }
                }
                
                if (spriteSheet._resultTexture == null)
                    spriteSheet._resultTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true, true);
            }
            
            if (spriteSheet._resultTexture.width != width || spriteSheet._resultTexture.height != height)
            {
                spriteSheet._resultTexture.Reinitialize(width, height, TextureFormat.RGBA32, false);
            }
            spriteSheet._resultTexture.name = target.name;
            

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    spriteSheet._resultTexture.SetPixel(x, y, Color.clear);
                }
            }
            spriteSheet._resultTexture.Apply();
        }
        
        private void CopyFrameFromTexture(ImpactSpriteSheet spriteSheet, ImpactSprite sprite, int spriteId)
        {
            for (int i = 0; i < spriteSheet.MaxFrames; i++)
            {
                if (i >= sprite.FrameCount)
                    break;
                
                float t = ((float) i / spriteSheet.MaxFrames);

                int sampleI = Mathf.FloorToInt(t * sprite.FrameCount);
                sampleI = i;
                int frameWidth = (sprite.Sprite.width / sprite.FrameCount);
                int widthOffset = frameWidth * sampleI;
                
                bool readWrite = false;
                string path = AssetDatabase.GetAssetPath(sprite.Sprites[sampleI]);
                if (!string.IsNullOrEmpty(path))
                {
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    readWrite = importer.isReadable;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
                
                spriteSheet._resultTexture.CopyPixels(sprite.Sprites[sampleI], 0, 0, 0, 0, sprite.Sprite.width, sprite.Sprite.height,
                    0, i * spriteSheet.MaxResolution, spriteId * spriteSheet.MaxResolution);
                
                if (!string.IsNullOrEmpty(path))
                {
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.isReadable = readWrite;
                
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
