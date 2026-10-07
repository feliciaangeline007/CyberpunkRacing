# 🏎️ Cyberpunk Racing: Neon Overdrive

Game balap mobil arkade 3D berkecepatan tinggi bertema **Cyberpunk & Synthwave** untuk platform **Android** dan **PC**. Dibuat menggunakan **Unity 6** dengan **Universal Render Pipeline (URP)**.

Pemain memacu mobil sport melintasi jalan tol layang neon di tengah megahnya gedung pencakar langit metropolis, mengumpulkan **Data Nodes**, memanfaatkan **Nitro Boost**, dan menembus garis akhir sebelum waktu habis!

---

## 📌 Informasi Proyek

| Informasi | Keterangan |
|---|---|
| **Judul Game** | **Cyberpunk Racing: Neon Overdrive** |
| **Engine** | Unity `6000.6.3f1` (Unity 6) |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Platform Target** | Android (Mendukung Android 8.0 hingga **Android 16 / API 36**) & PC Standalone |
| **Orientasi** | Landscape (Sensor Landscape) |
| **Genre** | Arcade 3D Car Racing / Sci-Fi Synthwave |
| **Arsitektur CPU** | ARM64 (`arm64-v8a`) via IL2CPP |

---

## ✨ Fitur Utama

### 1. 🏎️ Fisika Mobil Arkade & Penanganan Halus
- **Kendali Responsif**: Akselerasi mulus, pengereman responsif, dan power drift saat menikung tajam.
- **Nitro Boost Turbo**: Peningkatan kecepatan ekstrem disertai efek partikel semburan api knalpot ganda (*Dual Jet Exhaust*) dan asap ban.
- **Interaksi Benturan Realistis**: Perlambatan saat menyerempet dinding (*Wall Slide Friction*), efek goyangan kamera benturan (*Camera Impact Shake*), dan tombol reset lintasan seketika jika keluar jalur.

### 2. 🌆 Visual Synthwave & Metropolis 3D
- **Gedung Pencakar Langit Asli**: Variasi gedung metropolis dari Synty Studios (*Office Towers, Apartments, Modern Plazas*).
- **Jalan Tol Layang Neon**: Lintasan melengkung dengan marka neon bercahaya (*Cyan & Magenta Emission*), rel pengaman futuristik, dan pilar jembatan kokoh.
- **Pencahayaan Sunset Synthwave**: Matahari terbenam keemasan dengan efek URP Bloom, Tonemapping, dan Soft Shadows.

### 3. 🎁 Sistem Hadiah Harian (7-Day Daily Reward)
- **Login Streak 7 Hari**: Melacak kehadiran harian pemain dengan siklus 7 hari berturut-turut.
- **Ekonomi Cyber Credits**: Saldo mata uang yang dapat dikumpulkan melalui hadiah harian dan hasil balapan.
- **Tingkatan Hadiah**:
  - **Hari 1**: 🪙 `+500 ¢` (Bonus Sambutan Pemula)
  - **Hari 2**: ⚡ `+750 ¢` (Tuning Chip)
  - **Hari 3**: 🔋 `+1,000 ¢` (Neon Booster)
  - **Hari 4**: 💎 `+1,500 ¢` (Quantum Battery)
  - **Hari 5**: 🚀 `+2,200 ¢` (Cyber V8 Engine)
  - **Hari 6**: 🛡️ `+3,500 ¢` (Titanium Chassis)
  - **Hari 7**: 👑 `+5,000 ¢` (**Grand Apex Crown - Hadiah Utama!**)
- **Penyimpanan Permanen**: Disimpan otomatis via `PlayerPrefs` lengkap dengan hitung mundur reset harian (tengah malam).

### 4. 🏠 Showroom Garasi Interaktif (Main Menu)
- **Podium Putar 3D**: Mobil sport berputar di atas podium neon dengan latar panorama kota metropolis.
- **Kustomisasi Warna Bodi**: Pilihan warna cat bodi real-time:
  - 💠 *Cyber Cyan*
  - 🌸 *Neon Magenta*
  - ⚡ *Volt Gold*
  - ⬛ *Carbon Black*
- **Panel Pengaturan Terpadu**: Pengaturan Volume Audio, Presets Kualitas Grafis (Low, Medium, High), dan sakelar Sensor Kemudi HP (*Tilt/Gyroscope*).

### 5. 📱 Kompatibilitas Penuh Android 16 (API 36)
- **Bebas Crash di Android 16 & 15**: Menggunakan arsitektur `UnityPlayerActivity` yang terbukti stabil.
- **`android:appCategory="game"`**: Menjamin pengecualian dari pemaksaan layout bebas pada layar besar/foldable di Android 16.
- **Predictive Back Navigation**: Mendukung `android:enableOnBackInvokedCallback="true"`.
- **Ekstraksi Native Libs**: Mendukung `android:extractNativeLibs="true"` untuk kompatibilitas memory paging ARM64 modern.

---

## 🎮 Panduan Kontrol

