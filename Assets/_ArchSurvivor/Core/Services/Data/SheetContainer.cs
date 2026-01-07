using Cathei.BakingSheet;
using UnityEngine;

namespace _ArchSurvivor.Core.Services.Data {
    /// <summary>
    /// Root container for all data sheets.
    /// Senior Tip: Keep references to all your sheets here for easy injection and access.
    /// </summary>
    public class GameSheetContainer : SheetContainerBase {
        public GameSheetContainer(Microsoft.Extensions.Logging.ILogger logger) : base(logger) {}
        
        // Example Sheet (will be expanded)
        // public PlayerStatSheet PlayerStats { get; private set; }
    }
}
