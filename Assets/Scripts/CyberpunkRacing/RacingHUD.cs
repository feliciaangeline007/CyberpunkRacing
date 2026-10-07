using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkRacing
{
    /// <summary>
    /// HUD On-Screen Racing Cyberpunk Berestetika Tinggi:
    /// - Speedometer Digital Futuristik (KM/H) & Nitro Energy Gauge
    /// - Frosted Glass Top Panel (Data Nodes, Sisa Waktu & Waktu Berjalan)
    /// - Tombol Sentuh On-Screen: Gas, Rem, Nitro, Kemudi Kiri/Kanan, Reset Lintasan, & Jeda
    /// - Petunjuk Kontrol Keyboard PC di layar
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
        private GUIStyle _hintTextStyle;
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
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                {
                    RacingGameManager.Instance?.TogglePause();
                }
            }
            catch {}
#endif
        }

        private void OnDestroy()
        {
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
            DrawControlsHint();

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
            float panelH = Mathf.Clamp(sh * 0.15f, 70f, 100f);
            Rect rect = new Rect((sw - panelW) * 0.5f, 14f, panelW, panelH);

            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.08f, 0.15f, 0.90f);
            GUI.DrawTexture(rect, _glassHeaderTex);
            GUI.color = new Color(0f, 0.95f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 3f, rect.width, 3f), _whiteBarTex);
            GUI.color = prev;

            float colW = panelW / 3f;

            // Kolom 1: Data Nodes
            Rect c1 = new Rect(rect.x, rect.y + 10f, colW, panelH - 20f);
            GUI.Label(new Rect(c1.x, c1.y, c1.width, 18f), "DATA NODES", _hudHeaderLabel);
            GUI.Label(new Rect(c1.x, c1.y + 18f, c1.width, 32f), $"💠 {gm.collectedNodes:00}/{gm.totalNodes:00}", _hudHeaderVal);

            // Kolom 2: Sisa Waktu
            Rect c2 = new Rect(rect.x + colW, rect.y + 10f, colW, panelH - 20f);
            GUI.Label(new Rect(c2.x, c2.y, c2.width, 18f), "SISA WAKTU", _hudHeaderLabel);
            int m = Mathf.FloorToInt(gm.TimeRemaining / 60f);
            int s = Mathf.FloorToInt(gm.TimeRemaining % 60f);
            GUI.Label(new Rect(c2.x, c2.y + 18f, c2.width, 32f), $"⏱ {m:00}:{s:00}", _hudHeaderVal);

            // Kolom 3: Waktu Balapan Berjalan
            Rect c3 = new Rect(rect.x + colW * 2f, rect.y + 10f, colW, panelH - 20f);
            GUI.Label(new Rect(c3.x, c3.y, c3.width, 18f), "LAP TIME", _hudHeaderLabel);
            GUI.Label(new Rect(c3.x, c3.y + 18f, c3.width, 32f), $"{gm.ElapsedTime:00.0}s", _hudHeaderVal);

            // Tombol Reset Lintasan di pojok kiri atas
            float topBtnSize = Mathf.Clamp(sh * 0.09f, 44f, 58f);
            Rect resetRect = new Rect(18f, 16f, topBtnSize * 1.5f, topBtnSize);
            Color rBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.15f, 0.35f, 0.85f);
            if (GUI.Button(resetRect, "↺ RESET", _btnTextStyle))
            {
                car?.RespawnToSafePosition();
            }

            // Tombol Pause di pojok kanan atas
            Rect pauseRect = new Rect(sw - topBtnSize - 18f, 16f, topBtnSize, topBtnSize);
            GUI.backgroundColor = new Color(0.12f, 0.22f, 0.38f, 0.9f);
            if (GUI.Button(pauseRect, "⏸", _btnTextStyle))
            {
                gm.TogglePause();
            }
            GUI.backgroundColor = rBg;
        }

        // ── 2. Speedometer & Nitro Gauge ──────────────────────────────────────
        private void DrawSpeedometerAndNitro()
        {
            if (car == null) return;

            int sw = Screen.width;
            int sh = Screen.height;

            float speed = car.CurrentSpeedKmh;

            float spdW = Mathf.Clamp(sw * 0.20f, 140f, 210f);
            float spdH = Mathf.Clamp(sh * 0.17f, 85f, 125f);
            Rect spdRect = new Rect(20f, sh - spdH - 20f, spdW, spdH);

            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.85f);
            GUI.DrawTexture(spdRect, _glassHeaderTex);
            GUI.color = prev;

            GUI.Label(new Rect(spdRect.x, spdRect.y + 6f, spdRect.width, 46f), $"{Mathf.RoundToInt(speed)}", _speedoValStyle);
            GUI.Label(new Rect(spdRect.x, spdRect.y + 50f, spdRect.width, 18f), "KM / H", _speedoUnitStyle);

            // Bar Nitro di bawah speedometer
            float barW = spdRect.width - 24f;
            float barH = 10f;
            Rect nitroBgRect = new Rect(spdRect.x + 12f, spdRect.y + spdH - 18f, barW, barH);

            GUI.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            GUI.DrawTexture(nitroBgRect, _whiteBarTex);

            float nRatio = car.NitroRatio;
            Rect nitroFillRect = new Rect(nitroBgRect.x, nitroBgRect.y, barW * nRatio, barH);
            GUI.color = car.IsNitroActive ? new Color(1f, 0.45f, 0.05f, 1f) : new Color(0f, 0.95f, 1f, 0.95f);
            GUI.DrawTexture(nitroFillRect, _whiteBarTex);
            GUI.color = prev;
        }

        // ── 3. Touch Controls (Mobile On-Screen) ────────────────────────────────
        private void DrawTouchControls()
        {
            int sw = Screen.width;
            int sh = Screen.height;

            float btnSize = Mathf.Clamp(sh * 0.16f, 75f, 115f);
            float padBottom = 20f;
            float padEdge = 20f;

            // Kanan Bawah: Pedal GAS
            Rect gasRect = new Rect(sw - btnSize - padEdge, sh - btnSize - padBottom, btnSize, btnSize);
            // Kanan Bawah: Tombol NITRO di atas Gas
            Rect nitroRect = new Rect(sw - btnSize - padEdge, sh - (btnSize * 1.9f) - padBottom - 12f, btnSize, btnSize * 0.8f);
            // Kiri Bawah: Pedal REM
            Rect brakeRect = new Rect(padEdge + btnSize * 2.1f, sh - btnSize - padBottom, btnSize, btnSize);

            // Kiri Bawah: Tombol KEMUDI Kiri & Kanan
            float steerW = btnSize * 0.95f;
            float totalSteerW = steerW * 2f + 8f;
            Rect leftRect = new Rect(padEdge, sh - btnSize - padBottom, steerW, btnSize);
            Rect rightRect = new Rect(padEdge + steerW + 8f, sh - btnSize - padBottom, steerW, btnSize);

            // Indikator Holographic Swipe Steering Bar di atas tombol belok
            Rect swipeBarRect = new Rect(padEdge, sh - btnSize - padBottom - 34f, totalSteerW, 26f);

            // Deteksi sentuhan aktif menggunakan New Input System (tanpa error legacy)
            bool gasDown = CheckPointerInRect(gasRect);
            bool brakeDown = CheckPointerInRect(brakeRect);
            bool nitroDown = CheckPointerInRect(nitroRect);
            float touchSteer = 0f;
            if (CheckPointerInRect(leftRect)) touchSteer -= 1f;
            if (CheckPointerInRect(rightRect)) touchSteer += 1f;

            // Kirim ke Input Manager
            CarInputManager.TouchThrottleInput = gasDown ? 1f : (brakeDown ? -1f : 0f);
            CarInputManager.TouchSteerButtonInput = touchSteer;
            CarInputManager.TouchBrakeHeld = brakeDown;
            CarInputManager.TouchNitroHeld = nitroDown;

            // Gambar Indikator Swipe Steering Bar
            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.08f, 0.16f, 0.85f);
            GUI.DrawTexture(swipeBarRect, _glassHeaderTex);

            // Garis pembagi tengah
            GUI.color = new Color(0.2f, 0.4f, 0.6f, 0.5f);
            GUI.DrawTexture(new Rect(swipeBarRect.x + swipeBarRect.width * 0.5f - 1f, swipeBarRect.y + 4f, 2f, swipeBarRect.height - 8f), _whiteBarTex);

            // Kursor posisi swipe aktif
            float swipeNorm = CarInputManager.Instance != null ? CarInputManager.Instance.CurrentSwipeOffset : 0f;
            float cursorX = swipeBarRect.x + (swipeBarRect.width * 0.5f) + (swipeNorm * (swipeBarRect.width * 0.42f)) - 8f;
            Rect cursorRect = new Rect(cursorX, swipeBarRect.y + 3f, 16f, swipeBarRect.height - 6f);
            GUI.color = Mathf.Abs(swipeNorm) > 0.05f ? new Color(0f, 1f, 0.9f, 0.95f) : new Color(0.4f, 0.7f, 1f, 0.75f);
            GUI.DrawTexture(cursorRect, _whiteBarTex);

            // Label Swipe
            string swipeLabel = (CarInputManager.Instance != null && CarInputManager.Instance.IsGyroActive) ? "📱 TILT STEER" : "👈 SWIPE BELOK 👉";
            GUI.Label(swipeBarRect, swipeLabel, _hintTextStyle);

            // Gambar Tombol Gas (Auto-Gas aktif)
            GUI.color = gasDown ? new Color(0.2f, 1f, 0.4f, 0.95f) : new Color(0f, 0.9f, 0.95f, 0.9f);
            GUI.DrawTexture(gasRect, _pedalGasTex);
            GUI.Label(gasRect, "⮅\nAUTO\nGAS", _btnTextStyle);

            // Gambar Tombol Rem
            GUI.color = brakeDown ? new Color(1f, 0.25f, 0.25f, 0.95f) : new Color(0.9f, 0.2f, 0.4f, 0.85f);
            GUI.DrawTexture(brakeRect, _pedalBrakeTex);
            GUI.Label(brakeRect, "⮇\nREM", _btnTextStyle);

            // Gambar Tombol Nitro
            GUI.color = nitroDown ? new Color(1f, 0.6f, 0.1f, 1f) : new Color(0.85f, 0.4f, 1f, 0.85f);
            GUI.DrawTexture(nitroRect, _pedalNitroTex);
            GUI.Label(nitroRect, "⚡\nNITRO", _btnTextStyle);

            // Gambar Tombol Kemudi Kiri & Kanan
            GUI.color = touchSteer < -0.1f ? new Color(0f, 1f, 0.9f, 1f) : new Color(0.12f, 0.2f, 0.35f, 0.8f);
            GUI.DrawTexture(leftRect, _glassHeaderTex);
            GUI.Label(leftRect, "◀\nKIRI", _btnTextStyle);

            GUI.color = touchSteer > 0.1f ? new Color(0f, 1f, 0.9f, 1f) : new Color(0.12f, 0.2f, 0.35f, 0.8f);
            GUI.DrawTexture(rightRect, _glassHeaderTex);
            GUI.Label(rightRect, "▶\nKANAN", _btnTextStyle);

            GUI.color = prev;
        }

        private static bool CheckPointerInRect(Rect r)
        {
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    if (t.isInProgress)
                    {
                        Vector2 p = t.position.ReadValue();
                        Vector2 gp = new Vector2(p.x, Screen.height - p.y);
                        if (r.Contains(gp)) return true;
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 p = mouse.position.ReadValue();
                Vector2 gp = new Vector2(p.x, Screen.height - p.y);
                if (r.Contains(gp)) return true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var t = Input.GetTouch(i);
                        Vector2 gp = new Vector2(t.position.x, Screen.height - t.position.y);
                        if (r.Contains(gp)) return true;
                    }
                }
                else if (Input.GetMouseButton(0))
                {
                    Vector2 gp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (r.Contains(gp)) return true;
                }
            }
            catch {}
