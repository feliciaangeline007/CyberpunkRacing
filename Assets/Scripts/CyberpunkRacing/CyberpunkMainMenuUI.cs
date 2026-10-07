using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberpunkRacing
{
    /// <summary>
    /// Layar Utama Cyberpunk Car Racing (Neon Overdrive):
    /// - Showroom Mobil 3D di atas podium neon yang berputar
    /// - Pilihan Warna Mobil (Cyan, Magenta, Gold, Stealth)
    /// - Panduan Kontrol Lengkap (PC & Mobile Touch / Gyro)
    /// - Pengaturan Audio, Grafis, & Sensor Tilt
    /// </summary>
    public class CyberpunkMainMenuUI : MonoBehaviour
    {
        public enum Tab
        {
            Main,
            DailyReward,
            Controls,
            Settings
        }

        [Header("Pameran Mobil 3D")]
        public Transform showcaseCar;
        public Renderer carBodyRenderer;

        private Tab _currentTab = Tab.Main;
        private float _masterVolume = 1f;
        private int _qualityLevel = 2;
        private bool _tiltSteeringEnabled = false;
        private string _rewardClaimMessage = "";

        private bool _stylesReady = false;
        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;
        private GUIStyle _btnPrimaryStyle;
        private GUIStyle _btnSecondaryStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _cardDayStyle;
        private GUIStyle _cardRewardStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardStatusStyle;
        private GUIStyle _hudBadgeStyle;

        private static readonly Color[] PaintColors = new Color[]
        {
            new Color(0f, 0.9f, 1f),       // Cyber Cyan
            new Color(1f, 0.1f, 0.8f),     // Neon Magenta
            new Color(1f, 0.8f, 0.1f),     // Volt Gold
            new Color(0.12f, 0.14f, 0.18f) // Stealth Carbon
        };

        private static readonly string[] PaintNames = new string[]
        {
            "CYBER CYAN", "NEON MAGENTA", "VOLT GOLD", "CARBON BLACK"
        };

        private int _selectedColorIdx = 0;

        private void Start()
        {
            Time.timeScale = 1f;
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _qualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            _tiltSteeringEnabled = PlayerPrefs.GetInt("UseTiltSteering", Application.isMobilePlatform ? 1 : 0) == 1;
            AudioListener.volume = _masterVolume;

            _selectedColorIdx = PlayerPrefs.GetInt("SelectedCarColor", 0);
            ApplyCarPaint(_selectedColorIdx);
        }

        private void Update()
        {
            if (showcaseCar != null)
            {
                showcaseCar.Rotate(Vector3.up * 20f * Time.deltaTime, Space.World);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            switch (_currentTab)
            {
                case Tab.Main:
                    DrawMainTab();
                    break;
                case Tab.DailyReward:
                    DrawDailyRewardTab();
                    break;
                case Tab.Controls:
                    DrawControlsTab();
                    break;
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
            }
        }

        private void DrawMainTab()
        {
            int sw = Screen.width;
            int sh = Screen.height;

            // Header Neon Atas
            float hW = Mathf.Min(740f, sw - 32f);
            Rect hRect = new Rect((sw - hW) * 0.5f, 16f, hW, 88f);
            DrawNeonBox(hRect, new Color(0.02f, 0.04f, 0.08f, 0.88f), new Color(0f, 0.95f, 1f, 1f), new Color(1f, 0.1f, 0.85f, 0.8f));

            GUI.Label(new Rect(hRect.x, hRect.y + 8f, hRect.width, 32f), "✦ CYBERPUNK : NEON OVERDRIVE ✦", _titleStyle);
            GUI.Label(new Rect(hRect.x, hRect.y + 40f, hRect.width, 20f), "HIGH-SPEED ARCADE CAR RACING • HIGHWAY NIGHT RUN", _subStyle);

            int credits = DailyRewardManager.GetCredits();
            bool canClaim = DailyRewardManager.CanClaimToday();
            string claimAlert = canClaim ? "  •  🎁 [HADIAH HARIAN SIAP KLAIM!]" : "";
            GUI.Label(new Rect(hRect.x, hRect.y + 62f, hRect.width, 20f), $"💳 CYBER CREDITS: {credits:N0} ¢{claimAlert}", _subStyle);

            // Menu Tombol Kiri / Tengah
            float mW = Mathf.Min(380f, sw - 40f);
            float mH = 415f;
            Rect mRect = new Rect(28f, sh * 0.5f - mH * 0.42f, mW, mH);
            DrawNeonBox(mRect, new Color(0.02f, 0.03f, 0.07f, 0.92f), new Color(0f, 0.95f, 1f, 0.95f), new Color(1f, 0.1f, 0.85f, 0.6f));

            float bX = mRect.x + 24f;
            float bW = mRect.width - 48f;
            float curY = mRect.y + 20f;
            float bH = 48f;
            float gap = 10f;

            // Tombol Mulai
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0.95f, 1f);
            if (GUI.Button(new Rect(bX, curY, bW, bH + 2f), "🚀  MULAI BALAPAN", _btnPrimaryStyle))
            {
                RacingGameManager.LoadSceneSafe("CyberpunkHighway");
            }
            GUI.backgroundColor = prevBg;
            curY += bH + gap + 2f;

            // Tombol Hadiah Harian
            string dailyLabel = canClaim ? "🎁  HADIAH HARIAN  [★ KLAIM!]" : "🎁  HADIAH HARIAN";
            if (canClaim) GUI.backgroundColor = new Color(1f, 0.82f, 0.15f);
            if (GUI.Button(new Rect(bX, curY, bW, bH), dailyLabel, _btnSecondaryStyle))
            {
                _rewardClaimMessage = "";
                _currentTab = Tab.DailyReward;
            }
            GUI.backgroundColor = prevBg;
            curY += bH + gap;

            // Tombol Kontrol
            if (GUI.Button(new Rect(bX, curY, bW, bH), "🎮  PANDUAN KONTROL", _btnSecondaryStyle))
            {
                _currentTab = Tab.Controls;
            }
            curY += bH + gap;

            // Tombol Pengaturan
            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.92f), "⚙️  PENGATURAN", _btnSecondaryStyle))
            {
                _currentTab = Tab.Settings;
            }
            curY += (bH * 0.92f) + gap;

            // Ganti Warna Mobil
            string colText = $"🎨  WARNA: {PaintNames[_selectedColorIdx]}";
            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.88f), colText, _btnSecondaryStyle))
            {
                _selectedColorIdx = (_selectedColorIdx + 1) % PaintColors.Length;
                ApplyCarPaint(_selectedColorIdx);
                PlayerPrefs.SetInt("SelectedCarColor", _selectedColorIdx);
            }
            curY += (bH * 0.88f) + gap;

            // Tombol Keluar
            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.82f), "🚪  KELUAR", _btnSecondaryStyle))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void DrawControlsTab()
        {
            float mW = Mathf.Min(660f, Screen.width - 32f);
            float mH = Mathf.Min(500f, Screen.height - 40f);
            Rect panel = new Rect((Screen.width - mW) * 0.5f, (Screen.height - mH) * 0.5f, mW, mH);
            DrawNeonBox(panel, new Color(0.02f, 0.03f, 0.07f, 0.96f), new Color(0f, 0.95f, 1f, 1f), new Color(1f, 0.1f, 0.85f, 0.8f));

            GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 36f), "🎮  PANDUAN KONTROL CYBERPUNK", _titleStyle);

            string guideText =
                "📱  KONTROL LAYAR SENTUH (MOBILE / ANDROID):\n" +
                "  • USAP LAYAR (SWIPE GESTURE) : Usap area kiri layar ke kiri/kanan untuk kemudi halus (Default jika tanpa Gyro!)\n" +
                "  • Kiri Bawah [◀ / ▶] : Tombol alternatif kemudi belok kiri dan kanan\n" +
                "  • Kiri Atas [↺ RESET] : Reset posisi mobil ke tengah lintasan jika keluar jalur\n" +
                "  • Kanan Bawah [⮅ GAS] : Melaju kencang\n" +
                "  • Kanan Bawah [⚡ NITRO] : Boost kecepatan nitro kilatan api\n" +
                "  • Kiri Bawah [⮇ REM] : Rem dan mundur; tahan saat belok untuk DRIFT\n" +
                "  • Sensor TILT (Gyroscope) : Bisa diaktifkan di menu Pengaturan\n\n" +
                "⌨️  KONTROL KEYBOARD & GAMEPAD (PC):\n" +
                "  • W / Panah Atas / R-Trigger : Gas maju\n" +
                "  • S / Panah Bawah / L-Trigger : Rem / Mundur\n" +
                "  • A / D atau Panah / L-Stick : Kemudi belok\n" +
                "  • SPASI / Tombol A : Handbrake & Power Drift\n" +
                "  • SHIFT / N / Tombol X : Nitro Boost turbo\n" +
                "  • R / Tombol Select : Reset lintasan seketika\n\n" +
                "🎯  OBJEKTIF BALAPAN:\n" +
                "Laju melintasi jalan tol neon Metropolis, ambil 25 Data Nodes, dan tembus Garis Finish sebelum waktu habis!";

            GUI.Label(new Rect(panel.x + 24f, panel.y + 64f, panel.width - 48f, panel.height - 130f), guideText, _bodyStyle);

            if (GUI.Button(new Rect((Screen.width - 240f) * 0.5f, panel.y + panel.height - 52f, 240f, 40f), "⬅  KEMBALI KE MENU", _btnSecondaryStyle))
            {
                _currentTab = Tab.Main;
            }
        }

        private void DrawSettingsTab()
        {
            float mW = Mathf.Min(560f, Screen.width - 32f);
            float mH = Mathf.Min(460f, Screen.height - 40f);
            Rect panel = new Rect((Screen.width - mW) * 0.5f, (Screen.height - mH) * 0.5f, mW, mH);
            DrawNeonBox(panel, new Color(0.02f, 0.03f, 0.07f, 0.96f), new Color(0f, 0.95f, 1f, 1f), new Color(1f, 0.1f, 0.85f, 0.8f));

            GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 36f), "⚙️  PENGATURAN BALAPAN", _titleStyle);

            float rX = panel.x + 36f;
            float rW = panel.width - 72f;
            float curY = panel.y + 66f;

            // 1. Volume Master
            GUI.Label(new Rect(rX, curY, rW, 24f), $"Volume Suara: {Mathf.RoundToInt(_masterVolume * 100)}%", _subStyle);
            curY += 28f;
            float newVol = GUI.HorizontalSlider(new Rect(rX, curY, rW, 24f), _masterVolume, 0f, 1f);
            if (Mathf.Abs(newVol - _masterVolume) > 0.01f)
            {
                _masterVolume = newVol;
                AudioListener.volume = _masterVolume;
                PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
            }
            curY += 36f;

            // 2. Kualitas Grafis
            GUI.Label(new Rect(rX, curY, rW, 24f), "Kualitas Grafis (Visual Preset):", _subStyle);
            curY += 28f;
            string[] qNames = { "Ringan (Low)", "Sedang (Medium)", "Tinggi (High)" };
            float qBtnW = (rW - 16f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = _qualityLevel == i ? new Color(0f, 0.95f, 1f) : new Color(0.12f, 0.18f, 0.28f);
                if (GUI.Button(new Rect(rX + i * (qBtnW + 8f), curY, qBtnW, 36f), qNames[i], _btnSecondaryStyle))
                {
                    _qualityLevel = i;
                    QualitySettings.SetQualityLevel(i, true);
                    PlayerPrefs.SetInt("QualityLevel", _qualityLevel);
                }
                GUI.backgroundColor = prev;
            }
            curY += 48f;

            // 3. Sensor Tilt Steering (Gyroscope)
            string tiltStatus = _tiltSteeringEnabled ? "AKTIF [ON]" : "NONAKTIF [OFF]";
            string tiltLabel = $"Kemudi Sensor Miring HP: {tiltStatus}";
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = _tiltSteeringEnabled ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.25f, 0.3f, 0.4f);
            if (GUI.Button(new Rect(rX, curY, rW, 40f), tiltLabel, _btnSecondaryStyle))
            {
                _tiltSteeringEnabled = !_tiltSteeringEnabled;
                PlayerPrefs.SetInt("UseTiltSteering", _tiltSteeringEnabled ? 1 : 0);
            }
            GUI.backgroundColor = prevBg;

            if (GUI.Button(new Rect((Screen.width - 240f) * 0.5f, panel.y + panel.height - 52f, 240f, 40f), "⬅  SIMPAN & KEMBALI", _btnPrimaryStyle))
            {
                PlayerPrefs.Save();
                _currentTab = Tab.Main;
            }
        }

        private void ApplyCarPaint(int idx)
        {
            if (carBodyRenderer != null && idx >= 0 && idx < PaintColors.Length)
            {
                carBodyRenderer.material.color = PaintColors[idx];
                if (carBodyRenderer.material.HasProperty("_BaseColor"))
                {
                    carBodyRenderer.material.SetColor("_BaseColor", PaintColors[idx]);
                }
            }
        }

        private static void DrawNeonBox(Rect rect, Color bg, Color topBorder, Color bottomBorder)
        {
            Color prev = GUI.color;
            GUI.color = bg;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = topBorder;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), Texture2D.whiteTexture);
            GUI.color = bottomBorder;
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void DrawDailyRewardTab()
        {
            float mW = Mathf.Min(780f, Screen.width - 24f);
            float mH = Mathf.Min(560f, Screen.height - 30f);
            Rect panel = new Rect((Screen.width - mW) * 0.5f, (Screen.height - mH) * 0.5f, mW, mH);
            DrawNeonBox(panel, new Color(0.02f, 0.03f, 0.07f, 0.98f), new Color(1f, 0.8f, 0.1f, 1f), new Color(0f, 0.95f, 1f, 0.8f));

            GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 34f), "🎁  HADIAH REWARD HARIAN (7-DAY STREAK)", _titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 46f, panel.width, 20f), "Login setiap hari untuk klaim Cyber Credits berlimpah dan raih Grand Prize!", _subStyle);

            bool canClaim = DailyRewardManager.CanClaimToday();
            int currentDay = DailyRewardManager.GetCurrentClaimDay();
            var rewards = DailyRewardManager.SevenDayRewards;

            // Saldo Saat Ini
            int credits = DailyRewardManager.GetCredits();
            GUI.Label(new Rect(panel.x + 24f, panel.y + 68f, panel.width - 48f, 24f), $"💳 Saldo Cyber Credits Kamu: {credits:N0} ¢", _hudBadgeStyle);

            // Kotak Kartu 7 Hari (Baris 1: Hari 1-4, Baris 2: Hari 5-7)
            float startY = panel.y + 98f;
            float padX = 24f;
            float availW = panel.width - (padX * 2);

            // Baris 1: 4 kartu
            float cardGap = 10f;
            float cardW4 = (availW - (cardGap * 3)) / 4f;
            float cardH = 92f;

            for (int i = 0; i < 4; i++)
            {
                int day = i + 1;
                var item = rewards[i];
                Rect cardRect = new Rect(panel.x + padX + i * (cardW4 + cardGap), startY, cardW4, cardH);
                DrawRewardCard(cardRect, item, day, currentDay, canClaim);
            }

            // Baris 2: 3 kartu (Hari 5, 6, 7)
            float cardW3 = (availW - (cardGap * 2)) / 3f;
            float startY2 = startY + cardH + 10f;
            for (int i = 4; i < 7; i++)
            {
                int day = i + 1;
                var item = rewards[i];
                Rect cardRect = new Rect(panel.x + padX + (i - 4) * (cardW3 + cardGap), startY2, cardW3, cardH);
                DrawRewardCard(cardRect, item, day, currentDay, canClaim);
            }

            // Pesan Notifikasi Klaim
            float actionY = startY2 + cardH + 12f;
            if (!string.IsNullOrEmpty(_rewardClaimMessage))
            {
                GUI.Label(new Rect(panel.x + 20f, actionY, panel.width - 40f, 24f), _rewardClaimMessage, _hudBadgeStyle);
                actionY += 28f;
            }

            // Tombol Aksi Klaim
            float btnW = Mathf.Min(420f, panel.width - 60f);
            float btnX = (Screen.width - btnW) * 0.5f;

            if (canClaim)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.85f, 0.1f);
                var todayItem = rewards[currentDay - 1];
                string claimLabel = $"✨ KLAIM HADIAH HARI KE-{currentDay} (+{todayItem.credits:N0} ¢) ✨";
                if (GUI.Button(new Rect(btnX, actionY, btnW, 44f), claimLabel, _btnPrimaryStyle))
                {
                    if (DailyRewardManager.ClaimToday(out var claimed))
                    {
                        CyberSoundManager.Instance?.PlayVictory();
                        _rewardClaimMessage = $"✦ BERHASIL DIKLAIM: +{claimed.credits:N0} ¢ ({claimed.title})!";
                    }
                }
                GUI.backgroundColor = prev;
            }
            else
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.18f, 0.22f, 0.3f);
                string waitLabel = $"⏳ SUDAH DIKLAIM HARI INI • RESET: {DailyRewardManager.GetFormattedTimeUntilNextReset()}";
                GUI.Button(new Rect(btnX, actionY, btnW, 44f), waitLabel, _btnSecondaryStyle);
                GUI.backgroundColor = prev;
            }

            // Baris Tombol Bawah
            float botY = panel.y + panel.height - 48f;
            float backW = 240f;
            float backX = panel.x + (panel.width - backW) * 0.5f;
            if (GUI.Button(new Rect(backX, botY, backW, 38f), "⬅  KEMBALI KE MENU", _btnSecondaryStyle))
            {
                _rewardClaimMessage = "";
                _currentTab = Tab.Main;
            }
        }

        private void DrawRewardCard(Rect rect, DailyRewardItem item, int day, int currentDay, bool canClaim)
        {
            bool isPast = day < currentDay || (day == currentDay && !canClaim);
            bool isToday = (day == currentDay && canClaim);

            Color bg;
            Color border;
            string status;

            if (isToday)
            {
                bg = new Color(0.12f, 0.25f, 0.35f, 0.95f);
                border = new Color(1f, 0.85f, 0.1f, 1f);
                status = "★ SIAP KLAIM!";
            }
            else if (isPast)
            {
                bg = new Color(0.04f, 0.12f, 0.10f, 0.9f);
                border = new Color(0.2f, 0.8f, 0.4f, 0.8f);
                status = "✓ SUDAH DIAMBIL";
            }
            else
            {
                bg = new Color(0.03f, 0.05f, 0.08f, 0.85f);
                border = new Color(0.2f, 0.3f, 0.4f, 0.5f);
                status = "🔒 TERKUNCI";
            }

            DrawNeonBox(rect, bg, border, border);

            string dayTitle = day == 7 ? $"👑 HARI 7 (GRAND PRIZE)" : $"HARI {day}";
            GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 18f), dayTitle, _cardDayStyle);
            GUI.Label(new Rect(rect.x + 4f, rect.y + 24f, rect.width - 8f, 24f), $"{item.icon} +{item.credits:N0} ¢", _cardRewardStyle);
            GUI.Label(new Rect(rect.x + 4f, rect.y + 48f, rect.width - 8f, 18f), item.title, _cardTitleStyle);

            Color prev = GUI.color;
            if (isToday) GUI.color = new Color(1f, 0.9f, 0.2f);
            else if (isPast) GUI.color = new Color(0.3f, 0.9f, 0.4f);
            else GUI.color = new Color(0.6f, 0.65f, 0.75f);
            GUI.Label(new Rect(rect.x + 4f, rect.y + 68f, rect.width - 8f, 18f), status, _cardStatusStyle);
            GUI.color = prev;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            int sh = Screen.height;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 22, 22, 34),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0f, 0.95f, 1f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 44, 12, 16),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.2f, 0.85f) }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 48, 13, 16),
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _btnPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(sh / 42, 15, 20),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _btnSecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(sh / 48, 13, 17),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _cardDayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 56, 11, 14),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0f, 0.9f, 1f) }
            };

            _cardRewardStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 46, 13, 17),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.1f) }
            };

            _cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 58, 10, 13),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                normal = { textColor = Color.white }
            };

            _cardStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 58, 10, 13),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _hudBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(sh / 46, 13, 17),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            };

            _stylesReady = true;
        }
    }
}