### 📱 Layar Sentuh (Mobile / Android)
| Kontrol | Tindakan |
|---|---|
| **Usap Layar (Swipe Gesture)** | Usap area kiri layar ke kiri/kanan untuk kemudi belok yang halus |
| **Tombol [◀] / [▶]** | Alternatif kemudi belok kiri dan kanan |
| **Tombol [⮅ GAS]** | Melaju kencang |
| **Tombol [⮇ REM]** | Mengerem dan mundur; tahan saat belok untuk melakukan *Drift* |
| **Tombol [⚡ NITRO]** | Mengaktifkan dorongan nitro turbo |
| **Tombol [↺ RESET]** | Mereset posisi mobil ke tengah lintasan jika tersangkut |
| **Sensor Tilt (Kemudi Miring)** | Opsi kemudi miring HP menggunakan Gyroscope (dapat diaktifkan di menu Pengaturan) |

### ⌨️ Keyboard & Gamepad (PC)
| Tombol Keyboard | Gamepad | Tindakan |
|---|---|---|
| `W` / `Panah Atas` | Right Trigger (`R2` / `RT`) | Gas Maju |
| `S` / `Panah Bawah` | Left Trigger (`L2` / `LT`) | Rem / Mundur |
| `A` / `D` / `Panah Kiri/Kanan` | Left Stick (`L-Stick`) | Kemudi Belok |
| `Spasi` | Tombol `A` / `Cross` | Handbrake & Power Drift |
| `Left Shift` / `N` | Tombol `X` / `Square` | Nitro Turbo Boost |
| `R` | Tombol `Select` / `Back` | Reset Posisi ke Lintasan |
| `ESC` / `P` | Tombol `Start` | Jeda Balapan (Pause) |

---

## 📁 Struktur Folder Proyek

```text
Assets/
├── Editor/
│   ├── CyberpunkRacingBuilder.cs       # Master Builder otomatis scene & build APK
│   ├── AndroidReleaseVersionCode.cs     # Pengatur otomatis versionCode APK
│   └── SyntyURPMaterialUpgrader.cs      # Upgrader otomatis material Synty ke URP
├── Materials/
│   └── Cyberpunk/                      # Material neon, aspal jalan tol, dan partikel
├── Plugins/
│   └── Android/
│       └── AndroidManifest.xml         # Konfigurasi Manifest Android 16 & Game Category
├── Scenes/
│   ├── CyberpunkMainMenu.unity         # Scene Showroom Menu Utama 3D
│   └── CyberpunkHighway.unity          # Scene Balapan Jalan Tol Metropolis
├── Scripts/
│   └── CyberpunkRacing/
│       ├── CarController.cs            # Logika fisika pergerakan mobil & nitro
│       ├── CarInputManager.cs          # Manajemen input (Touch, Swipe, Gyro, Keyboard)
│       ├── CollectibleNode.cs          # Data Nodes bercahaya yang dapat diambil
│       ├── CyberpunkMainMenuUI.cs      # UI Showroom, Ganti Warna, Pengaturan & Hadiah Harian
│       ├── CyberSoundManager.cs        # Sintesis suara prosedural (chime, beep, victory)
│       ├── DailyRewardManager.cs       # Sistem login 7 hari & dompet Cyber Credits
│       ├── FinishLineTrigger.cs        # Deteksi garis finish jalan tol
│       ├── RacingCamera.cs             # Kamera balap follow dinamis dengan efek getar
│       ├── RacingGameManager.cs        # Alur balapan (Countdown, waktu, menang/kalah)
│       └── RacingHUD.cs                # HUD layar sentuh, speedometer, dan pedal on-screen
├── Settings/                           # Aset URP Pipeline (Mobile_RPAsset & PC_RPAsset)
└── SyntyStudios/                       # Aset 3D Polygon City (Gedung metropolis & mobil)
```

---

## 🚀 Cara Menjalankan & Build

### 1. Membuka di Unity Editor
1. Buka **Unity Hub**, tambahkan folder proyek ini.
2. Gunakan versi **Unity 6** (`6000.6.3f1` atau patch Unity 6 yang setara).
3. Buka scene `Assets/Scenes/CyberpunkMainMenu.unity`.
4. Tekan tombol **Play** di Unity Editor untuk langsung mencoba balapan.

### 2. Generate Ulang Scene Otomatis
Jika ingin membuat ulang lintasan jalan tol dan showroom dari awal:
- Buka menu atas Unity: **Tools > Cyberpunk Racing > ⚡ Generate Semua Scene Cyberpunk**.

### 3. Build Android APK
1. Pastikan platform di Build Settings telah diset ke **Android** (*Switch Platform*).
2. Jalankan build melalui menu: **Tools > Cyberpunk Racing > 📦 Build Android APK**.
3. Hasil build APK akan otomatis tersimpan di:
   - `Builds/Android/CyberpunkRacing.apk`
   - Salinan otomatis dikirimkan ke folder `~/Downloads/CyberpunkRacing.apk` untuk kemudahan transfer ke ponsel.
