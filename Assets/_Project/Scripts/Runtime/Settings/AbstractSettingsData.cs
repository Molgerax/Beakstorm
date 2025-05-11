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
        
        private static T _instance;

        public void Awake()
        {
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
            string json = JsonUtility.ToJson(this);

            using (StreamWriter writer = new StreamWriter(SavePath))
            {
                writer.Write(json);
            }
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
            return true;
        }
    }
}
