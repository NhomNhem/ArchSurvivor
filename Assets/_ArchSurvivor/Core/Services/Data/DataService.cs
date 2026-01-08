using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _ArchSurvivor.Common.Utilities;
using Cathei.BakingSheet;

namespace _ArchSurvivor.Core.Services.Data {
    public interface IDataProvider {
        GameSheetContainer Sheets { get; }
        UniTask LoadAsync(CancellationToken ct = default);
    }

    public class DataService : IDataProvider {
        public GameSheetContainer Sheets { get; private set; }

        public async UniTask LoadAsync(CancellationToken ct = default) {
            GameLog.Info("Loading Game Data...");
            
            // In a real scenario, we would use a JsonSheetLoader or AddressablesLoader
            // For now, we initialize an empty container to prevent nulls
            Sheets = new GameSheetContainer(null);
            
            string spreadsheetId = "1lN3pfQxHycx26oqKhBJZweiTgdAhPPnfXCQWiHAB13E"; 
            
            string credentialPath = System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName, "archsur-efcfaa3b7a2c.json");
            
            string jsonCredentials = File.ReadAllText(credentialPath);
            
            var converter = new GoogleSheetConverter(spreadsheetId, jsonCredentials);
            
            var success = await Sheets.Bake(converter);

            if (success) GameLog.Info("Game Data Loaded successfully !");
            if (!success) GameLog.Error("Failed to Load Game Data!");
        }
    }
}
