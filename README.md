# Coin Convoy: Escape Route

Game Android 3D **collect-and-escape** untuk tugas UTS mata kuliah **Android Game Development**. Dibuat dengan **Unity**, dimainkan di smartphone Android.

Pemain mengendarai mobil kurir, mengumpulkan 15 coin energi, bertahan dari kejaran musuh NavMesh, lalu mencapai gerbang keluar sebelum waktu habis.

| | |
|---|---|
| Mata kuliah | Android Game Development (UTS) |
| Deadline | Kamis, 8 Oktober 2026, 16.00-16.30 |
| Engine | Unity `6000.5.9f1` (Unity 6) |
| Render pipeline | **URP** (Universal Render Pipeline) |
| Platform | Android, orientasi landscape |
| Genre | 3D collect-and-escape |
| Penilaian | Gameplay 50% / Kreativitas 25% / Presentasi 25% |

---

## 1. Quick Start

1. Buka proyek lewat **Unity Hub** dengan Unity `6000.5.9f1`.
2. Tunggu import selesai, pastikan **Console tanpa error merah**.
3. (Opsional, sekali jalan) klik menu **Tools > Coin Convoy > Perbaiki Scenery Latar (Scene Aktif)** untuk memperbaiki skybox, fog, cahaya, gunung jauh, dinding tak terlihat, batu, Wind Zone, dan audio ambience.
4. Buka scene `Assets/Scenes/MainMenu.unity`.
5. Tekan **Play** → **PLAY** → main.

Generator scene (hanya bila ingin membuat ulang demo dari nol):

```text
Tools > Coin Convoy > Create Playable Demo
```

> Peringatan: generator **mengganti** `Game.unity` dan `MainMenu.unity`. Jangan dijalankan bila scene sudah Anda edit manual.

---

## 2. Scene dan Build Settings

| Index | Scene | Isi |
|---|---|---|
| 0 | `Assets/Scenes/MainMenu.unity` | Judul, BEST score, tombol PLAY dan QUIT |
| 1 | `Assets/Scenes/Game.unity` | Arena, player, musuh, coin, gerbang, HUD, GameManager |

Scene lain (`Level_1_PinggirHutan`, `Level_2_JurangAkar`, `Level_3_PuncakPohon`, `SampleScene`) adalah level lama dari tugas 3D Level Design dan **tidak** masuk Build Settings.

**Catatan:** proyek memakai **Force Binary** serialization (`ProjectSettings > EditorSettings`). Untuk diff git yang terbaca, ubah ke `Force Text` lalu save ulang scene (opsional).

---

## 3. Struktur Folder

```text
Assets/
├── Editor/                    # Tool editor (generator + scenery)
│   ├── CoinConvoyBuilder.cs
│   └── SceneryEnhancer.cs
├── Scenes/
│   ├── MainMenu.unity
│   └── Game.unity
├── Scripts/CoinConvoy/        # Seluruh script gameplay
├── Materials/
│   ├── CoinConvoy/            # Material scenery (dibuat tool)
│   └── EnchantedForest/       # Material level lama
├── Audio/                     # CoinPickup, ForestAmbience, VictoryFanfare, dll.
├── Sprite/                    # Ikon coin
├── Settings/                  # Aset URP (Mobile_RPAsset, renderer)
├── TutorialInfo/              # Template bawaan Unity (aman diabaikan)
└── Scripts/LevelDesign/       # Script level lama, tidak dipakai game ini
```

---

## 4. Kontrol

### Android (layar sentuh)

| Tombol | Fungsi |
|---|---|
| GAS | Maju |
| BRAKE | Rem, lalu mundur |
| LEFT / RIGHT | Belok |
| PAUSE | Jeda permainan |

Empat tombol utama berada di bawah kiri-kanan, ukuran minimal `140 x 110`, jarak tepi minimal `50 px`. Dua tombol bisa ditekan bersamaan (gas + belok).

### Editor (keyboard)

| Tombol | Fungsi |
|---|---|
| `W` / `S` atau panah atas-bawah | Maju / mundur |
| `A` / `D` atau panah kiri-kanan | Belok |
| `Space` | Rem |

Input memakai **Both** (`Active Input Handling`) sehingga Input System baru dan Legacy sama-sama bekerja.

---

## 5. Aturan Permainan

