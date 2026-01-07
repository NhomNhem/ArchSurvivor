using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _ArchSurvivor.Common.Utilities;

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
            
            await UniTask.Yield(); // Simulate async load
            
            GameLog.Info("Game Data Loaded successfully.");
        }
    }
}
