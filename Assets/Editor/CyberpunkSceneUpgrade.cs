using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CyberpunkRacing.Editor
{
    /// <summary>
    /// Upgrade visual & keselamatan jalur untuk scene Cyberpunk (otomatis saat scene dibuka).
    /// - Night skybox + ambient/fog warna synthwave
    /// - Global Volume: Bloom & Vignette supaya neon menyala
    /// - Kamera: HDR + post-processing aktif
    /// - Rel tepi jalur ditinggikan agar mobil tidak terperosok
    /// - Marka jalur neon menyala di tengah aspal
    /// </summary>
    public static class CyberpunkSceneUpgrade
    {
        private const string ProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";
        private const string SkyPath = "Assets/Materials/Cyberpunk/Mat_NightSky.mat";
        private const string LaneMatPath = "Assets/Materials/Cyberpunk/Mat_LaneGlow.mat";

        [MenuItem("Tools/Cyberpunk Racing/✨ Upgrade Visual Scene Cyberpunk", priority = 2)]
        public static void UpgradeFromMenu()
        {
            Apply(true);
        }

        [InitializeOnLoadMethod]
        private static void AutoUpgradeOnSceneOpen()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SessionState.GetBool("CyberpunkSceneUpgrade.Done", false)) return;

                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (sceneName != "CyberpunkHighway" && sceneName != "CyberpunkMainMenu") return;

                SessionState.SetBool("CyberpunkSceneUpgrade.Done", true);
                Apply(false);
            };
        }

        public static void Apply(bool fromMenu)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;
            bool wasDirty = scene.isDirty;

            UpgradeLighting();
            var profile = UpgradePostProcessingProfile();
            EnsureGlobalVolume(profile);
            UpgradeCamera();
            UpgradeRailSafety();
            AddLaneMarkers();

            if (!wasDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();

            Debug.Log($"[CyberpunkUpgrade] Visual scene '{scene.name}' sudah di-upgrade" +
                      (wasDirty ? " (scene belum disimpan otomatis karena ada perubahan Anda)." : " dan disimpan."));
            if (fromMenu)
            {
                EditorUtility.DisplayDialog("Cyberpunk Upgrade",
                    "Visual scene diperbarui: night skybox, bloom neon, post-processing, rel tepi lebih tinggi, dan marka jalur.",
                    "OK");
            }
        }

        // ── 1. Langit senja + ambient synthwave + fog keemasan ───────────────
        private static void UpgradeLighting()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/LangitSenja.mat");
            if (sky == null)
            {
                sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SunsetValley/Mat_SunsetSkybox.mat");
            }
            if (sky != null)
            {
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.65f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.28f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.12f, 0.20f);
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.70f, 0.48f, 0.42f);
        }

        // ── 2. Profil Bloom + Vignette + Color Adjustments ───────────────────
        private static VolumeProfile UpgradePostProcessingProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                EnsureFolder("Assets/Settings");
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            if (!profile.TryGet<Bloom>(out var bloom))
                bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.0f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.9f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
            bloom.clamp.overrideState = true;
            bloom.clamp.value = 40f;

            if (!profile.TryGet<Vignette>(out var vignette))
                vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.32f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.02f, 0.01f, 0.06f);

            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
                colorAdj = profile.Add<ColorAdjustments>();
            colorAdj.active = true;
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.15f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 12f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 10f;

            // Efek kecepatan: distorsi lensa ringan + chromatic aberration
            if (!profile.TryGet<LensDistortion>(out var lens))
                lens = profile.Add<LensDistortion>();
            lens.active = true;
            lens.intensity.overrideState = true;
            lens.intensity.value = -0.18f;
            lens.scale.overrideState = true;
            lens.scale.value = 1.02f;

            if (!profile.TryGet<ChromaticAberration>(out var chroma))
                chroma = profile.Add<ChromaticAberration>();
            chroma.active = true;
            chroma.intensity.overrideState = true;
            chroma.intensity.value = 0.18f;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void EnsureGlobalVolume(VolumeProfile profile)
        {
            var volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null)
            {
                var go = new GameObject("Global Volume (Cyberpunk)");
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
            }

            volume.sharedProfile = profile;
            volume.priority = 1f;
            EditorUtility.SetDirty(volume);
        }

        // ── 3. Kamera: HDR + post-processing aktif ───────────────────────────
        private static void UpgradeCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (RenderSettings.skybox != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.03f, 0.03f, 0.07f);
            }

            cam.allowHDR = true;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 600f);

            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;
            camData.dithering = true;
            EditorUtility.SetDirty(camData);
        }

        // ── 4. Rel tepi jalur lebih tinggi (mobil tidak mudah terperosok) ────
        private static void UpgradeRailSafety()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!t.name.StartsWith("Rail_L_") && !t.name.StartsWith("Rail_R_")) continue;
                Vector3 scale = t.localScale;
                if (scale.y >= 1.8f) continue;
                Vector3 pos = t.position;
                t.position = new Vector3(pos.x, 0.9f, pos.z);
                t.localScale = new Vector3(scale.x, 1.8f, scale.z);
                EditorUtility.SetDirty(t);
            }
        }

        // ── 5. Marka jalur neon (dash menyala) di tengah aspal ───────────────
        private static void AddLaneMarkers()
        {
            var laneMat = AssetDatabase.LoadAssetAtPath<Material>(LaneMatPath);
            if (laneMat == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) return;
                laneMat = new Material(lit) { name = "Mat_LaneGlow" };
                laneMat.color = new Color(0.9f, 0.95f, 1f);
                laneMat.EnableKeyword("_EMISSION");
                laneMat.SetColor("_EmissionColor", new Color(2.4f, 2.2f, 0.6f));
                EnsureFolder("Assets/Materials");
                EnsureFolder("Assets/Materials/Cyberpunk");
                AssetDatabase.CreateAsset(laneMat, LaneMatPath);
            }

            foreach (var road in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (!road.name.StartsWith("CyberRoad_")) continue;
                if (road.Find("LaneDash_L") != null) continue;

                CreateLaneDash(road, "LaneDash_L", new Vector3(0f, 0.43f, -3.2f), laneMat);
                CreateLaneDash(road, "LaneDash_R", new Vector3(0f, 0.43f, 3.2f), laneMat);
            }
        }

        private static void CreateLaneDash(Transform road, string name, Vector3 localPos, Material mat)
        {
            var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = name;
            dash.transform.SetParent(road, false);
            dash.transform.localPosition = localPos;
            dash.transform.localRotation = Quaternion.identity;
            dash.transform.localScale = new Vector3(0.5f, 0.04f, 5f);
            Object.DestroyImmediate(dash.GetComponent<Collider>());
            dash.GetComponent<Renderer>().sharedMaterial = mat;
            EditorUtility.SetDirty(dash);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
