using _ArchSurvivor.Core.Interfaces;
using UnityEngine;

namespace _ArchSurvivor.Core.Services.Data {
    public class SaveService : ISaveService {
        private const string DefaultPassword = "ArchSurvivor_Secret_2026";

        public void Save<T>(string key, T value, bool encrypt = false) {
            var settings = new ES3Settings();
            if (encrypt) {
                settings.encryptionType = ES3.EncryptionType.AES;
                settings.encryptionPassword = DefaultPassword;
            }
            
            ES3.Save(key, value, settings);
        }

        public T Load<T>(string key, T defaultValue = default, bool encrypt = false) {
            var settings = new ES3Settings();
            if (encrypt) {
                settings.encryptionType = ES3.EncryptionType.AES;
                settings.encryptionPassword = DefaultPassword;
            }

            return ES3.KeyExists(key, settings) ? ES3.Load<T>(key, settings) : defaultValue;
        }

        public void Delete(string key) {
            ES3.DeleteKey(key);
        }

        public bool HasKey(string key) {
            return ES3.KeyExists(key);
        }

        public void SaveAll() {
            // ES3 saves immediately by default, but we can manage file caching if needed.
            Debug.Log("[SaveService] All data synchronized to persistent storage.");
        }
    }
}
