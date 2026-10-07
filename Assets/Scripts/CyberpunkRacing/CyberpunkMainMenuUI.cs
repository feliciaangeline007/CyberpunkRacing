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

        private bool _stylesReady = false;
        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;
        private GUIStyle _btnPrimaryStyle;
        private GUIStyle _btnSecondaryStyle;
        private GUIStyle _bodyStyle;

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
            float hW = Mathf.Min(700f, sw - 32f);
            Rect hRect = new Rect((sw - hW) * 0.5f, 20f, hW, 85f);
            DrawNeonBox(hRect, new Color(0.02f, 0.04f, 0.08f, 0.88f), new Color(0f, 0.95f, 1f, 1f), new Color(1f, 0.1f, 0.85f, 0.8f));

            GUI.Label(new Rect(hRect.x, hRect.y + 10f, hRect.width, 38f), "✦ CYBERPUNK : NEON OVERDRIVE ✦", _titleStyle);
            GUI.Label(new Rect(hRect.x, hRect.y + 48f, hRect.width, 24f), "HIGH-SPEED ARCADE CAR RACING • HIGHWAY NIGHT RUN", _subStyle);

            // Menu Tombol Kiri / Tengah
            float mW = Mathf.Min(380f, sw - 40f);
            float mH = 360f;
            Rect mRect = new Rect(28f, sh * 0.5f - mH * 0.45f, mW, mH);
            DrawNeonBox(mRect, new Color(0.02f, 0.03f, 0.07f, 0.92f), new Color(0f, 0.95f, 1f, 0.95f), new Color(1f, 0.1f, 0.85f, 0.6f));

            float bX = mRect.x + 24f;
            float bW = mRect.width - 48f;
            float curY = mRect.y + 24f;
            float bH = 52f;
            float gap = 12f;

            // Tombol Mulai
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0.95f, 1f);
            if (GUI.Button(new Rect(bX, curY, bW, bH + 4f), "🚀  MULAI BALAPAN", _btnPrimaryStyle))
            {
                SceneManager.LoadScene("CyberpunkHighway");
            }
            GUI.backgroundColor = prevBg;
            curY += bH + gap + 4f;

            // Tombol Kontrol
            if (GUI.Button(new Rect(bX, curY, bW, bH), "🎮  PANDUAN KONTROL", _btnSecondaryStyle))
            {
                _currentTab = Tab.Controls;
            }
            curY += bH + gap;

            // Tombol Pengaturan
            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.9f), "⚙️  PENGATURAN", _btnSecondaryStyle))
            {
                _currentTab = Tab.Settings;
            }
            curY += (bH * 0.9f) + gap;

            // Ganti Warna Mobil
            string colText = $"🎨  WARNA: {PaintNames[_selectedColorIdx]}";
            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.85f), colText, _btnSecondaryStyle))
            {
                _selectedColorIdx = (_selectedColorIdx + 1) % PaintColors.Length;
                ApplyCarPaint(_selectedColorIdx);
                PlayerPrefs.SetInt("SelectedCarColor", _selectedColorIdx);
            }
            curY += (bH * 0.85f) + gap;

            // Tombol Keluar
            if (GUI.Button(new Rect(bX, curY, bW, bH * 0.8f), "🚪  KELUAR", _btnSecondaryStyle))
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

            _stylesReady = true;
        }
    }
}
