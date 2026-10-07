using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace EnchantedForest.Editor
{
    public static class AndroidBuildProfileSetup
    {
        private const string ProfileName = "Lumora Android";
        private const string ProfilePath = "Assets/Settings/Build Profiles/Lumora Android.asset";
        private const string ApplicationId = "com.feliciaangeline.lumora";

        [MenuItem("Tools/Lumora/Build/Create Android Build Profile")]
        public static void CreateOrUpdateAndroidProfile()
        {
            ConfigureAndroidPlayerSettings();

            InstalledPlatformInfo androidPlatform = default;
            bool androidModuleInstalled = false;
            foreach (InstalledPlatformInfo platform in BuildProfile.GetInstalledPlatformModules())
            {
                if (!string.Equals(platform.displayName, "Android", StringComparison.OrdinalIgnoreCase)) continue;
                androidPlatform = platform;
                androidModuleInstalled = true;
                break;
            }

            if (!androidModuleInstalled)
            {
                const string message = "Android Build Support belum terpasang untuk Unity Editor. " +
                    "Pasang modul Android Build Support melalui Unity Hub, lalu jalankan menu ini kembali.";
                Debug.LogError($"[Lumora Android] {message}");
                EditorUtility.DisplayDialog("Android Build Support diperlukan", message, "OK");
                return;
            }

            EnsureFolder("Assets/Settings/Build Profiles");
            BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                profile = BuildProfile.CreateBuildProfile(androidPlatform.platformGuid, ProfileName);
            }

            profile.overrideGlobalScenes = true;
            profile.scenes = EditorBuildSettings.scenes;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Lumora Android] Build profile siap: {AssetDatabase.GetAssetPath(profile)}");
            EditorUtility.DisplayDialog(
                "Build Profile Android siap",
                $"Profil \"{ProfileName}\" dibuat/diperbarui.\n\n" +
                "Buka File > Build Profiles, pilih profil Lumora Android, lalu tekan Build.",
                "OK");
        }

        private static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.productName = "Lumora";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
