using Cysharp.Text;
using System.Runtime.CompilerServices;

namespace _ArchSurvivor.Common.Utilities {
    /// <summary>
    /// Utility class for optimized string operations using ZString.
    /// Senior Tip: Always use ZString for HUD updates or frequently changing strings to avoid GC Alloc.
    /// </summary>
    public static class GameString {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format(string format, params object[] args) {
            return ZString.Format(format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Concat(string s1, string s2) {
            return ZString.Concat(s1, s2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Concat(string s1, string s2, string s3) {
            return ZString.Concat(s1, s2, s3);
        }
        
        // Add more common concat/format patterns as needed to keep the code clean
    }
}
