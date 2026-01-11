using System.Globalization;
using UnityEngine;
using _ArchSurvivor.Common.Utilities;

namespace _ArchSurvivor.Core.Services.Data {
    public static class DataParser {
        public static float ParseFloat(string raw, float defaultValue, string context = "") {
            if (string.IsNullOrEmpty(raw)) return defaultValue;
            
            if (float.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out float result)) {
                return result;
            }

            GameLog.Error($"[Data Error] Invalid float format: '{raw}' at {context}. Using default: {defaultValue}");
            return defaultValue;
        }

        public static int ParseInt(string raw, int defaultValue, string context = "") {
            if (string.IsNullOrEmpty(raw)) return defaultValue;

            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out int result)) {
                return result;
            }

            GameLog.Error($"[Data Error] Invalid int format: '{raw}' at {context}. Using default: {defaultValue}");
            return defaultValue;
        }

        public static float ParsePercentage(string raw, float defaultValue, string context = "") {
            if (string.IsNullOrEmpty(raw)) return defaultValue;

            string cleanValue = raw.Replace("%", "").Trim();
            if (float.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float result)) {
                return result / 100f;
            }

            GameLog.Error($"[Data Error] Invalid percentage format: '{raw}' at {context}. Using default: {defaultValue}");
            return defaultValue;
        }
    }
}
