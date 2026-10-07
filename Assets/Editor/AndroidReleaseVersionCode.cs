using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CoinConvoy.Editor
{
    public sealed class AndroidReleaseVersionCode : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            string[] parts = PlayerSettings.bundleVersion.Split('.');
            if (parts.Length < 2 || parts.Length > 3 ||
                !int.TryParse(parts[0], out int major) ||
                !int.TryParse(parts[1], out int minor) ||
                (parts.Length == 3 && !int.TryParse(parts[2], out _)))
            {
                throw new BuildFailedException(
                    $"Versi aplikasi '{PlayerSettings.bundleVersion}' tidak valid. Gunakan format MAJOR.MINOR.PATCH.");
            }

            int patch = parts.Length == 3 ? int.Parse(parts[2]) : 0;
            if (major < 0 || minor < 0 || patch < 0 || minor >= 1000 || patch >= 1000)
            {
                throw new BuildFailedException(
                    "Versi Android harus memiliki MINOR dan PATCH di bawah 1000, dan semua bagian tidak boleh negatif.");
            }

            long versionCode = (long)major * 1_000_000L + (long)minor * 1_000L + patch;
            if (versionCode < 1 || versionCode > int.MaxValue)
            {
                throw new BuildFailedException(
                    $"Versi '{PlayerSettings.bundleVersion}' menghasilkan Android versionCode yang tidak valid.");
            }

            PlayerSettings.Android.bundleVersionCode = (int)versionCode;
            Debug.Log($"[Lumora Build] Android versionCode disetel ke {versionCode} dari {PlayerSettings.bundleVersion}.");
        }
    }
}
