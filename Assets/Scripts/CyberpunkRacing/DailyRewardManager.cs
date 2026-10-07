using System;
using UnityEngine;

namespace CyberpunkRacing
{
    [System.Serializable]
    public struct DailyRewardItem
    {
        public int dayNumber;
        public int credits;
        public string title;
        public string description;
        public string icon;
    }

    /// <summary>
    /// Sistem Hadiah Harian (Daily Reward System) Cyberpunk Racing:
    /// - Melacak login beruntun pemain selama 7 hari berturut-turut.
    /// - Mengelola saldo mata uang dalam game (Cyber Credits).
    /// - Reset harian otomatis berdasarkan tanggal kalender lokal.
    /// - Menghitung hitung mundur waktu (countdown) menuju hadiah berikutnya.
    /// </summary>
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        private const string PrefsCredits = "CyberCredits";
        private const string PrefsStreak = "DailyRewardStreak";
        private const string PrefsLastClaimDate = "DailyRewardLastClaimDate";
        private const string DateFormat = "yyyy-MM-dd";

        // Daftar Hadiah 7 Hari
        public static readonly DailyRewardItem[] SevenDayRewards = new DailyRewardItem[]
        {
            new DailyRewardItem { dayNumber = 1, credits = 500,  title = "Bonus Sambutan",   description = "Starter Cyber Credits",       icon = "🪙" },
            new DailyRewardItem { dayNumber = 2, credits = 750,  title = "Tuning Chip",       description = "Peningkatan Performa Mobil",  icon = "⚡" },
            new DailyRewardItem { dayNumber = 3, credits = 1000, title = "Neon Booster",      description = "Cadangan Nitro Tambahan",    icon = "🔋" },
            new DailyRewardItem { dayNumber = 4, credits = 1500, title = "Quantum Battery",   description = "Efisiensi Energi Tinggi",    icon = "💎" },
            new DailyRewardItem { dayNumber = 5, credits = 2200, title = "Cyber V8 Engine",   description = "Akselerasi Lebih Responsif", icon = "🚀" },
            new DailyRewardItem { dayNumber = 6, credits = 3500, title = "Titanium Chassis",  description = "Ketahanan & Kecepatan Maks",  icon = "🛡️" },
            new DailyRewardItem { dayNumber = 7, credits = 5000, title = "GRAND APEX CROWN",  description = "Grand Prize Juara Metropolis!",icon = "👑" }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Beri modal awal jika pemain baru pertama kali main
            if (!PlayerPrefs.HasKey(PrefsCredits))
            {
                PlayerPrefs.SetInt(PrefsCredits, 1000);
                PlayerPrefs.Save();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── KREDIT / MATA UANG ────────────────────────────────────────────────

        public static int GetCredits()
        {
            return PlayerPrefs.GetInt(PrefsCredits, 1000);
        }

        public static void AddCredits(int amount)
        {
            if (amount <= 0) return;
            int cur = GetCredits();
            PlayerPrefs.SetInt(PrefsCredits, cur + amount);
            PlayerPrefs.Save();
        }

        public static bool SpendCredits(int amount)
        {
            if (amount <= 0) return true;
            int cur = GetCredits();
            if (cur >= amount)
            {
                PlayerPrefs.SetInt(PrefsCredits, cur - amount);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        // ── LOGIKA HADIAH HARIAN ──────────────────────────────────────────────

        /// <summary>
        /// Mengembalikan streak hari saat ini (1 sampai 7).
        /// </summary>
        public static int GetStreak()
        {
            int streak = PlayerPrefs.GetInt(PrefsStreak, 1);
            if (streak < 1) streak = 1;
            if (streak > 7) streak = 7;
            return streak;
        }

        /// <summary>
        /// Mengecek apakah hadiah hari ini siap diklaim.
        /// </summary>
        public static bool CanClaimToday()
        {
            string lastClaimStr = PlayerPrefs.GetString(PrefsLastClaimDate, string.Empty);
            if (string.IsNullOrEmpty(lastClaimStr))
            {
                return true; // Belum pernah klaim sama sekali
            }

            if (DateTime.TryParse(lastClaimStr, out DateTime lastClaimDate))
            {
                DateTime today = DateTime.Today;
                // Jika tanggal klaim terakhir sebelum hari ini, berarti siap klaim
                return today > lastClaimDate.Date;
            }

            return true;
        }

        /// <summary>
        /// Mendapatkan indeks hari yang akan diklaim saat ini (1 sampai 7).
        /// Otomatis mengecek apakah streak masih berlanjut atau putus.
        /// </summary>
        public static int GetCurrentClaimDay()
        {
            string lastClaimStr = PlayerPrefs.GetString(PrefsLastClaimDate, string.Empty);
            int currentStreak = PlayerPrefs.GetInt(PrefsStreak, 1);

            if (string.IsNullOrEmpty(lastClaimStr))
            {
                return 1;
            }

            if (DateTime.TryParse(lastClaimStr, out DateTime lastClaimDate))
            {
                DateTime today = DateTime.Today;
                TimeSpan diff = today - lastClaimDate.Date;

                if (diff.Days == 0)
                {
                    // Sudah diklaim hari ini, kembalikan hari yang tadi diklaim
                    return Mathf.Clamp(currentStreak, 1, 7);
                }
                else if (diff.Days == 1)
                {
                    // Kemarin diklaim, hari ini streak berlanjut
                    int nextDay = currentStreak + 1;
                    if (nextDay > 7) nextDay = 1; // Looping setelah hari ke-7
                    return nextDay;
                }
                else
                {
                    // Lebih dari 1 hari absen, streak kembali ke hari ke-1
                    return 1;
                }
            }

            return 1;
        }

        /// <summary>
        /// Hitung sisa waktu hingga reset hari berikutnya (tengah malam 00:00).
        /// </summary>
        public static TimeSpan GetTimeUntilNextReset()
        {
            DateTime now = DateTime.Now;
            DateTime midnight = DateTime.Today.AddDays(1);
            TimeSpan diff = midnight - now;
            return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
        }

        /// <summary>
        /// Format sisa waktu: "HH:mm:ss"
        /// </summary>
        public static string GetFormattedTimeUntilNextReset()
        {
            TimeSpan t = GetTimeUntilNextReset();
            return $"{t.Hours:D2}j {t.Minutes:D2}m {t.Seconds:D2}d";
        }

        /// <summary>
        /// Eksekusi klaim hadiah hari ini.
        /// </summary>
        public static bool ClaimToday(out DailyRewardItem claimedReward)
        {
            claimedReward = default;
            if (!CanClaimToday()) return false;

            int dayToClaim = GetCurrentClaimDay();
            claimedReward = SevenDayRewards[dayToClaim - 1];

            // Tambahkan kredit
            AddCredits(claimedReward.credits);

            // Simpan status
            PlayerPrefs.SetInt(PrefsStreak, dayToClaim);
            PlayerPrefs.SetString(PrefsLastClaimDate, DateTime.Today.ToString(DateFormat));
            PlayerPrefs.Save();

            Debug.Log($"[DailyReward] Berhasil mengklaim Hadiah Hari ke-{dayToClaim}: +{claimedReward.credits} Cyber Credits ({claimedReward.title})");
            return true;
        }
    }
}