| Aturan | Nilai |
|---|---|
| Jumlah coin | 15 |
| Nilai per coin | 10 poin |
| Target coin untuk buka gerbang | 15 (semua coin) |
| Nyawa awal | 3 |
| Timer | 180 detik |
| Bonus menang | sisa detik x 2 |
| Jeda kebal setelah kena | 2 detik |
| High score | `PlayerPrefs` (`HighScore`), hanya disimpan saat menang |

**Menang:** semua coin terkumpul → gerbang berubah merah ke hijau → mobil masuk trigger gerbang.
**Kalah:** nyawa 0 ATAU waktu 0.

### Kurva kesulitan

- Kecepatan musuh pengejar: `3.5 + 0.25 x jumlah coin`, maksimum `7.0`.
- Musuh bertambah: setiap 5 coin satu musuh baru, maksimum 4 (`ConvoySpawner`).
- Level Buronan naik tiap 3 coin, maksimum level 6.
- 30 detik terakhir: musik berpindah ke track tegang.

---

## 6. Referensi Script Lengkap

| Script | Ditempel ke | Field Inspector utama | Komponen / Tag wajib |
|---|---|---|---|
| `CarController.cs` | `PlayerCar` | Acceleration 16, Max Speed 12, Steering 90, Brake 22, Visual | `Rigidbody` (Mass 1100, Freeze Rot X/Z, Interpolate), Collider, tag **Player** |
| `TouchButton.cs` | 4 tombol UI | Action (Gas/Brake/Left/Right), Car | `Button` + `Image`, Canvas punya `GraphicRaycaster` |
| `FollowCamera.cs` | `Main Camera` | Target = PlayerCar, Offset `(0,7,-10)`, Smooth 8 | Camera |
| `GameManager.cs` | `GameManager` | Lives 3, Points 10, Target 15, Use Timer ON, Time 180, Escape Gate, semua field UI | Singleton `GameManager.Instance` |
| `CoinPickup.cs` | setiap `Coin` | Pickup Sound, Pickup Effect | `Collider Is Trigger = ON` |
| `EnemyChaser.cs` | `EnemyCar` | Target kosong, Base Speed 3.5, Max 7, Per Coin 0.25 | `NavMeshAgent`, Collider, tag **Enemy** |
| `PatrolEnemy.cs` | `EnemyPatrol` | Waypoints, Patrol 2.5, Detect 10, Lose 16, Chase 5.5 | `NavMeshAgent`, tag **Enemy** |
| `DamageFeedback.cs` | `PlayerCar` | Invulnerability 2, Flash 0.12, Hit Sound | Merespons tag **Enemy** |
| `EscapeGate.cs` | gerbang keluar | Closed/Open Color, Gate Renderer | `Collider Is Trigger = ON` |
| `MovingObstacle.cs` | obstacle bergerak | Point A, Point B, Speed 3 | Collider; tag Enemy bila menyakiti |
| `PowerUp.cs` | objek power-up | Type (Nitro/Shield/Magnet/ExtraLife), Nitro 1.7 / 5 dtk | `Collider Is Trigger = ON` |
| `PathLights.cs` | `PathLights` | Array lentera berurutan, Interval 0.25 | lentera child |
| `ConvoySpawner.cs` | `ConvoySpawner` | Enemy Prefab, Spawn Points, Per 5 coin, Max 4 | prefab enemy |
| `BountyConvoyEvent.cs` | `GameManager` (opsional) | Alert Duration 8 detik | sirene/beacon musuh |
| `BountyMeter.cs` | `BountyMeter` | Label TMP, Per 3 coin, Max 6, Danger Arrow | TMP_Text |
| `ArrowIndicator.cs` | anak Main Camera | Target = gerbang | Renderer panah |
| `ScreenFlash.cs` | Image full-screen | Duration 0.35, Max Alpha 0.45 | `Image`, Raycast Target OFF |
| `ScorePop.cs` | `ScoreText` | Pop Scale 1.35 | TMP_Text |
| `MusicManager.cs` | `MusicManager` | Normal Track, Danger Track | `AudioSource` |
| `MobileOptimizer.cs` | `MobileOptimizer` | Target Frame Rate 30 | - |
| `MainMenu.cs` | `MainMenu` | High Score Text | dipasang ke OnClick PLAY/QUIT |

Aturan kode: **tanpa `GameObject.Find` di `Update` runtime**. Target musuh dicache saat startup; indikator radar hanya mencoba ulang pencarian entitas bila referensinya belum tersedia.

Polish runtime:

