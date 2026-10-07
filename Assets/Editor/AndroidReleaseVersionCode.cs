using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CyberpunkRacing.Editor
{
    /// <summary>
    /// Menghitung Android versionCode secara otomatis dari bundleVersion (MAJOR.MINOR.PATCH).
    /// Contoh: "1.2.3" → versionCode 1002003
    /// Contoh: "0.1.0" → versionCode 1000 (minimum 1 agar valid)
    /// </summary>
    public sealed class AndroidReleaseVersionCode : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            string version = PlayerSettings.bundleVersion;
            string[] parts = version.Split('.');

            if (parts.Length < 2 || parts.Length > 3)
            {
                throw new BuildFailedException(
                    $"[CyberpunkRacing] Bundle version '{version}' tidak valid. Gunakan format MAJOR.MINOR atau MAJOR.MINOR.PATCH.");
            }

            if (!int.TryParse(parts[0], out int major) ||
                !int.TryParse(parts[1], out int minor) ||
                (parts.Length == 3 && !int.TryParse(parts[2], out _)))
            {
                throw new BuildFailedException(
                    $"[CyberpunkRacing] Bundle version '{version}' mengandung nilai non-angka.");
            }

            int patch = parts.Length == 3 ? int.Parse(parts[2]) : 0;

            if (major < 0 || minor < 0 || patch < 0 || minor >= 1000 || patch >= 1000)
            {
                throw new BuildFailedException(
                    $"[CyberpunkRacing] Version '{version}': MINOR dan PATCH harus 0–999, semua bagian tidak boleh negatif.");
            }

            long versionCode = (long)major * 1_000_000L + (long)minor * 1_000L + patch;

            // Android mengharuskan versionCode >= 1
            if (versionCode < 1) versionCode = 1;

            if (versionCode > int.MaxValue)
            {
                throw new BuildFailedException(
                    $"[CyberpunkRacing] Version '{version}' menghasilkan versionCode {versionCode} yang melebihi batas Android.");
            }

            PlayerSettings.Android.bundleVersionCode = (int)versionCode;
            Debug.Log($"[CyberpunkRacing] Android versionCode = {versionCode} (dari versi {version}).");
        }
    }
}
