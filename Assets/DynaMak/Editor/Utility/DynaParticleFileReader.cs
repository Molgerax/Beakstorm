using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DynaMak.Properties;
using DynaMak.Utility;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;


namespace DynaMak.Editors.Utility
{
    public static class DynaParticleFileReader
    {
        #region Shader File Extension Validation
        
        public static readonly string[] ValidFileExtensions =
        {
            ".hlsl", ".cginc", ".compute", ".shader"
        };
        
        public static bool IsShaderFile(this UnityEngine.Object o)
        {
            return o.HasFileExtension(ValidFileExtensions);
        }
        
        #endregion

        #region Lookup Tables

        public static readonly Dictionary<string, int> DataTypeSizeDatabase
            = new Dictionary<string, int>
            {
                { "bool", 4 },
                
                { "int", 4 },
                { "uint", 4 },
                { "half", 4 },
                { "float", 4 },
                { "double", 8 },
                
                { "int2", 8 },
                { "uint2", 8 },
                { "half2", 8 },
                { "float2", 8 },
                { "double2", 16 },
                
                { "int3", 12 },
                { "uint3", 12 },
                { "half3", 12 },
                { "float3", 12 },
                { "double3", 24 },
                
                { "int4", 16 },
                { "uint4", 16 },
                { "half4", 16 },
                { "float4", 16 },
                { "double4", 24 },
                
                { "float2x2", 16 },
                { "float3x3", 36 },
                { "float4x4", 64 },
            };

        
        
        public static Dictionary<string, Type> PropertyToPropertyTypeDatabase
            = new Dictionary<string, Type>
            {
                
            };
                
        #endregion


        #region Populate Property Dictionary
        
        [MenuItem("DynaMak/Refresh Dictionary")]
        public static void RefreshDictionary()
        {
            PropertyToPropertyTypeDatabase = new Dictionary<string, Type>();
            RetrievePropertyTypeDatabase();
            //LogDictionary<string, Type>(PropertyToPropertyTypeDatabase);
        }