- Gas dan belok merespons lebih halus; grip lateral membantu mobil tidak terasa terlalu licin.
- Kamera menyesuaikan look-ahead dengan kecepatan, memberi FOV kick saat nitro, dan shake singkat saat terkena musuh.
- Coin memakai emission dan burst pool ringan; skid dust dibatasi 64 partikel per sistem.
- `MobileOptimizer` membatasi target ke 30 FPS, jarak shadow 40 m, dan mematikan realtime light lokal agar efek glow memakai emission.
- `ConvoySpawner` menghitung chaser dan patroli yang sedang aktif bersama-sama; batas 4 berlaku untuk jumlah total musuh aktif.
- Opsional: tambahkan `BountyConvoyEvent` ke `GameManager` di scene `Game`. Saat bounty penuh dan gerbang terbuka, sirene/beacon memberi peringatan 8 detik tanpa menambah musuh atau kecepatan.

---

## 7. Scenery dan Lingkungan

Jalankan **Tools > Coin Convoy > Perbaiki Scenery Latar (Scene Aktif)** untuk menerapkan seluruhnya sekaligus. Tool idempoten (aman dijalankan berulang) dan melakukan:

1. **Skybox senja** — material `Skybox/Procedural` di `Assets/Materials/CoinConvoy/CoinConvoy_Sky.mat`.
2. **Ambient warna** — Trilight (langit biru, horizon hangat, tanah gelap).
3. **Fog** — `Exponential Squared`, density `0.012`, warna kabut hangat, menyamarkan tepi map.
4. **Directional Light** — rotasi `(48, -32, 0)`, oranye lembut `1.0/0.87/0.74`, intensitas `1.15`, Soft Shadows `0.85`.
5. **Gunung jauh** — 14 kubah rendah mengelilingi map di luar arena (tanpa collider, statis untuk batching).
6. **Dinding tak terlihat** — 4 box collider setinggi 30 m mengelilingi arena, `Renderer` dimatikan sehingga tidak terlihat tapi menghalangi mobil dan musuh.
7. **Batu ornamen** — 16 batu di luar jalur, seed tetap sehingga hasil selalu sama.
8. **Wind Zone** — `windMain 0.7`, `windTurbulence 0.5` untuk menggerakkan rumput/pohon.
9. **Audio ambience** — `ForestAmbience.wav`, loop, volume `0.35`, 2D.
10. **Bake ulang NavMesh** — semua `NavMeshSurface` di-bake ulang setelah dinding ditambahkan.

Menu kedua: **Tools > Coin Convoy > Perbaiki Scenery Semua Scene Build** menerapkan hal yang sama ke `MainMenu` dan `Game` sekaligus.

### Pengaturan Terrain manual (untuk level ber-Terrain)

| Parameter | Nilai aman HP menengah |
|---|---|
| Terrain Paint | 4 layer: Tanah, Rumput, Jalan, Batu (Opacity 0.4) |
| Brush kekuatan sculpt | 0.15 (jangan terlalu curam di jalur) |
| Detail Density | 0.4 - 0.6 |
| Detail Distance | 60 - 80 m |
| Tree Distance | 150 m |
| Billboard Start | 50 m |
| Pixel Error | 5 - 15 |

---

## 8. Optimasi Mobile

| Setting | Nilai |
|---|---|
| Target FPS | 30 (`MobileOptimizer`) |
| VSync | Off |
| Quality level Android | `Mobile` |
| Shadow Distance | 40 m |
| Shadow Cascades | 1 |
| Anti Aliasing | Disabled / 2x |
| Musuh aktif | maksimal 4 |
| Particle per sistem | maksimal 100, total 10 sistem |
| Light realtime | 1 Directional; Point light tanpa shadow |
| Draw Calls | ideal < 100 |
| Texture | Max Size 1024, kompresi otomatis |
| Batching | objek statis ditandai `Batching Static`, GPU Instancing pada material berulang |

### Membaca Profiler

`Window > Analysis > Profiler` → rekam 30 detik aksi penuh, cek 4 metrik:

| Metrik | Sehat | Tindakan bila merah |
|---|---|---|
| CPU Main Thread | < 33 ms | cari script di bawahnya |
| GPU | < 33 ms | turunkan shadow/AA |
| Batches | < 100 | batching static, samakan material |
| GC Alloc | 0 B per frame | hilangkan `new`/string di Update |

---

## 9. Build Android

### Player Settings

