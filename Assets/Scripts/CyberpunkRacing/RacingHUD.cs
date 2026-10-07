using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkRacing
{
    /// <summary>
    /// HUD On-Screen Racing Cyberpunk:
    /// - Speedometer Digital Futuristik (KM/H) & Nitro Energy Gauge
    /// - Frosted Glass Top Panel (Data Nodes & Timer)
    /// - Tombol Sentuh On-Screen: Gas, Rem, Nitro, Kemudi Kiri/Kanan, & Jeda
    /// - Modal Pause, Kemenangan, & Kekalahan
    /// </summary>
    public class RacingHUD : MonoBehaviour
    {
        public static RacingHUD Instance { get; private set; }

        public CarController car;

        // Tekstur Prosedural On-Screen
        private Texture2D _pedalGasTex;
        private Texture2D _pedalBrakeTex;
        private Texture2D _pedalNitroTex;
        private Texture2D _glassHeaderTex;
        private Texture2D _whiteBarTex;

        // GUI Styles
        private bool _stylesReady = false;
        private GUIStyle _hudHeaderLabel;
        private GUIStyle _hudHeaderVal;
        private GUIStyle _speedoValStyle;
        private GUIStyle _speedoUnitStyle;
        private GUIStyle _btnTextStyle;
        private GUIStyle _countdownStyle;
        private GUIStyle _modalTitleStyle;
        private GUIStyle _modalBodyStyle;
        private GUIStyle _menuBtnStyle;

        private void Awake()
        {
            Instance = this;
            GenerateTextures();
        }

        private void Start()
        {
            if (car == null)
            {
                car = FindAnyObjectByType<CarController>();
            }
        }

        private void Update()
        {
            // Keyboard ESC / P untuk Jeda
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
            {
                RacingGameManager.Instance?.TogglePause();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                RacingGameManager.Instance?.TogglePause();
            }
#endif
        }

        private void OnDestroy()
        {
            // Bersihkan tekstur agar tidak bocor di memori
            if (_pedalGasTex != null) Destroy(_pedalGasTex);
            if (_pedalBrakeTex != null) Destroy(_pedalBrakeTex);
            if (_pedalNitroTex != null) Destroy(_pedalNitroTex);
            if (_glassHeaderTex != null) Destroy(_glassHeaderTex);
            if (_whiteBarTex != null) Destroy(_whiteBarTex);
        }

        private void OnGUI()
        {
            EnsureStyles();

            var gm = RacingGameManager.Instance;
            if (gm == null) return;

            DrawTopHeader(gm);
            DrawSpeedometerAndNitro();
            DrawTouchControls();

            // Hitung mundur 3-2-1-GO
            if (gm.State == GameState.Countdown)
            {
                DrawCountdown(gm.CountdownTimer);
            }
            else if (gm.State == GameState.Paused)
            {
                DrawPauseModal(gm);
            }
            else if (gm.State == GameState.Finished)
            {
                DrawFinishedModal(gm);
            }
        }

        // ── 1. Frosted Glass Top Panel ─────────────────────────────────────────
        private void DrawTopHeader(RacingGameManager gm)
        {
            int sw = Screen.width;
            int sh = Screen.height;

            float panelW = Mathf.Clamp(sw * 0.54f, 320f, 620f);
            float panelH = Mathf.Clamp(sh * 0.16f, 75f, 110f);
            Rect rect = new Rect((sw - panelW) * 0.5f, 16f, panelW, panelH);

            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.08f, 0.15f, 0.88f);
            GUI.DrawTexture(rect, _glassHeaderTex);
            GUI.color = new Color(0f, 0.95f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 3f, rect.width, 3f), _whiteBarTex);
            GUI.color = prev;

            float colW = panelW / 3f;

            // Kolom 1: Data Nodes
            Rect c1 = new Rect(rect.x, rect.y + 12f, colW, panelH - 24f);
            GUI.Label(new Rect(c1.x, c1.y, c1.width, 20f), "DATA NODES", _hudHeaderLabel);
            GUI.Label(new Rect(c1.x, c1.y + 20f, c1.width, 32f), $"💠 {gm.collectedNodes:00}/{gm.totalNodes:00}", _hudHeaderVal);

            // Kolom 2: Waktu Tersisa
            Rect c2 = new Rect(rect.x + colW, rect.y + 12f, colW, panelH - 24f);
            GUI.Label(new Rect(c2.x, c2.y, c2.width, 20f), "SISA WAKTU", _hudHeaderLabel);
            int m = Mathf.FloorToInt(gm.TimeRemaining / 60f);
            int s = Mathf.FloorToInt(gm.TimeRemaining % 60f);
            GUI.Label(new Rect(c2.x, c2.y + 20f, c2.width, 32f), $"⏱ {m:00}:{s:00}", _hudHeaderVal);

            // Kolom 3: Waktu Balapan Berjalan
            Rect c3 = new Rect(rect.x + colW * 2f, rect.y + 12f, colW, panelH - 24f);
            GUI.Label(new Rect(c3.x, c3.y, c3.width, 20f), "LAP TIME", _hudHeaderLabel);
            GUI.Label(new Rect(c3.x, c3.y + 20f, c3.width, 32f), $"{gm.ElapsedTime:00.0}s", _hudHeaderVal);

            // Tombol Pause di pojok kanan atas
            float pauseSize = Mathf.Clamp(sh * 0.10f, 48f, 64f);
            Rect pauseRect = new Rect(sw - pauseSize - 18f, 18f, pauseSize, pauseSize);
            Color pBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.2f, 0.35f, 0.9f);
            if (GUI.Button(pauseRect, "⏸", _btnTextStyle))
            {
                gm.TogglePause();
            }
            GUI.backgroundColor = pBg;
        }

        // ── 2. Speedometer & Nitro Gauge ──────────────────────────────────────
        private void DrawSpeedometerAndNitro()
        {
            if (car == null) return;

            int sw = Screen.width;
            int sh = Screen.height;

            float speed = car.CurrentSpeedKmh;
            float ratio = car.SpeedRatio;

            // Box Speedometer di kiri tengah/bawah
            float spdW = Mathf.Clamp(sw * 0.22f, 150f, 220f);
            float spdH = Mathf.Clamp(sh * 0.18f, 90f, 130f);
            Rect spdRect = new Rect(24f, sh - spdH - 24f, spdW, spdH);

            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.82f);
            GUI.DrawTexture(spdRect, _glassHeaderTex);
            GUI.color = prev;

            GUI.Label(new Rect(spdRect.x, spdRect.y + 8f, spdRect.width, 48f), $"{Mathf.RoundToInt(speed)}", _speedoValStyle);
            GUI.Label(new Rect(spdRect.x, spdRect.y + 54f, spdRect.width, 20f), "KM / H", _speedoUnitStyle);

            // Bar Nitro di bawah speedometer
            float barW = spdRect.width - 24f;
            float barH = 10f;
            Rect nitroBgRect = new Rect(spdRect.x + 12f, spdRect.y + spdH - 18f, barW, barH);

            GUI.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            GUI.DrawTexture(nitroBgRect, _whiteBarTex);

            float nRatio = car.NitroRatio;
            Rect nitroFillRect = new Rect(nitroBgRect.x, nitroBgRect.y, barW * nRatio, barH);
            GUI.color = car.IsNitroActive ? new Color(1f, 0.4f, 0.1f, 1f) : new Color(0f, 0.95f, 1f, 0.95f);
            GUI.DrawTexture(nitroFillRect, _whiteBarTex);
            GUI.color = prev;
        }

        // ── 3. Touch Controls (Mobile On-Screen) ────────────────────────────────
        private void DrawTouchControls()
        {
            int sw = Screen.width;
            int sh = Screen.height;

            float btnSize = Mathf.Clamp(sh * 0.16f, 75f, 115f);
            float padBottom = 22f;
            float padEdge = 24f;

            // Kanan Bawah: Pedal GAS
            Rect gasRect = new Rect(sw - btnSize - padEdge, sh - btnSize - padBottom, btnSize, btnSize);
            // Kanan Bawah: Tombol NITRO di atas Gas
            Rect nitroRect = new Rect(sw - btnSize - padEdge, sh - (btnSize * 2f) - padBottom - 16f, btnSize, btnSize * 0.85f);
            // Kiri Bawah: Pedal REM
            Rect brakeRect = new Rect(padEdge + btnSize * 2.2f, sh - btnSize - padBottom, btnSize, btnSize);

            // Kiri Bawah: Tombol KEMUDI Kiri & Kanan
            float steerW = btnSize * 0.95f;
            Rect leftRect = new Rect(padEdge, sh - btnSize - padBottom, steerW, btnSize);
            Rect rightRect = new Rect(padEdge + steerW + 10f, sh - btnSize - padBottom, steerW, btnSize);

            // Deteksi sentuhan aktif
            bool gasDown = false;
            bool brakeDown = false;
            bool nitroDown = false;
            float touchSteer = 0f;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    Vector2 pos = new Vector2(t.position.x, Screen.height - t.position.y);

                    if (gasRect.Contains(pos)) gasDown = true;
                    if (brakeRect.Contains(pos)) brakeDown = true;
                    if (nitroRect.Contains(pos)) nitroDown = true;
                    if (leftRect.Contains(pos)) touchSteer -= 1f;
                    if (rightRect.Contains(pos)) touchSteer += 1f;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 pos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (gasRect.Contains(pos)) gasDown = true;
                if (brakeRect.Contains(pos)) brakeDown = true;
                if (nitroRect.Contains(pos)) nitroDown = true;
                if (leftRect.Contains(pos)) touchSteer -= 1f;
                if (rightRect.Contains(pos)) touchSteer += 1f;
            }

            // Kirim ke Input Manager
            CarInputManager.TouchThrottleInput = gasDown ? 1f : (brakeDown ? -1f : 0f);
            CarInputManager.TouchSteerInput = Mathf.Clamp(touchSteer, -1f, 1f);
            CarInputManager.TouchBrakeHeld = brakeDown;
            CarInputManager.TouchNitroHeld = nitroDown;

            // Gambar Tombol Gas
            Color prev = GUI.color;
            GUI.color = gasDown ? new Color(0.2f, 1f, 0.4f, 0.95f) : new Color(0f, 0.9f, 0.95f, 0.85f);
            GUI.DrawTexture(gasRect, _pedalGasTex);
            GUI.Label(gasRect, "⮅\nGAS", _btnTextStyle);

            // Gambar Tombol Nitro
            GUI.color = nitroDown ? new Color(1f, 0.3f, 0.1f, 0.98f) : new Color(1f, 0.75f, 0.05f, 0.88f);
            GUI.DrawTexture(nitroRect, _pedalNitroTex);
            GUI.Label(nitroRect, "⚡\nNITRO", _btnTextStyle);

            // Gambar Tombol Rem
            GUI.color = brakeDown ? new Color(1f, 0.2f, 0.2f, 0.95f) : new Color(0.9f, 0.3f, 0.3f, 0.82f);
            GUI.DrawTexture(brakeRect, _pedalBrakeTex);
            GUI.Label(brakeRect, "⏹\nREM", _btnTextStyle);

            // Tombol Kemudi (Jika tidak memakai gyro tilt)
            if (CarInputManager.Instance != null && !CarInputManager.Instance.useTiltSteering)
            {
                GUI.color = touchSteer < -0.1f ? new Color(0f, 1f, 1f, 0.98f) : new Color(0.1f, 0.4f, 0.7f, 0.8f);
                GUI.DrawTexture(leftRect, _pedalBrakeTex);
                GUI.Label(leftRect, "◀\nKIRI", _btnTextStyle);

                GUI.color = touchSteer > 0.1f ? new Color(0f, 1f, 1f, 0.98f) : new Color(0.1f, 0.4f, 0.7f, 0.8f);
                GUI.DrawTexture(rightRect, _pedalBrakeTex);
                GUI.Label(rightRect, "▶\nKANAN", _btnTextStyle);
            }

            GUI.color = prev;
        }

        // ── 4. Countdown 3, 2, 1, GO ───────────────────────────────────────────
        private void DrawCountdown(float timer)
        {
            int count = Mathf.CeilToInt(timer);
            string text = count switch
            {
                3 => "3",
                2 => "2",
                1 => "1",
                _ => "GO! 🚀"
            };

            float w = 400f;
            float h = 140f;
            Rect rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.42f, w, h);
            GUI.Label(rect, text, _countdownStyle);
        }

        // ── 5. Modal Pause ────────────────────────────────────────────────────
        private void DrawPauseModal(RacingGameManager gm)
        {
            float mW = Mathf.Min(480f, Screen.width - 40f);
            float mH = 320f;
            Rect modal = new Rect((Screen.width - mW) * 0.5f, (Screen.height - mH) * 0.5f, mW, mH);

            DrawModalBackground(modal);
            GUI.Label(new Rect(modal.x, modal.y + 20f, modal.width, 36f), "⏸ PERMAINAN DIJEDA", _modalTitleStyle);

            float bW = modal.width - 60f;
            float bX = modal.x + 30f;
            float curY = modal.y + 75f;
            float bH = 50f;

            if (GUI.Button(new Rect(bX, curY, bW, bH), "LANJUTKAN ▶", _menuBtnStyle))
            {
                gm.TogglePause();
            }
            curY += bH + 14f;

            if (GUI.Button(new Rect(bX, curY, bW, bH), "ULANGI BALAPAN ↺", _menuBtnStyle))
            {
                gm.RestartRace();
            }
            curY += bH + 14f;

            if (GUI.Button(new Rect(bX, curY, bW, bH), "MENU UTAMA 🏠", _menuBtnStyle))
            {
                gm.GoToMainMenu();
            }
        }

        // ── 6. Modal Selesai (Menang / Kalah) ──────────────────────────────────
        private void DrawFinishedModal(RacingGameManager gm)
        {
            float mW = Mathf.Min(520f, Screen.width - 40f);
            float mH = 380f;
            Rect modal = new Rect((Screen.width - mW) * 0.5f, (Screen.height - mH) * 0.5f, mW, mH);

            DrawModalBackground(modal);

            string title = gm.PlayerWon ? "🏆 FINISH! KAMU MENANG! 🏆" : "💥 WAKTU HABIS! COBA LAGI! 💥";
            GUI.Label(new Rect(modal.x, modal.y + 20f, modal.width, 36f), title, _modalTitleStyle);

            string info = $"💠 Data Nodes Dikumpulkan: {gm.collectedNodes} / {gm.totalNodes}\n" +
                          $"⏱ Waktu Balapan: {gm.ElapsedTime:0.00} detik\n" +
                          $"🏎 Kecepatan Rata-rata: ~{Mathf.RoundToInt(car != null ? car.CurrentSpeedKmh : 0f)} KM/H";
            GUI.Label(new Rect(modal.x + 24f, modal.y + 70f, modal.width - 48f, 90f), info, _modalBodyStyle);

            float bW = modal.width - 60f;
            float bX = modal.x + 30f;
            float curY = modal.y + 180f;
            float bH = 54f;

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0.95f, 1f);
            if (GUI.Button(new Rect(bX, curY, bW, bH), gm.PlayerWon ? "MAIN LAGI 🚀" : "COBA LAGI ↺", _menuBtnStyle))
            {
                gm.RestartRace();
            }
            GUI.backgroundColor = prev;
            curY += bH + 16f;

            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.9f), "MENU UTAMA 🏠", _menuBtnStyle))
            {
                gm.GoToMainMenu();
            }
        }

        private void DrawModalBackground(Rect rect)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);
            GUI.DrawTexture(rect, _glassHeaderTex);
            GUI.color = new Color(0f, 0.95f, 1f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), _whiteBarTex);
            GUI.color = new Color(1f, 0.1f, 0.85f, 0.9f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 3f, rect.width, 3f), _whiteBarTex);
            GUI.color = prev;
        }

        // ── Inisialisasi Tekstur & Style ────────────────────────────────────────
        private void GenerateTextures()
        {
            _pedalGasTex = CreateRoundedTexture(128, 128, 64, new Color(0f, 0.8f, 0.85f, 0.92f), new Color(0.3f, 1f, 1f, 1f), 4);
            _pedalNitroTex = CreateRoundedTexture(128, 128, 64, new Color(1f, 0.65f, 0.05f, 0.92f), new Color(1f, 0.95f, 0.4f, 1f), 4);
            _pedalBrakeTex = CreateRoundedTexture(128, 128, 28, new Color(0.85f, 0.25f, 0.25f, 0.92f), new Color(1f, 0.45f, 0.45f, 1f), 4);
            _glassHeaderTex = CreateRoundedTexture(256, 128, 20, new Color(1f, 1f, 1f, 0.7f), new Color(1f, 1f, 1f, 0.9f), 2);
            _whiteBarTex = Texture2D.whiteTexture;
        }

        private static Texture2D CreateRoundedTexture(int width, int height, int radius, Color fill, Color border, int borderThickness)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (width - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (height - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > radius - borderThickness)
                    {
                        tex.SetPixel(x, y, border);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fill);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            int sh = Screen.height;

            _hudHeaderLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 55, 11, 14),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0f, 0.95f, 1f) }
            };

            _hudHeaderVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 30, 16, 24),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _speedoValStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 11, 46, 78),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _speedoUnitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 45, 12, 17),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0f, 0.95f, 1f) }
            };

            _btnTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 38, 14, 20),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _countdownStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 7, 72, 140),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.2f) }
            };

            _modalTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 26, 20, 28),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0f, 0.95f, 1f) }
            };

            _modalBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 42, 15, 20),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _menuBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(sh / 40, 15, 20),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesReady = true;
        }
    }
}
