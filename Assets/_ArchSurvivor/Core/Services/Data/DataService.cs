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
            GameLog.Info("Loading Game Data from Google Sheets...");

            // Senior Tip: Ép hệ thống dùng InvariantCulture để nạp dấu chấm thập phân (1.1) 
            // bất kể máy tính đang ở vùng miền nào (Việt Nam dùng dấu phẩy).
            var originalCulture = System.Globalization.CultureInfo.DefaultThreadCurrentCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            try {
                string root = Directory.GetParent(Application.dataPath).FullName;
                string credentialPath = Path.Combine(root, "archsur-efcfaa3b7a2c.json");

                if (!File.Exists(credentialPath)) {
                    GameLog.Error($"Missing credential file: {credentialPath}");
                    return;
                }

                string jsonCredentials = File.ReadAllText(credentialPath);
                string spreadsheetId = "1lN3pfQxHycx26oqKhBJZweiTgdAhPPnfXCQWiHAB13E";

                Sheets = new GameSheetContainer();
                var converter = new GoogleSheetConverter(spreadsheetId, jsonCredentials);
                
                await Sheets.Bake(converter);

                // Kiểm tra và thông báo kết quả nạp từng bảng
                if (Sheets.Characters != null && Sheets.Characters.Count > 0) {
                    GameLog.Info($"[DATA] Loaded {Sheets.Characters.Count} characters.");
                }

                if (Sheets.Cards != null && Sheets.Cards.Count > 0) {
                    GameLog.Info($"[DATA] Loaded {Sheets.Cards.Count} cards.");
                }

                if (Sheets.CardEffect != null && Sheets.CardEffect.Count > 0) {
                    GameLog.Info($"[DATA] Loaded {Sheets.CardEffect.Count} card effects.");
                }
                
                if (Sheets.Characters?.Count == 0 && Sheets.Cards?.Count == 0 && Sheets.CardEffect?.Count == 0) {
                    GameLog.Warning("[DATA] Google Sheets connected but all sheets are empty.");
                }
            }
            catch (System.Exception ex) {
                GameLog.Error($"[Data Error] {ex.Message}");
                if (ex.InnerException != null) GameLog.Error($"[Detail] {ex.InnerException.Message}");
            }
            finally {
                // Khôi phục lại Culture gốc của máy tính
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = originalCulture;

                // Ensure systems depending on this don't crash if data loading fails
                if (Sheets == null) Sheets = new GameSheetContainer();
                if (Sheets.Characters == null) Sheets.Characters = new CharacterSheet();
            }
        }
    }
}