| Parameter | Nilai |
|---|---|
| Product Name | `Lumora` |
| Package Name | `com.feliciaangeline.lumora` |
| Orientation | Landscape Left (+ Right), portrait OFF |
| Render Outside Safe Area | ON |
| Minimum API Level | Android 8.0 (API 26) |
| Scripting Backend | **IL2CPP** |
| Target Architectures | ARM64 |
| Active Input Handling | Both |

### Langkah

1. `File > Build Settings` → Android → **Switch Platform**.
2. Pastikan `MainMenu` index 0, `Game` index 1.
3. HP: aktifkan **Developer Options > USB Debugging**, sambung kabel, izinkan.
4. `File > Build and Run` → pilih lokasi `Builds/CoinConvoy.apk`.
5. Tunggu Gradle (pertama kali 5-15 menit), game terbuka otomatis di HP.

### Pembaruan dari GitHub Releases

Updater otomatis memeriksa rilis stabil terbaru dari `feliciaangeline007/Lumora-3D`
ketika menu utama dibuka. Updater hanya membaca GitHub Release publik dan mencari
aset APK (`.apk`) pada rilis terbaru.

Untuk menerbitkan pembaruan:

1. Naikkan `Player Settings > Other Settings > Bundle Version`, misalnya ke `0.1.1`.
   Gunakan format angka `MAJOR.MINOR.PATCH`; versi ini harus sama dengan tag rilis
   GitHub (`v0.1.1` atau `0.1.1`).
2. Buat APK Android baru. Saat build Android, `AndroidReleaseVersionCode` membuat
   `versionCode` dari versi tersebut agar APK baru dapat menggantikan versi lama.
3. Buat GitHub Release stabil dengan tag versi yang sama dan unggah satu aset APK,
   misalnya `Lumora-0.1.1.apk`. Jangan unggah APK yang belum ditandatangani.
4. Pengguna akan melihat versi baru di menu utama, memilih **UNDUH UPDATE**, lalu
   memilih **PASANG UPDATE**. Android meminta izin “install unknown apps” bila belum
   pernah diberikan; persetujuan pemasangan tetap dilakukan oleh pengguna.

Pastikan seluruh APK ditandatangani dengan **keystore dan alias yang sama** dengan
rilis pertama. Android menolak pembaruan yang ditandatangani dengan kunci berbeda.
Updater membutuhkan koneksi internet; jika GitHub belum memiliki rilis stabil atau
rilis terbaru tidak berisi APK, aplikasi tetap berjalan dan menampilkan pesan yang
bisa dicoba kembali.

### Masalah umum

| Gejala | Penyebab | Solusi |
|---|---|---|
| Crash saat dibuka | script error di startup | Development Build + lihat log |
| Layar hitam | kamera salah / shader tak didukung | satu Main Camera aktif, tag `MainCamera`, pakai shader URP |
| Kontrol mati | EventSystem / GraphicRaycaster hilang | cek keduanya ada di scene |
| UI terpotong | Canvas Scaler salah | Scale With Screen Size 1920x1080, Match 0.5 |
| Error Gradle/JDK | konfigurasi eksternal | Preferences > External Tools → pakai JDK/Gradle bawaan Unity |
| Lag | kualitas terlalu tinggi | ikuti tabel optimasi di atas |

---

## 10. QA - Wajib Lulus 3 Kali Berturut

Checklist utama:

- [ ] NavMesh ter-bake, musuh tidak menembus dinding / jatuh
- [ ] Skor, nyawa, timer, coin reset setelah restart
- [ ] High score tidak hilang setelah aplikasi ditutup
- [ ] Coin tidak melayang atau tertanam di terrain
- [ ] Pohon/ornamen tidak menutupi jalur atau kamera
- [ ] Mobil tidak tersangkut
- [ ] Tombol cukup besar, tidak tertutup notch
- [ ] Suara tidak terlalu keras / tidak muncul
- [ ] Game BISA dimenangkan dan BISA dikalahkan
- [ ] Saat bounty penuh, event peringatan muncul tanpa menambah musuh melebihi batas 4
- [ ] Pause/resume/pindah scene tidak crash
- [ ] FPS stabil 30 dengan 4 musuh + particle

Skenario singkat (ulangi 3x tanpa error):

```text
Buka app -> PLAY -> kontrol (gas/brake/belok) -> ambil 5 coin
-> musuh kejar + kena (nyawa turun, kebal 2 dtk) -> PAUSE/RESUME
-> kumpulkan semua coin -> gerbang hijau + lentera menyala
-> masuk gerbang = MENANG -> RESTART (state reset)
-> main lagi, biarkan nyawa 0 = GAME OVER -> MENU -> BEST tersimpan
```

