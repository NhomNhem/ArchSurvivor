using R3;

namespace _ArchSurvivor.Core.Interfaces {
    public interface ISaveService {
        void Save<T>(string key, T value, bool encrypt = false);
        T Load<T>(string key, T defaultValue = default, bool encrypt = false);
        void Delete(string key);
        bool HasKey(string key);
        void SaveAll();
    }
}
