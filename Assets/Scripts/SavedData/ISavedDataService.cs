using General;

namespace SavedData
{
    public interface ISavedDataService : IService
    {
        T GetModel<T>() where T : IModel;
        void RegisterModels();
        T LoadData<T>() where T : IModel, new();
        void SaveData<T>(T data) where T : IModel;
        void DeleteData<T>() where T : IModel;
        bool HasData<T>() where T : IModel;
    }
}