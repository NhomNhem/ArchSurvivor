using UnityEngine;

namespace _ArchSurvivor.Common.Utilities {
    public static class GameLog {
        private const string Prefix = "[ArchSurvivor] ";

        public static void Info(string message) => Debug.Log($"{Prefix}{message}");
        public static void Warning(string message) => Debug.LogWarning($"{Prefix}{message}");
        public static void Error(string message) => Debug.LogError($"{Prefix}{message}");
        
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Verbose(string message) => Debug.Log($"{Prefix}<color=grey>{message}</color>");
    }
}
