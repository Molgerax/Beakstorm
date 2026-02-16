using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
    [CustomEditor(typeof(TrembleColorData))]
    public class TrembleColorDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TrembleColorData colorData = (TrembleColorData) target;

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
                SaveChanges();
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
                    data.pairs.Add(new TrembleColorData.DataPair(type, Color.white));
            }
        }
    }
}