---

## 11. Video Presentasi (maks. 5 menit)

| No | Bagian | Durasi | Mulai |
|---|---|---|---|
| 1 | Pembuka + judul | 20 dtk | 0:00 |
| 2 | Konsep dan tujuan | 30 dtk | 0:20 |
| 3 | Kontrol | 20 dtk | 0:50 |
| 4 | Mekanik inti dan collectible | 60 dtk | 1:10 |
| 5 | Enemy dan obstacle | 45 dtk | 2:10 |
| 6 | Power-up, misi, fitur unik | 45 dtk | 2:55 |
| 7 | Gameplay penuh sampai menang/kalah | 60 dtk | 3:40 |
| 8 | Penutup | 20 dtk | 4:40 |

Rekam 13 adegan (lebih banyak dari kebutuhan), edit di CapCut, target final **<= 4:55**.

**Rekam layar:** resolusi 1080p, 30 FPS, suara internal + mic, orientasi landscape, **Mode Jangan Ganggu aktif**, baterai >= 80% dan dicharger, storage kosong >= 2 GB.

**Upload:** lampirkan video ke comment postingan kelas, beri komentar `Nama / NIM / Judul game / Engine`, lalu **buka ulang halaman untuk verifikasi** benar-benar terunggah.

---

## 12. Jadwal dan Cadangan

| Waktu | Kegiatan |
|---|---|
| Selasa malam | Tingkat A lulus tes |
| Rabu siang | Twist A jalan + APK pertama sukses |
| **Rabu malam** | **FREEZE FITUR** - nol fitur baru setelah ini |
| Kamis 07.00-08.00 | Build final (fallback: APK lama yang pernah sukses) |
| Kamis 08.00-09.30 | QA 3 putaran |
| Kamis 10.00-11.30 | Rekam video |
| Kamis 12.30-13.30 | Edit, durasi <= 4:55 |
| Kamis 14.00-14.30 | Upload + verifikasi |
| Kamis 14.30-15.30 | Buffer cadangan |
| 16.00-16.30 | Jendela pengumpulan (verifikasi akhir saja) |

**Urutan pemotongan bila waktu mepet:**
Magnet -> BountyMeter/panah -> Musik dua-track -> PathLights -> Musuh patroli -> Power-up -> Timer -> **ConvoySpawner (pertahankan terakhir)**.

Cara memotong aman: jangan hapus file script, cukup kosongkan reference Inspector atau nonaktifkan GameObject, lalu tes ulang.

---

## 13. Aset dan Kredit

- **Audio:** `CoinPickup.wav`, `ForestAmbience.wav`, `VictoryFanfare.wav`, `Fire.wav`, `BouncePad.wav`.
- **Material:** `Assets/Materials/CoinConvoy` (dibuat tool scenery), `Assets/Materials/EnchantedForest` (level lama).
- **Level lama:** scene `Level_1/2/3` dari tugas 3D Level Design, dipertahankan utuh.
- **Tidak ada plugin berbayar.** Seluruh tooling memakai fitur bawaan Unity: NavMesh (AI Navigation), TextMeshPro, URP, Particle, Audio.

---

## 14. Git

Proyek ini di-push ke GitHub dengan akun **`feliciaangeline007`**.

Folder yang di-ignore (lihat `.gitignore`): `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`, build artifacts (`*.apk`), dan cache editor lainnya. Yang di-commit: `Assets/`, `Packages/`, `ProjectSettings/`, `.gitignore`, dan `README.md`.

---

## 15. Troubleshooting Cepat

| Gejala | Cek pertama |
|---|---|
| Console error merah | baca baris pertama error, biasanya nama script/field |
| Musuh diam | NavMesh belum di-bake (biru tidak ada di Scene View) |
| Skor tidak naik | `GameManager` hilang / field UI belum diisi |
| Coin tidak bisa diambil | `Is Trigger` ON, tag mobil = `Player` |
| Gerbang tidak terbuka | field `Escape Gate` di GameManager kosong |
| Mobil terbang saat mulai | posisi spawn terlalu dekat collider (naikkan Y) |
| Input keyboard mati | Active Input Handling = Both |
| Scene kosong di HP | Build Settings: MainMenu index 0, Game index 1 |
