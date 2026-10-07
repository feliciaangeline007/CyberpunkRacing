using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using CyberpunkRacing;

namespace CyberpunkRacing.Editor
{
    /// <summary>
    /// Master Builder Otomatis untuk Game CYBERPUNK CAR RACING (Synthwave Sunset Overdrive):
    /// 1. Mobil Sport 3D Nyata (Synty Muscle Car) dengan roda berputar, belok roda, dan efek nitro knalpot api.
    /// 2. Gedung Pencakar Langit 3D Metropolis Asli dari Synty Studios dengan variasi arsitektur bertingkat.
    /// 3. Jalan Tol Layang Futuristik dengan marka jalur neon bercahaya, rel pengaman neon, dan pilar jembatan.
    /// 4. Pencahayaan & Langit Matahari Senja Keemasan (Sunset Synthwave) dengan URP Bloom & Soft Shadows.
    /// 5. Showroom Menu Utama dengan mobil sport 3D berputar di atas podium neon dengan latar kota metropolis.
    /// </summary>
    public class CyberpunkRacingBuilder : EditorWindow
    {
        private const string ScenePath = "Assets/Scenes";
        private const string MatPath = "Assets/Materials/Cyberpunk";

        [MenuItem("Tools/Cyberpunk Racing/⚡ Generate Semua Scene Cyberpunk", priority = 1)]
        public static void GenerateAllMenu() => BuildAll(false);

        public static void GenerateAllBatch() => BuildAll(false);

        [MenuItem("Tools/Cyberpunk Racing/📦 Build Android APK", priority = 10)]
        public static void BuildAndroidAPKMenu() => BuildAndroidAPK();

