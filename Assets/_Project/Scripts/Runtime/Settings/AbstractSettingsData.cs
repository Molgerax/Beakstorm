using System.IO;
using UnityEngine;

namespace Beakstorm.Settings
{
    [System.Serializable]
    public abstract class AbstractSettingsData<T> : ScriptableObject where T : AbstractSettingsData<T>
    {
        public static T Instance
        {
            get
            {
                if (_instance)
                    return _instance;

                _instance = FindAnyObjectByType<T>();
                if (_instance)
                    return _instance;
                
                _instance = CreateInstance<T>();
                _instance.Initialize_Internal();
                return _instance;
            }
        }
        
        public virtual void Apply() {}
        
        private static T _instance;

        public void Awake()
        {
            if (_instance)
            {
                LoadData();
            }
            
            _instance = (T)this;
        }

        public abstract string FileName { get; }

        public string SavePath => Application.persistentDataPath + Path.AltDirectorySeparatorChar + FileName + ".json";

        private void Initialize_Internal()
        {
            if (!LoadData())
            {
                Initialize();
            }
        }
        
        public virtual void Initialize() {}

        public void SaveData()
        {
            if (_instance != this)
                _instance = (T)this;
            
            string json = JsonUtility.ToJson(this);

            using (StreamWriter writer = new StreamWriter(SavePath))
            {
                writer.Write(json);
            }
            
            Apply();
        }

        public bool LoadData()
        {
            string json;

            if (!File.Exists(SavePath))
                return false;
            
            using (StreamReader reader = new StreamReader(SavePath))
            {
                json = reader.ReadToEnd();
            }
            
            JsonUtility.FromJsonOverwrite(json, this);
            
            Apply();
            return true;
        }
    }
}
