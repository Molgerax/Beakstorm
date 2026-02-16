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
                
                if (pair.Type.IsNullOrEmpty())
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.Type);
                
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
            HashSet<string> entityTypes = new();
            
            TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
            NamingConvention typeNamingConvention = syncSettings.TypeNamingConvention;
            
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (type.GetCustomAttributes(typeof(PointEntityAttribute)).FirstOrDefault() is PointEntityAttribute pointEntityAttribute)
                    {
                        string baseName = pointEntityAttribute.TrenchBroomName ?? type.Name.ToNamingConvention(typeNamingConvention);
                        string pointPrefix = pointEntityAttribute.Category ?? FgdConsts.POINT_PREFIX;
                        string fullPointName = (pointPrefix.Length == 0) ? baseName : $"{pointPrefix}_{baseName}";
                        
                        entityTypes.Add(fullPointName);
                    }
                }
            }

            // Check if any data pairs are no longer in scripts
            for (var index = data.pairs.Count - 1; index >= 0; index--)
            {
                TrembleColorData.DataPair dataPair = data.pairs[index];
                
                if (dataPair?.Type.IsNullOrEmpty() ?? true)
                {
                    data.pairs.RemoveAt(index);
                    continue;
                }
                
                if (!entityTypes.Contains(dataPair.Type))
                    data.pairs.RemoveAt(index);
                
            }

            // make a new entry for each new type
            foreach (string type in entityTypes)
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
            
            data.pairs.Sort((s1, s2) => String.Compare(s1.Type, s2.Type, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}