#endif
            return false;
        }

        // ── 4. Controls Hint Bar (PC Keyboard & Gamepad) ───────────────────────
        private void DrawControlsHint()
        {
            int sw = Screen.width;
            int sh = Screen.height;
            Rect hintRect = new Rect(0f, sh - 28f, sw, 24f);

            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.65f);
            GUI.DrawTexture(hintRect, _whiteBarTex);
            GUI.color = prev;

            GUI.Label(hintRect, "🏎️ AUTO-GAS AKTIF  •  [A / D / SWIPE] Belok  •  [SPACE / REM] Drift  •  [SHIFT] Nitro  •  [R] Reset  •  [ESC] Jeda", _hintTextStyle);
        }

        // ── 5. Countdown Overlay (3-2-1-GO) ────────────────────────────────────
        private void DrawCountdown(float timer)
        {
            int sw = Screen.width;
            int sh = Screen.height;

            string text = "";
            Color col = Color.white;

            if (timer > 2.5f) { text = "3"; col = new Color(0f, 0.95f, 1f); }
            else if (timer > 1.5f) { text = "2"; col = new Color(1f, 0.95f, 0.1f); }
            else if (timer > 0.5f) { text = "1"; col = new Color(1f, 0.2f, 0.8f); }
            else { text = "GO!"; col = new Color(0.2f, 1f, 0.4f); }

            _countdownStyle.normal.textColor = col;
            Rect r = new Rect(0f, sh * 0.28f, sw, 140f);
            GUI.Label(r, text, _countdownStyle);
        }

        // ── 6. Pause Modal ─────────────────────────────────────────────────────
        private void DrawPauseModal(RacingGameManager gm)
        {
            int sw = Screen.width;
            int sh = Screen.height;

            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _whiteBarTex);
            GUI.color = prev;

            float mw = Mathf.Clamp(sw * 0.42f, 320f, 480f);
            float mh = Mathf.Clamp(sh * 0.55f, 320f, 440f);
            Rect box = new Rect((sw - mw) * 0.5f, (sh - mh) * 0.5f, mw, mh);

            GUI.color = new Color(0.05f, 0.09f, 0.16f, 0.98f);
            GUI.DrawTexture(box, _glassHeaderTex);
            GUI.color = prev;

            GUI.Label(new Rect(box.x, box.y + 24f, box.width, 42f), "PAUSED", _modalTitleStyle);

            float bw = box.width * 0.75f;
            float bh = 48f;
            float bx = box.x + (box.width - bw) * 0.5f;
            float by = box.y + 90f;

            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0.8f, 0.95f, 0.9f);
            if (GUI.Button(new Rect(bx, by, bw, bh), "▶  LANJUTKAN RACE", _menuBtnStyle))
            {
                gm.TogglePause();
            }

            GUI.backgroundColor = new Color(0.7f, 0.2f, 0.9f, 0.9f);
            if (GUI.Button(new Rect(bx, by + 60f, bw, bh), "↺  ULANGI RACE", _menuBtnStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            GUI.backgroundColor = new Color(0.3f, 0.35f, 0.45f, 0.9f);
            if (GUI.Button(new Rect(bx, by + 120f, bw, bh), "🏠  MENU UTAMA", _menuBtnStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("CyberpunkMainMenu");
            }
            GUI.backgroundColor = bg;
        }

        // ── 7. Finished Modal ──────────────────────────────────────────────────
        private void DrawFinishedModal(RacingGameManager gm)
        {
            int sw = Screen.width;
            int sh = Screen.height;

            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _whiteBarTex);
            GUI.color = prev;

            float mw = Mathf.Clamp(sw * 0.45f, 340f, 520f);
            float mh = Mathf.Clamp(sh * 0.62f, 360f, 480f);
            Rect box = new Rect((sw - mw) * 0.5f, (sh - mh) * 0.5f, mw, mh);

            GUI.color = new Color(0.05f, 0.09f, 0.16f, 0.98f);
            GUI.DrawTexture(box, _glassHeaderTex);
            GUI.color = prev;

            string title = gm.PlayerWon ? "🏆 VICTORY!" : "⚡ WAKTU HABIS";
            Color tCol = gm.PlayerWon ? new Color(0f, 0.95f, 1f) : new Color(1f, 0.25f, 0.3f);
            _modalTitleStyle.normal.textColor = tCol;
            GUI.Label(new Rect(box.x, box.y + 20f, box.width, 42f), title, _modalTitleStyle);

            string body = $"Waktu Lap: {gm.ElapsedTime:00.0} detik\n" +
                          $"Data Nodes Terkumpul: {gm.collectedNodes}/{gm.totalNodes}\n" +
                          $"Rekor Terbaik: {PlayerPrefs.GetFloat("BestRaceTime", 9999f):00.0} detik";
            GUI.Label(new Rect(box.x + 20f, box.y + 75f, box.width - 40f, 75f), body, _modalBodyStyle);

            float bw = box.width * 0.75f;
            float bh = 48f;
            float bx = box.x + (box.width - bw) * 0.5f;
            float by = box.y + 165f;

            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0.85f, 1f, 0.95f);
            if (GUI.Button(new Rect(bx, by, bw, bh), "▶  MAIN LAGI (RESTART)", _menuBtnStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            GUI.backgroundColor = new Color(0.3f, 0.35f, 0.45f, 0.9f);
            if (GUI.Button(new Rect(bx, by + 62f, bw, bh), "🏠  MENU UTAMA", _menuBtnStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("CyberpunkMainMenu");
            }
            GUI.backgroundColor = bg;
        }

        private void GenerateTextures()
        {
            _pedalGasTex = CreateFlatTexture(new Color(0.06f, 0.18f, 0.30f, 0.9f));
            _pedalBrakeTex = CreateFlatTexture(new Color(0.28f, 0.08f, 0.15f, 0.9f));
            _pedalNitroTex = CreateFlatTexture(new Color(0.25f, 0.08f, 0.32f, 0.9f));
            _glassHeaderTex = CreateFlatTexture(new Color(0.04f, 0.07f, 0.14f, 0.92f));
            _whiteBarTex = CreateFlatTexture(Color.white);
        }

        private static Texture2D CreateFlatTexture(Color c)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] cols = { c, c, c, c };
            t.SetPixels(cols);
            t.Apply();
            return t;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            _hudHeaderLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.6f, 0.75f, 0.95f) }
            };

            _hudHeaderVal = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _speedoValStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0f, 0.95f, 1f) }
            };

            _speedoUnitStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.7f, 0.8f, 0.95f) }
            };

            _btnTextStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _hintTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.75f, 0.85f, 0.95f, 0.9f) }
            };

            _countdownStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 72,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _modalTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _modalBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.85f, 0.9f, 0.98f) }
            };

            _menuBtnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _stylesReady = true;
        }
    }
}
