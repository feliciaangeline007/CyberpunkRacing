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
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!File.Exists(TriggerPath)) return;

            try
            {
                File.Delete(TriggerPath);
                string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Memulai proses pembuatan scene Cyberpunk & build APK...\n";
                File.WriteAllText(LogPath, log);
                Debug.Log("[AutoCyberpunkBuild] Trigger terdeteksi! Membangun scene Cyberpunk Car Racing...");

                // 1. Generate Scene Cyberpunk Main Menu & Highway
                CyberpunkRacingBuilder.BuildAll(false);
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Scene Cyberpunk berhasil digenerate!\n");

                // 2. Build Android APK
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Memulai build Android APK Cyberpunk...\n");
                CyberpunkRacingBuilder.BuildAndroidAPK();

                string apkPath = "Builds/Android/CyberpunkRacing.apk";
                if (File.Exists(apkPath))
                {
                    long bytes = new FileInfo(apkPath).Length;
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SUKSES! APK selesai dibuat di {apkPath} ({bytes / (1024 * 1024.0):0.00} MB)!\n");
                    Debug.Log($"[AutoCyberpunkBuild] SUKSES! APK selesai: {apkPath}");
                }
                else
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Status: Proses build selesai.\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n{ex.StackTrace}\n");
                Debug.LogError($"[AutoCyberpunkBuild] Error: {ex.Message}");
            }
        }
    }
}
