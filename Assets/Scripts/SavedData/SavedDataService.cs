using System;
using System.Collections.Generic;
using System.IO;
using Collectible;
using GameConfig;
using Newtonsoft.Json;
using UI.Settings;
using UnityEngine;

namespace SavedData
{
    [Serializable]
    public class SavedDataService : ISavedDataService
    {
        private readonly ILocalDataProtector _localDataProtector;

        public SavedDataService()
        {
            Debug.Log("SavedData initialized");
            _localDataProtector = new AesCbcLocalDataProtector(new PersistentFileLocalSecretProvider());
            RegisterModels();
        }

        private Dictionary<Type, IModel> models = new Dictionary<Type, IModel>();

        public void RegisterModels()
        {
            var settingsModel = LoadData<SettingsModel>();
            var collectibleModel = LoadData<CollectibleModel>();

        }
        public T GetModel<T>() where T : IModel
        {
            if (models.TryGetValue(typeof(T), out var model))
            {
                return (T)model;
            }
            else
            {
                return default(T);
            }
        }

        public void SaveData<T>(T data) where T : IModel
        {
            SaveModel(data);
        }

        public T LoadData<T>() where T : IModel, new()
        {
            var savableModel = ReadData<T>();
            models[typeof(T)] = savableModel != null ? savableModel : new T();
            return (T)models[typeof(T)];
        }

        public T ReadData<T>() where T : IModel
        {
            var fileName = typeof(T).Name;
            var fullPath = Application.persistentDataPath + "/" + fileName;
            if (!File.Exists(fullPath)) return default;

            var fileContent = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                DeleteCorruptedData(typeof(T));
                return default;
            }

            try
            {
                if (IsProtectionEnabledFor(typeof(T)))
                {
                    if (!_localDataProtector.TryUnprotect(fileContent, out var plainJson))
                    {
                        DeleteCorruptedData(typeof(T));
                        return default;
                    }

                    var protectedModel = JsonConvert.DeserializeObject<T>(plainJson);
                    if (protectedModel == null)
                    {
                        DeleteCorruptedData(typeof(T));
                        return default;
                    }

                    return protectedModel;
                }

                var plainModel = JsonConvert.DeserializeObject<T>(fileContent);
                if (plainModel == null)
                {
                    DeleteCorruptedData(typeof(T));
                    return default;
                }

                return plainModel;
            }
            catch
            {
                DeleteCorruptedData(typeof(T));
                return default;
            }
        }

        private void DeleteCorruptedData(Type modelType)
        {
            try
            {
                var fullPath = Application.persistentDataPath + "/" + modelType.Name;
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
            }

            models.Remove(modelType);
        }

        public void Dispose()
        {
            // Save all models before disposing
            foreach (var modelPair in models)
            {
                if (modelPair.Value != null)
                {
                    SaveModel(modelPair.Value);
                }
            }

            models.Clear();
        }

        public void DeleteData<T>() where T : IModel
        {
            var fileName = typeof(T).Name;
            var fullPath = Application.persistentDataPath + "/" + fileName;
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            if (models.ContainsKey(typeof(T)))
            {
                models.Remove(typeof(T));
            }
        }

        public bool HasData<T>() where T : IModel
        {
            var fileName = typeof(T).Name;
            var fullPath = Application.persistentDataPath + "/" + fileName;
            return File.Exists(fullPath);
        }

        private void SaveModel(IModel model)
        {
            var fileName = model.GetType().Name;
            var fullPath = Application.persistentDataPath + "/" + fileName;

            var json = JsonConvert.SerializeObject(model);
            var dataToWrite = IsProtectionEnabledFor(model.GetType()) ? _localDataProtector.Protect(json) : json;

            File.WriteAllText(fullPath, dataToWrite);
            models[model.GetType()] = model;
        }

        private static bool IsProtectionEnabledFor(Type modelType)
        {
            return modelType == typeof(CollectibleModel)
                   || modelType == typeof(RemoteConfigModel);
        }
    }
}
