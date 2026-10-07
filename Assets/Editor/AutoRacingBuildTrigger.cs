using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CyberpunkRacing.Editor
{
    /// <summary>
    /// Trigger otomatis yang dieksekusi oleh Unity Editor yang sedang berjalan.
    /// Membangun scene Cyberpunk Car Racing, mengonfigurasi Icon, dan mem-build APK Android!
    /// </summary>
    [InitializeOnLoad]
    public static class AutoRacingBuildTrigger
    {
        private const string SceneTriggerPath = "Temp/CyberpunkBuildTrigger.flag";
        private const string ApkTriggerPath = "Temp/CyberpunkBuildAPKTrigger.flag";
        private const string LogPath = "Logs/AutoCyberpunkBuild.log";

        static AutoRacingBuildTrigger()
        {
            EditorApplication.update -= CheckTrigger;
            EditorApplication.update += CheckTrigger;
        }

        private static void CheckTrigger()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            bool buildScenes = File.Exists(SceneTriggerPath);
            bool buildApk = File.Exists(ApkTriggerPath);

            if (!buildScenes && !buildApk) return;

            if (buildScenes)
            {
                try { File.Delete(SceneTriggerPath); } catch {}
            }
            if (buildApk)
            {
                try { File.Delete(ApkTriggerPath); } catch {}
            }

            try
            {
                string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Trigger terdeteksi! Memulai proses...\n";
                File.WriteAllText(LogPath, log);
                Debug.Log("[AutoCyberpunkBuild] Trigger terdeteksi! Memproses scene dan asset Cyberpunk...");

                // 1. Generate Scene Cyberpunk Main Menu & Highway + App Icon
                CyberpunkRacingBuilder.BuildAll(false);
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SUKSES: Scene & Icon Cyberpunk berhasil dibuat!\n");
                Debug.Log("[AutoCyberpunkBuild] SUKSES: Scene Cyberpunk berhasil digenerate!");

                // 2. Jika diminta build APK, jalankan build APK Android
                if (buildApk)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Memulai proses Build Android APK...\n");
                    Debug.Log("[AutoCyberpunkBuild] Memulai Build Android APK...");
                    CyberpunkRacingBuilder.BuildAndroidAPK();
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SUKSES: Build Android APK selesai!\n");
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
