using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CyberpunkRacing.Editor
{
    /// <summary>
    /// Trigger otomatis yang dieksekusi oleh Unity Editor yang sedang berjalan
    /// begitu script ini terkompilasi.
    /// Membangun scene Cyberpunk Car Racing dan mem-build APK Android!
    /// </summary>
    [InitializeOnLoad]
    public static class AutoRacingBuildTrigger
    {
        private const string TriggerPath = "Temp/CyberpunkBuildTrigger.flag";
        private const string LogPath = "Logs/AutoCyberpunkBuild.log";

        static AutoRacingBuildTrigger()
        {
            EditorApplication.update += CheckTrigger;
        }

        private static void CheckTrigger()
        {
            if (!File.Exists(TriggerPath)) return;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            try
            {
                File.Delete(TriggerPath);
                EditorApplication.update -= CheckTrigger;

                string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Memulai proses pembuatan scene Cyberpunk...\n";
                File.WriteAllText(LogPath, log);
                Debug.Log("[AutoCyberpunkBuild] Trigger terdeteksi! Membangun scene Cyberpunk Car Racing...");

                // 1. Generate Scene Cyberpunk Main Menu & Highway
                CyberpunkRacingBuilder.BuildAll(false);
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SUKSES: Scene Cyberpunk Highway & Main Menu berhasil dibuat ulang dari nol!\n");
                Debug.Log("[AutoCyberpunkBuild] SUKSES: Scene Cyberpunk berhasil digenerate!");
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n{ex.StackTrace}\n");
                Debug.LogError($"[AutoCyberpunkBuild] Error: {ex.Message}");
            }
        }
    }
}
