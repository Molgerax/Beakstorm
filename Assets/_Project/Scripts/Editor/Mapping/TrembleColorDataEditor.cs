using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TinyGoose.Tremble;
using UnityEditor;
using UnityEngine;

namespace Beakstorm.Mapping.Editor
{
    [CustomEditor(typeof(TrembleColorData))]
    public class TrembleColorDataEditor : UnityEditor.Editor
    {
        private const string PATH = "Assets/_Generated/Code/" + nameof(TrembleColorData) + ".cs";
        
        public override void OnInspectorGUI()
        {
            TrembleColorData colorData = (TrembleColorData) target;
            //base.OnInspectorGUI();

            EditorGUI.BeginChangeCheck();

            foreach (var pair in colorData.pairs)
            {
                if (pair == null)
                    continue;
                
                if (pair.Type == null || pair.Type.Type == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.Type.Type.Name);
                
                Color newCol = EditorGUILayout.ColorField(pair.Color);
                pair.Color = newCol;
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (EditorGUI.EndChangeCheck())
                SaveChanges();

            if (GUILayout.Button("Generate"))
            {
                Generate(colorData);
                GenerateCode(colorData);
            }
        }


        private void Generate(TrembleColorData data)
        {
            HashSet<Type> entityTypes = new();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (type.GetCustomAttributes(typeof(PointEntityAttribute)).FirstOrDefault() is PointEntityAttribute pointEntityAttribute)
                        entityTypes.Add(type);
                }
            }

            // Check if any data pairs are no longer in scripts
            for (var index = data.pairs.Count - 1; index >= 0; index--)
            {
                TrembleColorData.DataPair dataPair = data.pairs[index];
                
                if (dataPair?.Type?.Type == null)
                {
                    data.pairs.RemoveAt(index);
                    continue;
                }
                
                if (!entityTypes.Contains(dataPair.Type))
                    data.pairs.RemoveAt(index);
                
            }

            // make a new entry for each new type
            foreach (Type type in entityTypes)
            {
                bool exists = false;
                foreach (var dataPair in data.pairs)
                {
                    if (dataPair.Type == type)
                        exists = true;
                }
                if (!exists)
                    data.pairs.Add(new TrembleColorData.DataPair(type, Color.gray));
            }
        }

        private void GenerateCode(TrembleColorData colorData)
        {
            string path = GetPath(PATH);

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("namespace Beakstorm.Mapping\n{\npublic static class TrembleColors\n{\n");

            foreach (var pair in colorData.pairs)
            {
                WriteLine(stringBuilder, pair);
            }
            
            stringBuilder.Append("\n}\n}");
            
            string text = stringBuilder.ToString();
            
            if (!File.Exists(path))
            {
                File.WriteAllText(path, text, Encoding.UTF8);
            }
            else
            {
                using (var writer = new StreamWriter(path, false))
                {
                    writer.WriteLine(text);
                }
            }
            
            AssetDatabase.ImportAsset(PATH);
        }

        private void WriteLine(StringBuilder builder, TrembleColorData.DataPair dataPair)
        {
            builder.AppendLine($"public const string {dataPair.Type.Type.Name} = \"{dataPair.Color.ToStringInvariant(2)}\";");
        }
        
        static string GetPath(string folderPath)
        {
            string fullPath = Application.dataPath + "/" + Path.GetRelativePath("Assets", folderPath);

            return fullPath;
        }
    }
}