        [UnityEditor.Callbacks.DidReloadScripts]
        private static void RefreshDictionaryOnCompiled()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RefreshDictionaryOnCompiled;
                return;
            }
            EditorApplication.delayCall += RefreshDictionary;
        }
        
        
        public static void RetrievePropertyTypeDatabase()
        {
            List<Type> dynaPropertyTypes =
                DerivedType.GetDerivedTypes(typeof(DynaPropertyBinderBase), Assembly.GetAssembly(typeof(DynaPropertyBinderBase)));

            for (int i = 0; i < dynaPropertyTypes.Count; i++)
            {
                Type type = dynaPropertyTypes[i];
                if(type.ContainsGenericParameters) continue;
                
                PropertyInfo propertyInfo = type.GetProperty(nameof(DynaPropertyBinderBase.DictKeys));
                if(propertyInfo is null) continue;

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.hideFlags = HideFlags.HideAndDontSave;
                DynaPropertyBinderBase instance = go.AddComponent(type) as DynaPropertyBinderBase;
                string[] keys = propertyInfo.GetValue(instance) as string[];
                Object.DestroyImmediate(go);
                
                
                if(keys is null) continue;
                for (int j = 0; j < keys.Length; j++)
                {
                    PropertyToPropertyTypeDatabase.Add(keys[j], dynaPropertyTypes[i]);
                }
            }
        }

        public static int GetPropertyOffset(Type type)
        {
            int offset = 1;
            
            if (!type.IsSubclassOf(typeof(DynaPropertyBinderBase)) || type.ContainsGenericParameters) return offset;
            
            PropertyInfo propertyInfo = type.GetProperty(nameof(DynaPropertyBinderBase.DictParsingOffset));
            if(propertyInfo is null) return offset;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.hideFlags = HideFlags.HideAndDontSave;
            DynaPropertyBinderBase instance = go.AddComponent(type) as DynaPropertyBinderBase;
            offset = (int)propertyInfo.GetValue(instance);
            Object.DestroyImmediate(go);

            return offset;
        }

        [MenuItem("DynaMak/Read Dictionary")]
        public static void ReadDict()
        {
            LogDictionary<string, Type>(PropertyToPropertyTypeDatabase);
        }
        
        public static void LogDictionary<T1, T2>(Dictionary<T1, T2> dictionary)
        {
            string output = String.Empty;
            foreach (KeyValuePair<T1,T2> keyValuePair in dictionary)
            {
                output += $"{keyValuePair.Key}: {keyValuePair.Value}\n";
            }
            Debug.Log(output);
        }

        #endregion
        
        /// <summary>
        /// Gets the path to the particle struct in the given file. Usually returns the path of the given file.
        /// If no definition for a struct is found, it is being looked for in the first level of #include file based on file name alone. 
        /// </summary>
        /// <param name="shaderFile">Shader file to be checked.</param>
        /// <returns>Path to the shader file including a particle struct.</returns>
        public static string GetStructDefinePath(Object shaderFile)
        {
            if (shaderFile == null)
            {
                Debug.LogError($"Shader File Object is null.");
                return null;
            }
            
            string path = AssetDatabase.GetAssetPath(shaderFile);
            string text = File.ReadAllText(path);

            // Excluding wrong files
            if (!shaderFile.HasFileExtension(ValidFileExtensions))
            {
                Debug.LogError("Object is not a shader file.");
                return null;
            }
            
            
            
            char[] seperators = { '\n' };
            string[] strValues = text.Split(seperators);

            
            if (!text.Contains("#define PARTICLE_STRUCT", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string str in strValues)
                {
                    if (str.Contains("#include") && str.Contains("Particle"))
                    {
                        string fileName = FindStringBetweenQuotes(str);
                        
                        string oldFilename = path.Split('/').Last();
                        path = path.Replace( oldFilename, String.Empty);
                        string newPath = path + fileName;

                        newPath = newPath.Replace(" ", String.Empty);
                        return newPath;
                    }
                }

                return null;
            }

            return path;
        }

        public static string FindStringBetweenQuotes(string str)
        {
            foreach (Match match in Regex.Matches(str, "\".*?\""))
            {
                return match.ToString().Replace("\"", String.Empty);
            }

            return null;
        }

        #region File Reader Functions

        /// <summary>
        /// Tests a Compute Shader File on being a valid particle struct file.
        /// </summary>
        /// <param name="particleComputeShader">Particle Compute Shader to search for particle struct</param>
        /// <returns>Shader file as <see cref="Object"/> if particle struct found, otherwise null</returns>
        public static Object ComputeShaderToParticleStruct(ComputeShader particleComputeShader)
        {
            string shaderPath = GetStructDefinePath(particleComputeShader);

            if (shaderPath == null)
            {
                Debug.LogError($"{particleComputeShader.name} does not contain " +
                               $"a particle struct or a reference to a file with one.");
                return null;
            }
            
            Object hlslFile = AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Object));
            if (!hlslFile.IsShaderFile())
            {
                Debug.LogError($"Not a valid shader file.");
                return null;
            }

            return hlslFile;
        }
        
        
        /// <summary>
        /// Reads the length of a particle struct from a compute shader in Bytes.
        /// Returns 0 if no valid particle struct is found.
        /// </summary>
        /// <param name="particleComputeShader"></param>
        /// <returns></returns>
        public static int GetStructLengthFromFile(ComputeShader particleComputeShader)
        {
            string shaderPath = GetStructDefinePath(particleComputeShader);

            if (shaderPath == null)
            {
                Debug.LogError($"{particleComputeShader.name} does not contain " +
                               $"a particle struct or a reference to a file with one.");
                return 0;
            }
            
            Object hlslFile = AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Object));
            if (!hlslFile.IsShaderFile())
            {
                Debug.LogError($"Not a valid shader file.");
                return 0;
            }
            
            int totalStructLength = 0;

            string path = AssetDatabase.GetAssetPath(hlslFile);
            string text = File.ReadAllText(path);
            
            
            // Excluding wrong files
            if (!hlslFile.HasFileExtension(ValidFileExtensions))
            {
                Debug.LogError("Object is not an HLSL file.");
                return 0;
            }

            text = text.Replace("\n", ",SPACE,");
            
            char[] seperators = { ';', ',', ' '};
            string[] strValues = text.Split(seperators);


            
            bool isFinished = false;
            bool isInsideStruct = false;
            int structIndentLevel = 0;
            int indentLevel = 0;
            bool isComment = false;
            int longComment = 0;


            foreach (string str in strValues)
            {
                if (str.Contains("struct"))
                {
                    isInsideStruct = true;
                    structIndentLevel = indentLevel;
                }
                
                if (str.Contains("{")) indentLevel++;
                if (str.Contains("}"))
                {
                    indentLevel--;
                    if (structIndentLevel == indentLevel && isInsideStruct)
                    {
                        isInsideStruct = false;
                        isFinished = true;
                    }
                }
                
                if (str.Contains("//")) isComment = true;
                if (str.Contains("SPACE")) isComment = false;
                
                if (str.Contains("/*")) longComment ++;
                if (str.Contains("*/")) longComment --;

                
                
                if (isFinished) continue;
                if (!isInsideStruct) continue;
                if (structIndentLevel != indentLevel - 1) continue;
                if (isComment) continue;
                if (longComment > 0) continue;

                
                if (DataTypeSizeDatabase.ContainsKey(str))
                {
                    totalStructLength += DataTypeSizeDatabase[str];
                }
            }


            return totalStructLength;
        }


        
        /// <summary>
        /// Entry containing the name of a shader property and the
        /// corresponding type of DynaPropertyBinder.
        /// </summary>
        public readonly struct PropertyEntry
        {
            public readonly string propertyName;
            public readonly Type binderType;

            public PropertyEntry(string name, Type type)
            {
                propertyName = name;
                binderType = type;
            }
        }

        /// <summary>
        /// Reads a list of all properties inside the shader file, as well as
        /// getting the types of their DynaPropertyBinder componentes. 
        /// </summary>
        /// <param name="hlslFile">Compute shader file</param>
        /// <param name="getEmissionPropertiesOnly">If true, returns only emission properties, if false gets all properties.</param>
        /// <returns>List of property entries with name and type of the property</returns>
        public static List<PropertyEntry> GetComponentListFromFile(Object hlslFile, bool getEmissionPropertiesOnly = false)
        {
            List<PropertyEntry> propertyEntries = new List<PropertyEntry>();

            string path = AssetDatabase.GetAssetPath(hlslFile);
            string text = File.ReadAllText(path);
            
            
            // Excluding wrong files
            if (!hlslFile.HasFileExtension(ValidFileExtensions))
            {
                Debug.LogError("Object is not an HLSL file.");
                return null;
            }

            text = text.Replace("\n", ",SPACE,");
            text = text.Replace("(", ",BRACKET_OPEN,");
            text = text.Replace(")", ",BRACKET_CLOSED,");
            text = text.Replace("Texture2D", "Texture2D,");
            
            char[] seperators = { ';', ',', ' '};
            string[] strValues = text.Split(seperators);


            int isStartOfLine = 2;
            int indentLevel = 0;
            int bracketLevel = 0;
            int structIndentLevel = 0;
            bool isInsideStruct = false;
            bool isComment = false;
            int longComment = 0;
            bool isEmissionProperties = false;
            bool hasPropertyRead = false;


            for(int i = 0; i < strValues.Length; i++)
            {
                string str = strValues[i];

                if (str.Contains("struct"))
                {
                    isInsideStruct = true;
                    structIndentLevel = indentLevel;
                }
                
                if (str.Contains("{")) indentLevel++;
                if (str.Contains("}"))
                {
                    indentLevel--;
                    if (structIndentLevel == indentLevel && isInsideStruct)
                    {
                        isInsideStruct = false;
                    }
                }
                
                if (str.Contains("//")) isComment = true;
                if (str.Contains("SPACE"))
                {
                    isComment = false;
                    isStartOfLine = 2;
                    hasPropertyRead = false;
                }
                else
                {
                    isStartOfLine = Math.Max(0, isStartOfLine-1);
                }
                

                if (str.Contains("/*")) longComment ++;
                if (str.Contains("*/")) longComment --;
                
                if (str.Contains("BRACKET_OPEN")) bracketLevel ++;
                if (str.Contains("BRACKET_CLOSED")) bracketLevel --;

                if (str.Contains("EMISSION_PROPERTIES_START")) isEmissionProperties = true;
                if (str.Contains("EMISSION_PROPERTIES_END")) isEmissionProperties = false;
                
                if (isInsideStruct) continue;
                if (indentLevel > 0) continue;
                if (isComment) continue;
                if (longComment > 0) continue;
                if (isStartOfLine == 0) continue;
                if (bracketLevel > 0) continue;
                if (hasPropertyRead) continue;
                
                if(!isEmissionProperties && getEmissionPropertiesOnly ) continue;

                
                if (PropertyToPropertyTypeDatabase.ContainsKey(str))
                {
                    if(strValues[i+2].Contains("BRACKET_OPEN") && isStartOfLine == 1) continue;

                    Type propType = PropertyToPropertyTypeDatabase[str];
                    string propName = strValues[i + GetPropertyOffset(propType)];
                    
                    hasPropertyRead = true;
                    
                    PropertyEntry entry = new PropertyEntry(propName, propType);
                    propertyEntries.Add(entry);
                }
            }

            return propertyEntries;
        }
        #endregion


        #region Adding Property Functions

        /// <summary>
        /// Reads properties from a Particles shader file, and attaches them to a GameObject.
        /// </summary>
        /// <param name="particleComputeShader">Compute Shader file used for the particle system</param>
        /// <param name="go">GameObject to attach the DynaProperty components to</param>
        /// <param name="emitterProperties">Return either emitter properties or default properties.</param>
        /// <returns>Array of DynaProperty read from the file</returns>
        public static DynaPropertyBinderBase[] AddPropertyBindersFromShaderFile(ComputeShader particleComputeShader, GameObject go, bool emitterProperties = false)
        {
            string path = GetStructDefinePath(particleComputeShader);

            if (path == null)
            {
                Debug.LogError($"{particleComputeShader.name} does not contain " +
                               $"a particle struct or a reference to a file with one.");
                return null;
            }
            
            Object hlslFile = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (!hlslFile.IsShaderFile())
            {
                Debug.LogError($"Not a valid shader file.");
                return null;
            }

            List<PropertyEntry> propertyEntries = GetComponentListFromFile(hlslFile, emitterProperties);
            if (propertyEntries is null)
            {
                Debug.LogError($"List of property entries in particle file {hlslFile} is null.");
                return null;
            }
            
            Debug.Log($"Found {propertyEntries.Count} properties in the particle file.");

            List<DynaPropertyBinderBase> existingProperties = new List<DynaPropertyBinderBase>(); 
            go.GetComponents(existingProperties);

            
            DynaPropertyBinderBase[] propertyArray = new DynaPropertyBinderBase[propertyEntries.Count];
            int propertyIndex = 0;
            
            foreach (PropertyEntry entry in propertyEntries)
            {
                DynaPropertyBinderBase newProp = 
                    AddPropertyBinderFromType(entry.propertyName, entry.binderType, go, existingProperties);
                
                if(newProp is null) continue;
                propertyArray[propertyIndex] = newProp;
                propertyIndex++;
            }
            return propertyArray;
        }

        
        /// <summary>
        /// Adds a property binder component.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="type">Type of the property binder</param>
        /// <param name="go">GameObject to add component to</param>
        /// <param name="existingProperties">DynaProperties, which already exist on the GameObject</param>
        public static DynaPropertyBinderBase AddPropertyBinderFromType(string propertyName, Type type, GameObject go, List<DynaPropertyBinderBase> existingProperties = null)
        {
            if (!type.IsSubclassOf(typeof(DynaPropertyBinderBase)))
            {
                Debug.LogError($"Type {type} does not inherit from DynaPropertyBinderBase.");
                return null;
            }

            if (existingProperties is not null)
            {
                foreach (DynaPropertyBinderBase existingProperty in existingProperties)
                {
                    if (existingProperty.Name != propertyName || existingProperty.GetType() != type) continue;
                    
                    //Debug.LogWarning($"Property of name {propertyName} and type {type} already exists, therefore will not be added again.");
                    return existingProperty;
                }
            }

            DynaPropertyBinderBase propertyComponent = go.AddComponent(type) as DynaPropertyBinderBase;

            if (propertyComponent is null)
            {
                Debug.LogError($"DynaPropertyBinderBase of type {type} could not be added.");
                return null;
            }
            
            propertyComponent.SetPropertyName(propertyName);
            Undo.RegisterCreatedObjectUndo(propertyComponent, $"Added component of type {type}");

            return propertyComponent;
        }

        #endregion
        
    }
}