        public static void BuildAll(bool showDialog = true)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                void OnPlayState(PlayModeStateChange state)
                {
                    if (state == PlayModeStateChange.EnteredEditMode)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayState;
                        EditorApplication.delayCall += () => BuildAll(showDialog);
                    }
                }
                EditorApplication.playModeStateChanged += OnPlayState;
                return;
            }

            try
            {
                if (showDialog) EditorUtility.DisplayProgressBar("Cyberpunk Racing Builder", "Menyiapkan material & shader URP...", 0.1f);
                EnsureFolders();
                SyntyURPMaterialUpgrader.UpgradeAllSyntyMaterials();
                var mats = CreateMaterials();

                if (showDialog) EditorUtility.DisplayProgressBar("Cyberpunk Racing Builder", "🌆 Membangun Lintasan Cyberpunk Highway dengan Gedung & Mobil 3D...", 0.45f);
                BuildHighwayScene(mats);

                if (showDialog) EditorUtility.DisplayProgressBar("Cyberpunk Racing Builder", "🏠 Membangun Showroom Cyberpunk Main Menu...", 0.8f);
                BuildMainMenuScene(mats);

                RegisterScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (showDialog)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("✦ Selesai!", "Scene Cyberpunk Car Racing (Mobil 3D + Gedung Metropolis + Langit Senja) berhasil dibuat!", "OK");
                }

                OpenScene("CyberpunkMainMenu.unity");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[CyberpunkRacing] Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MatPath)) AssetDatabase.CreateFolder("Assets/Materials", "Cyberpunk");
            if (!AssetDatabase.IsValidFolder(ScenePath)) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!Directory.Exists("Builds/Android")) Directory.CreateDirectory("Builds/Android");
        }

        public class CyberMats
        {
            public Material CyberRoad;
            public Material NeonCyan;
            public Material NeonMagenta;
            public Material NeonYellow;
            public Material LaneGlow;
            public Material BuildingWall;
            public Material BuildingCyanGlow;
            public Material BuildingMagentaGlow;
            public Material CarBodyBlue;
            public Material CarCabin;
            public Material CarWheel;
            public Material CarTailLight;
            public Material CyberCoin;
        }

        private static CyberMats CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new CyberMats();

            m.CyberRoad = CreateMat("Mat_CyberRoad", litShader, new Color(0.08f, 0.09f, 0.12f), 0.25f, 0.82f);
            m.NeonCyan = CreateMat("Mat_NeonCyan", litShader, new Color(0f, 0.95f, 1f), 0.1f, 0.95f, new Color(0f, 3.2f, 3.8f));
            m.NeonMagenta = CreateMat("Mat_NeonMagenta", litShader, new Color(1f, 0.12f, 0.88f), 0.1f, 0.95f, new Color(3.6f, 0.2f, 3.0f));
            m.NeonYellow = CreateMat("Mat_NeonYellow", litShader, new Color(1f, 0.85f, 0.1f), 0.1f, 0.95f, new Color(3.0f, 2.4f, 0.2f));
            m.LaneGlow = CreateMat("Mat_LaneGlow", litShader, new Color(0f, 0.95f, 1f), 0.1f, 0.95f, new Color(0f, 2.5f, 3.0f));
            m.BuildingWall = CreateMat("Mat_BuildingWall", litShader, new Color(0.07f, 0.08f, 0.12f), 0.85f, 0.45f);
            m.BuildingCyanGlow = CreateMat("Mat_BuildingCyanGlow", litShader, new Color(0.05f, 0.15f, 0.22f), 0.5f, 0.8f, new Color(0f, 1.6f, 2.2f));
            m.BuildingMagentaGlow = CreateMat("Mat_BuildingMagentaGlow", litShader, new Color(0.18f, 0.05f, 0.15f), 0.5f, 0.8f, new Color(2.0f, 0.2f, 1.6f));

            m.CarBodyBlue = CreateMat("Mat_CyberCarBody", litShader, new Color(0.04f, 0.38f, 0.95f), 0.92f, 0.95f);
            m.CarCabin = CreateMat("Mat_CyberCarCabin", litShader, new Color(0.03f, 0.04f, 0.06f), 0.95f, 0.98f);
            m.CarWheel = CreateMat("Mat_CyberCarWheel", litShader, new Color(0.12f, 0.13f, 0.15f), 0.4f, 0.35f);
            m.CarTailLight = CreateMat("Mat_CyberTailLight", litShader, new Color(1.0f, 0.1f, 0.1f), 0.2f, 0.8f, new Color(4.0f, 0.2f, 0.2f));
            m.CyberCoin = CreateMat("Mat_CyberDataOrb", litShader, new Color(0f, 0.95f, 1f), 0.9f, 0.95f, new Color(0.3f, 3.2f, 3.8f));

            return m;
        }

        private static Material CreateMat(string name, Shader sh, Color col, float metal, float smooth, Color? emit = null)
        {
            string path = $"{MatPath}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = sh;
            mat.color = col;
            mat.SetFloat("_Metallic", metal);
            mat.SetFloat("_Smoothness", smooth);
            if (emit.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emit.Value);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static PhysicsMaterial GetOrCreateFrictionlessPhysMat()
        {
            string path = $"{MatPath}/Mat_ZeroFriction.physicsMaterial";
            PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (pm == null)
            {
                pm = new PhysicsMaterial("FrictionlessPhysMat")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0.05f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Average
                };
                AssetDatabase.CreateAsset(pm, path);
            }
            return pm;
        }

        // ── 1. GAMEPLAY SCENE: CYBERPUNK HIGHWAY ──────────────────────────────
        private static void BuildHighwayScene(CyberMats m)
        {
            Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCyberpunkLighting();
            SetupPostProcessing();

            var worldRoot = new GameObject("--- CYBERPUNK HIGHWAY ---");

            float trackLength = 550f;
            float roadWidth = 15f;
            int numSegments = Mathf.CeilToInt(trackLength / 12f);

            var roadRoot = new GameObject("NeonHighway");
            roadRoot.transform.SetParent(worldRoot.transform);

            var cityRoot = new GameObject("MetropolisSkyscrapers");
            cityRoot.transform.SetParent(worldRoot.transform);

            var archesRoot = new GameObject("HoloArches");
            archesRoot.transform.SetParent(worldRoot.transform);

            var coinsRoot = new GameObject("DataNodes");
            coinsRoot.transform.SetParent(worldRoot.transform);

            AudioClip coinClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CoinPickup.wav");
            PhysicsMaterial smoothPhysMat = GetOrCreateFrictionlessPhysMat();

            // Daftar prefab gedung bertingkat 3D Synty
            List<GameObject> buildingPrefabs = LoadBuildingPrefabs();
            GameObject pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SyntyStudios/PolygonCity/Prefabs/Environments/SM_Env_Bridge_Pillar_01.prefab");

            for (int i = 0; i < numSegments; i++)
            {
                float z = i * 12f;
                float xOffset = Mathf.Sin(z * 0.02f) * 22f + Mathf.Cos(z * 0.05f) * 8f;
                float nextZ = (i + 1) * 12f;
                float nextX = Mathf.Sin(nextZ * 0.02f) * 22f + Mathf.Cos(nextZ * 0.05f) * 8f;

                Vector3 currentPos = new Vector3(xOffset, 0f, z);
                Vector3 forwardDir = (new Vector3(nextX, 0f, nextZ) - currentPos).normalized;
                Quaternion segRot = Quaternion.LookRotation(forwardDir);
                Vector3 rightDir = segRot * Vector3.right;

                // 1. Aspal Tol Futuristik
                var roadSeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roadSeg.name = $"CyberRoad_{i}";
                roadSeg.transform.SetParent(roadRoot.transform);
                roadSeg.transform.position = currentPos + Vector3.down * 0.4f + forwardDir * 6f;
                roadSeg.transform.rotation = segRot;
                roadSeg.transform.localScale = new Vector3(roadWidth, 0.8f, 13f);
                roadSeg.GetComponent<Renderer>().sharedMaterial = m.CyberRoad;

                // Marka Jalur Neon Tengah
                var laneMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                laneMarker.name = $"LaneMarker_{i}";
                laneMarker.transform.SetParent(roadRoot.transform);
                laneMarker.transform.position = currentPos + Vector3.up * 0.02f + forwardDir * 6f;
                laneMarker.transform.rotation = segRot;
                laneMarker.transform.localScale = new Vector3(0.35f, 0.04f, 7.5f);
                laneMarker.GetComponent<Renderer>().sharedMaterial = m.LaneGlow;
                DestroyImmediate(laneMarker.GetComponent<Collider>());

                // 2. Rel Neon Kiri (Cyan) - Lebih tinggi sebagai pengaman
                var railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railL.name = $"Rail_L_{i}";
                railL.transform.SetParent(roadRoot.transform);
                railL.transform.position = currentPos - (rightDir * (roadWidth * 0.5f + 0.3f)) + Vector3.up * 1.0f + forwardDir * 6f;
                railL.transform.rotation = segRot;
                railL.transform.localScale = new Vector3(0.55f, 2.0f, 13f);
                railL.GetComponent<Renderer>().sharedMaterial = m.NeonCyan;
                railL.GetComponent<BoxCollider>().sharedMaterial = smoothPhysMat;

                // 3. Rel Neon Kanan (Magenta)
                var railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railR.name = $"Rail_R_{i}";
                railR.transform.SetParent(roadRoot.transform);
                railR.transform.position = currentPos + (rightDir * (roadWidth * 0.5f + 0.3f)) + Vector3.up * 1.0f + forwardDir * 6f;
                railR.transform.rotation = segRot;
                railR.transform.localScale = new Vector3(0.55f, 2.0f, 13f);
                railR.GetComponent<Renderer>().sharedMaterial = m.NeonMagenta;
                railR.GetComponent<BoxCollider>().sharedMaterial = smoothPhysMat;

                // Pilar Penyangga Jalan Tol Layang di Bawah
                if (i % 3 == 0)
                {
                    Vector3 pillarPos = currentPos + forwardDir * 6f + Vector3.down * 12f;
                    if (pillarPrefab != null)
                    {
                        var pillar = Object.Instantiate(pillarPrefab, pillarPos, segRot, roadRoot.transform);
                        pillar.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);
                    }
                    else
                    {
                        var pilCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pilCol.name = $"Pillar_{i}";
                        pilCol.transform.SetParent(roadRoot.transform);
                        pilCol.transform.position = pillarPos;
                        pilCol.transform.localScale = new Vector3(3f, 12f, 3f);
                        pilCol.GetComponent<Renderer>().sharedMaterial = m.BuildingWall;
                        DestroyImmediate(pilCol.GetComponent<Collider>());
                    }
                }

                // 4. Gedung Pencakar Langit 3D Metropolis (Synty 3D Prefabs)
                if (buildingPrefabs.Count > 0 && i % 2 == 0)
                {
                    float bDistL = roadWidth * 0.5f + 18f + (i % 3) * 5f;
                    float bDistR = roadWidth * 0.5f + 18f + ((i + 1) % 3) * 5f;

                    // Gedung Kiri
                    GameObject prefabL = buildingPrefabs[i % buildingPrefabs.Count];
                    Vector3 posL = currentPos - (rightDir * bDistL) + forwardDir * 6f;
                    posL.y = -2f;
                    var bldL = Object.Instantiate(prefabL, posL, segRot * Quaternion.Euler(0f, 90f, 0f), cityRoot.transform);
                    bldL.name = $"CityTower_L_{i}";
                    float hScaleL = 1.3f + Mathf.Abs(Mathf.Sin(i * 1.5f)) * 1.2f;
                    bldL.transform.localScale = new Vector3(1.4f, hScaleL, 1.4f);

                    // Gedung Kanan
                    GameObject prefabR = buildingPrefabs[(i + 3) % buildingPrefabs.Count];
                    Vector3 posR = currentPos + (rightDir * bDistR) + forwardDir * 6f;
                    posR.y = -2f;
                    var bldR = Object.Instantiate(prefabR, posR, segRot * Quaternion.Euler(0f, -90f, 0f), cityRoot.transform);
                    bldR.name = $"CityTower_R_{i}";
                    float hScaleR = 1.2f + Mathf.Abs(Mathf.Cos(i * 1.7f)) * 1.3f;
                    bldR.transform.localScale = new Vector3(1.4f, hScaleR, 1.4f);
                }

                // 5. Gerbang Lengkung Holo (Holo Arch)
                if (i > 2 && i % 7 == 0 && i < numSegments - 2)
                {
                    var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    arch.name = $"HoloArch_{i}";
                    arch.transform.SetParent(archesRoot.transform);
                    arch.transform.position = currentPos + forwardDir * 6f + Vector3.up * 6.8f;
                    arch.transform.rotation = segRot;
                    arch.transform.localScale = new Vector3(roadWidth + 3.5f, 1.0f, 1.2f);
                    arch.GetComponent<Renderer>().sharedMaterial = (i % 14 == 0) ? m.NeonCyan : m.NeonMagenta;

                    // Neon Light pada Gerbang
                    var archLight = new GameObject("ArchLight");
                    archLight.transform.SetParent(arch.transform, false);
                    var l = archLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 16f;
                    l.color = (i % 14 == 0) ? new Color(0f, 0.95f, 1f) : new Color(1f, 0.15f, 0.85f);
                    l.intensity = 2.0f;
                }

                // 6. Data Orbs / Koin Balap (Floating Glowing Orbs)
                if (i > 1 && i % 2 == 0 && i < numSegments - 2)
                {
                    float coinXJitter = Mathf.Sin(i * 1.8f) * (roadWidth * 0.30f);
                    Vector3 coinPos = currentPos + forwardDir * 6f + rightDir * coinXJitter + Vector3.up * 1.15f;

                    var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    coin.name = $"DataNode_{i}";
                    coin.transform.SetParent(coinsRoot.transform);
                    coin.transform.position = coinPos;
                    coin.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                    coin.GetComponent<Renderer>().sharedMaterial = m.CyberCoin;
                    coin.GetComponent<Collider>().isTrigger = true;

                    var node = coin.AddComponent<CollectibleNode>();
                    node.pickupAudio = coinClip;
                }
            }

            // Gerbang Garis Akhir
            float finalZ = trackLength - 10f;
            float finalX = Mathf.Sin(finalZ * 0.02f) * 22f + Mathf.Cos(finalZ * 0.05f) * 8f;
            CreateFinishArch(new Vector3(finalX, 0f, finalZ), m.NeonCyan, worldRoot);

            // 7. MOBIL BALAP 3D NYATA (Synty Muscle Car)
            float spawnZ = 6f;
            float spawnX = Mathf.Sin(spawnZ * 0.02f) * 22f + Mathf.Cos(spawnZ * 0.05f) * 8f;
            GameObject carObj = BuildCyberCar(new Vector3(spawnX, 0.45f, spawnZ), m);
            float nextSpawnZ = spawnZ + 2f;
            float nextSpawnX = Mathf.Sin(nextSpawnZ * 0.02f) * 22f + Mathf.Cos(nextSpawnZ * 0.05f) * 8f;
            Vector3 trackForward = (new Vector3(nextSpawnX, 0f, nextSpawnZ) - new Vector3(spawnX, 0f, spawnZ)).normalized;
            carObj.transform.rotation = Quaternion.LookRotation(trackForward);
            var carController = carObj.GetComponent<CarController>();

            // Kamera Pelacak Sinematik
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            camObj.AddComponent<AudioListener>();

            // Pastikan Universal Additional Camera Data aktif
            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var followCam = camObj.AddComponent<RacingCamera>();
            followCam.target = carObj.transform;

            // Audio & SFX Manager
            var soundObj = new GameObject("CyberSoundManager");
            var soundMgr = soundObj.AddComponent<CyberSoundManager>();
            soundMgr.externalChimeClip = coinClip;

            // GameManager Balapan
            var gmObj = new GameObject("RacingGameManager");
            var gm = gmObj.AddComponent<RacingGameManager>();
            gm.totalNodes = 22;
            gm.totalRaceTime = 95f;

            // HUD Layar Sentuh Modern (Speedometer + Pedal Sentuh + Countdown)
            var hudObj = new GameObject("CyberHUD");
            var hud = hudObj.AddComponent<RacingHUD>();
            hud.car = carController;

            EditorSceneManager.SaveScene(sc, $"{ScenePath}/CyberpunkHighway.unity");
        }

        // ── 2. MAIN MENU: CYBERPUNK METROPOLIS HANGAR ─────────────────────────
        private static void BuildMainMenuScene(CyberMats m)
        {
            Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCyberpunkLighting();
            SetupPostProcessing();

            var root = new GameObject("--- CYBERPUNK MAIN MENU ---");

            // Podium Bulat Bercahaya
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "PodiumFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = Vector3.down * 0.3f;
            floor.transform.localScale = new Vector3(14f, 0.6f, 14f);
            floor.GetComponent<Renderer>().sharedMaterial = m.CyberRoad;

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PodiumNeonRim";
            rim.transform.SetParent(root.transform);
            rim.transform.position = Vector3.down * 0.28f;
            rim.transform.localScale = new Vector3(14.4f, 0.58f, 14.4f);
            rim.GetComponent<Renderer>().sharedMaterial = m.NeonCyan;

            // Mobil Balap Pameran 3D Berputar
            var car = BuildCyberCar(new Vector3(0f, 0.05f, 0f), m);
            car.transform.SetParent(root.transform);
            Object.DestroyImmediate(car.GetComponent<CarController>());
            Object.DestroyImmediate(car.GetComponent<CarInputManager>());
            Object.DestroyImmediate(car.GetComponent<Rigidbody>());

            // Gedung Latar Belakang 3D Synty
            List<GameObject> buildingPrefabs = LoadBuildingPrefabs();
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f * Mathf.Deg2Rad;
                Vector3 bPos = new Vector3(Mathf.Sin(ang) * 45f, -2f, Mathf.Cos(ang) * 45f);

                if (buildingPrefabs.Count > 0)
                {
                    GameObject p = buildingPrefabs[i % buildingPrefabs.Count];
                    var b = Object.Instantiate(p, bPos, Quaternion.Euler(0f, -i * 45f, 0f), root.transform);
                    b.transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);
                }
            }

            // Kamera Menu 3/4 Sinematik
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            cam.transform.position = new Vector3(0f, 2.3f, -7.0f);
            cam.transform.LookAt(new Vector3(0f, 0.9f, 0f));
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // Main Menu Controller
            var menuObj = new GameObject("CyberpunkMainMenu");
            var menu = menuObj.AddComponent<CyberpunkMainMenuUI>();
            menu.showcaseCar = car.transform;
            menu.carBodyRenderer = car.GetComponentInChildren<Renderer>();

            EditorSceneManager.SaveScene(sc, $"{ScenePath}/CyberpunkMainMenu.unity");
        }

        // ── LOAD GEDUNG 3D DARI SYNTY STUDIOS ─────────────────────────────────
        private static List<GameObject> LoadBuildingPrefabs()
        {
            var list = new List<GameObject>();
            string[] paths = new string[]
            {
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_OfficeSquare_01.prefab",
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_OfficeRound_01.prefab",
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_OfficeOctagon_01.prefab",
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_Apartment_01.prefab",
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_Apartment_02.prefab",
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_Shop_01.prefab",
                "Assets/SyntyStudios/PolygonCity/Prefabs/Buildings/SM_Bld_Spire_01.prefab",
                "Assets/SyntyStudios/PolygonSamples/PolygonSciFiCity/Prefabs/SM_Bld_Large_01.prefab",
                "Assets/SyntyStudios/PolygonSamples/PolygonSciFiCity/Prefabs/SM_Bld_Large_05.prefab",
            };

            foreach (var p in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
            }
            return list;
        }

        // ── LIGHTING SETUP: CYBERPUNK SYNTHWAVE SUNSET ───────────────────────
        private static void SetupCyberpunkLighting()
        {
            // Gunakan Langit Senja Keemasan
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/LangitSenja.mat");
            if (skybox == null)
            {
                skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SunsetValley/Mat_SunsetSkybox.mat");
            }
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
            }

            // 1. Cahaya Matahari Senja Keemasan (Golden Hour Sun)
            var sunObj = new GameObject("Directional Light (Cahaya Matahari Senja)");
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.76f, 0.45f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            // Rendah di cakrawala menyinari sepanjang lintasan
            sunObj.transform.rotation = Quaternion.Euler(14f, 175f, 0f);
            RenderSettings.sun = sun;

            // 2. Cahaya Rim Synthwave Cyan (Neon Rim Fill)
            var rimObj = new GameObject("Directional Light (Cyan Synthwave Rim)");
            var rim = rimObj.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.18f, 0.82f, 1.0f);
            rim.intensity = 0.65f;
            rimObj.transform.rotation = Quaternion.Euler(32f, -40f, 0f);

            // Ambient Trilight Senja
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.65f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.28f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.12f, 0.20f);

            // Kabut Senja
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.70f, 0.48f, 0.42f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
        }

        // ── POST-PROCESSING: BLOOM NEON & TONEMAPPING ────────────────────────
        private static void SetupPostProcessing()
        {
            var volumeObj = new GameObject("Global PostProcess Volume");
            var vol = volumeObj.AddComponent<Volume>();
            vol.isGlobal = true;

            string profPath = "Assets/Settings/DefaultVolumeProfile.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profPath);
            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(prof, profPath);
            }

            // Bloom
            if (!prof.TryGet<Bloom>(out var bloom)) bloom = prof.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.85f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.35f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.70f;

            // Tonemapping
            if (!prof.TryGet<Tonemapping>(out var tone)) tone = prof.Add<Tonemapping>();
            tone.active = true;
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;

            // Vignette
            if (!prof.TryGet<Vignette>(out var vig)) vig = prof.Add<Vignette>();
            vig.active = true;
            vig.intensity.overrideState = true;
            vig.intensity.value = 0.25f;

            vol.profile = prof;
        }

        // ── MOBIL SPORT 3D ASLI (SYNTY MUSCLE CAR) ───────────────────────────
        private static GameObject BuildCyberCar(Vector3 pos, CyberMats m)
        {
            var carRoot = new GameObject("CyberSportsCar");
            carRoot.tag = "Player";
            carRoot.transform.position = pos;

            var rb = carRoot.AddComponent<Rigidbody>();
            rb.mass = 1200f;

            // Muat Prefab 3D Synty Muscle Car
            string prefabPath = "Assets/SyntyStudios/PolygonCity/Prefabs/Vehicles/SM_Veh_Car_Muscle_01.prefab";
            GameObject musclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject modelInstance;
            if (musclePrefab != null)
            {
                modelInstance = Object.Instantiate(musclePrefab, carRoot.transform, false);
                modelInstance.name = "MuscleCarVisual";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;
            }
            else
            {
                // Fallback darurat
                modelInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                modelInstance.name = "MuscleCarVisual";
                modelInstance.transform.SetParent(carRoot.transform, false);
                modelInstance.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                modelInstance.transform.localScale = new Vector3(1.85f, 0.75f, 4.0f);
            }

            // Temukan roda di dalam prefab Synty
            Transform fl = modelInstance.transform.Find("SM_Veh_Car_Muscle_Wheel_fl");
            Transform fr = modelInstance.transform.Find("SM_Veh_Car_Muscle_Wheel_fr");
            Transform rl = modelInstance.transform.Find("SM_Veh_Car_Muscle_Wheel_rl");
            Transform rr = modelInstance.transform.Find("SM_Veh_Car_Muscle_Wheel_rr");

            // Lampu Depan LED Cyan
            CreateCarLight(carRoot.transform, new Vector3(-0.65f, 0.55f, 2.1f), new Color(0f, 0.95f, 1f));
            CreateCarLight(carRoot.transform, new Vector3( 0.65f, 0.55f, 2.1f), new Color(0f, 0.95f, 1f));

            // Lampu Belakang Merah
            CreateCarLight(carRoot.transform, new Vector3(-0.65f, 0.55f, -2.1f), new Color(1f, 0.1f, 0.1f), isHeadlight: false);
            CreateCarLight(carRoot.transform, new Vector3( 0.65f, 0.55f, -2.1f), new Color(1f, 0.1f, 0.1f), isHeadlight: false);

            // Neon Underglow Bawah Mobil
            var underglow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            underglow.name = "NeonUnderglow";
            underglow.transform.SetParent(carRoot.transform, false);
            underglow.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            underglow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            underglow.transform.localScale = new Vector3(1.9f, 3.8f, 1f);
            underglow.GetComponent<Renderer>().sharedMaterial = m.NeonCyan;
            DestroyImmediate(underglow.GetComponent<Collider>());

            // Collider Fisika Bebas Hambatan Dinding
            var smoothMat = GetOrCreateFrictionlessPhysMat();

            var boxCol = carRoot.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.55f, 0f);
            boxCol.size = new Vector3(1.85f, 1.0f, 4.0f);
            boxCol.sharedMaterial = smoothMat;

            // Bumper Bulat Depan (Mencegah sudut tajam tersangkut pada sambungan pagar tol)
            var bumperCol = carRoot.AddComponent<SphereCollider>();
            bumperCol.center = new Vector3(0f, 0.45f, 1.6f);
            bumperCol.radius = 0.8f;
            bumperCol.sharedMaterial = smoothMat;

            // Audio Mesin
            var audio = carRoot.AddComponent<AudioSource>();
            audio.loop = true;
            audio.playOnAwake = true;
            audio.spatialBlend = 0f;
            audio.volume = 0.55f;
            audio.pitch = 0.95f;

            // Controller Kendaraan
            carRoot.AddComponent<CarInputManager>();
            var ctrl = carRoot.AddComponent<CarController>();
            ctrl.frontLeftWheel = fl;
            ctrl.frontRightWheel = fr;
            ctrl.rearLeftWheel = rl;
            ctrl.rearRightWheel = rr;
            ctrl.carBodyVisual = modelInstance.transform;
            ctrl.engineAudio = audio;

            return carRoot;
        }

        private static void CreateCarLight(Transform parent, Vector3 localPos, Color col, bool isHeadlight = true)
        {
            var lightObj = new GameObject(isHeadlight ? "HeadLight" : "TailLight");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.localPosition = localPos;
            if (isHeadlight) lightObj.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            var l = lightObj.AddComponent<Light>();
            l.type = isHeadlight ? LightType.Spot : LightType.Point;
            l.color = col;
            l.intensity = isHeadlight ? 3.0f : 1.5f;
            l.range = isHeadlight ? 22f : 4f;
            if (isHeadlight) l.spotAngle = 65f;
        }

        private static void CreateFinishArch(Vector3 pos, Material archMat, GameObject parent)
        {
            var archRoot = new GameObject("FinishGate_Arch");
            archRoot.transform.SetParent(parent.transform);
            archRoot.transform.position = pos;

            var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch.name = "ArchBeam";
            arch.transform.SetParent(archRoot.transform, false);
            arch.transform.localPosition = new Vector3(0f, 5.0f, 0f);
            arch.transform.localScale = new Vector3(16f, 1.5f, 2.0f);
            arch.GetComponent<Renderer>().sharedMaterial = archMat;

            var trigger = archRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 2.5f, 0f);
            trigger.size = new Vector3(14f, 5f, 2f);
            archRoot.AddComponent<FinishLineTrigger>();
        }

        private static void RegisterScenes()
        {
            string[] paths = {
                $"{ScenePath}/CyberpunkMainMenu.unity",
                $"{ScenePath}/CyberpunkHighway.unity"
            };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths)
            {
                if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[CyberpunkRacing] Mendaftarkan {list.Count} scene Cyberpunk ke Build Settings!");
        }

        private static void OpenScene(string fileName)
        {
            string full = $"{ScenePath}/{fileName}";
            if (File.Exists(full))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(full);
            }
        }

        // ── BUILD ANDROID APK ─────────────────────────────────────────────────
        public static void BuildAndroidAPK()
        {
            Debug.Log("[CyberpunkRacing] Memulai Build Android APK...");
            EnsureFolders();
            RegisterScenes();

            string apkPath = "Builds/Android/CyberpunkRacing.apk";

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.Lumora.CyberpunkRacing");
            PlayerSettings.productName = "Cyberpunk Racing";
            PlayerSettings.companyName = "Lumora";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && File.Exists(s.path)) scenes.Add(s.path);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[CyberpunkRacing] APK Berhasil di-build: {apkPath} ({summary.totalSize / 1024 / 1024} MB)");
            }
            else
            {
                Debug.LogError($"[CyberpunkRacing] Build APK Gagal: {summary.result}");
            }
        }
    }
}
