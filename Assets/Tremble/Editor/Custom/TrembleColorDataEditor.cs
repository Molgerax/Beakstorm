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
        private SerializedProperty _pairs;

        private void OnEnable()
        {
            serializedObject.FindProperty("pairs");
        }

        public override void OnInspectorGUI()
        {
            TrembleColorData colorData = (TrembleColorData) target;
            
            for (var index = 0; index < colorData.pairs.Count; index++)
            {
                //SerializedProperty prop = _pairs.GetArrayElementAtIndex(index);
                //SerializedProperty colorProp = prop.FindPropertyRelative("Color");
                
                var pair = colorData.pairs[index];
                if (pair == null)
                    continue;

                if (pair.Type.IsNullOrEmpty())
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.Type);

                EditorGUI.BeginChangeCheck();
                
                Color newCol = EditorGUILayout.ColorField(pair.Color);
                pair.Color = newCol;

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Generate"))
            {
                Generate(colorData);
                EditorUtility.SetDirty(target);
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
                    
                    if (type.GetCustomAttributes(typeof(PrefabEntityAttribute)).FirstOrDefault() is PrefabEntityAttribute prefabEntityAttribute)
                    {
                        string baseName = prefabEntityAttribute.TrenchBroomName ?? type.Name.ToNamingConvention(typeNamingConvention);
                        string pointPrefix = prefabEntityAttribute.Category ?? FgdConsts.POINT_PREFIX;
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