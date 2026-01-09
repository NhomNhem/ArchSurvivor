using Cathei.BakingSheet;
using UnityEngine;
using System;

namespace _ArchSurvivor.Core.Services.Data {
    /// <summary>
    /// Bộ Logger giúp BakingSheet in thông tin ra Console của Unity thay vì bị NullReference
    /// </summary>
    public class UnityBakingSheetLogger : Microsoft.Extensions.Logging.ILogger {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            string message = formatter(state, exception);
            
            // Nếu là lỗi "Column name is invalid", chúng ta hạ cấp xuống Log thường 
            // vì nó thường là do cột trống ở cuối Google Sheet, không ảnh hưởng gameplay.
            if (message.Contains("Column name is invalid")) {
                // Không in Error nữa cho đỡ ngứa mắt
                Debug.Log($"[BakingSheet - Info] Ignore empty/invalid column on Sheets (Safe).");
                return; 
            }

            if (exception != null) message += $" | Details: {exception.Message}";
            
            if (logLevel >= Microsoft.Extensions.Logging.LogLevel.Error) Debug.LogError($"[BakingSheet] {message}");
            else if (logLevel >= Microsoft.Extensions.Logging.LogLevel.Warning) Debug.LogWarning($"[BakingSheet] {message}");
            else Debug.Log($"[BakingSheet] {message}");
        }
    }

    public class GameSheetContainer : SheetContainerBase {
        public GameSheetContainer() : base(new UnityBakingSheetLogger()) { }
        
        public CharacterSheet Characters { get; set; } 
        public CardSheet Cards { get; set; }
        public CardEffectSheet CardEffect { get; set; }
    }
}
