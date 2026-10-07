using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberpunkRacing
{
    public enum GameState
    {
        Countdown,
        Racing,
        Paused,
        Finished
    }

    /// <summary>
    /// Manajer Utama Alur Balapan Cyberpunk Highway.
    /// Mengatur fase hitung mundur 3-2-1-GO, waktu balapan, koleksi node data, dan status menang/kalah.
    /// </summary>
    public class RacingGameManager : MonoBehaviour
    {
        public static RacingGameManager Instance { get; private set; }

        [Header("Target & Waktu")]
        public float totalRaceTime = 90f;
        public int totalNodes = 25;
        public int collectedNodes = 0;

        [Header("Status (Read-only)")]
        public GameState State { get; private set; } = GameState.Countdown;
        public float TimeRemaining { get; private set; }
        public float ElapsedTime { get; private set; }
        public float CountdownTimer { get; private set; } = 3.5f;
        public bool PlayerWon { get; private set; } = false;

        private void Awake()
        {
            // Singleton guard — hancurkan duplikat jika ada
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Reset state penuh setiap scene load
            Time.timeScale = 1f;
            TimeRemaining = totalRaceTime;
            ElapsedTime = 0f;
            CountdownTimer = 3.5f;
            State = GameState.Countdown;
            PlayerWon = false;
            collectedNodes = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            switch (State)
            {
                case GameState.Countdown:
                    UpdateCountdown();
                    break;
                case GameState.Racing:
                    UpdateRacing();
                    break;
            }
        }

        private void UpdateCountdown()
        {
            float prev = CountdownTimer;
            CountdownTimer -= Time.deltaTime;

            int prevSec = Mathf.CeilToInt(prev);
            int curSec = Mathf.CeilToInt(CountdownTimer);

            if (curSec < prevSec && curSec > 0)
            {
                CyberSoundManager.Instance?.PlayCountdownBeep(false);
            }

            if (CountdownTimer <= 0f)
            {
                CountdownTimer = 0f;
                State = GameState.Racing;
                CyberSoundManager.Instance?.PlayCountdownBeep(true);
            }
        }

        private void UpdateRacing()
        {
            ElapsedTime += Time.deltaTime;
            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                FinishRace(false);
            }
        }

        public void OnNodeCollected()
        {
            collectedNodes++;
        }

        public void OnFinishLineCrossed()
        {
            if (State == GameState.Racing)
            {
                FinishRace(true);
            }
        }

        public void FinishRace(bool won)
        {
            if (State == GameState.Finished) return;

            State = GameState.Finished;
            PlayerWon = won;
            Time.timeScale = 0f;

            if (won)
            {
                CyberSoundManager.Instance?.PlayVictory();
                float bestTime = PlayerPrefs.GetFloat("BestRaceTime", 9999f);
                if (ElapsedTime < bestTime)
                {
                    PlayerPrefs.SetFloat("BestRaceTime", ElapsedTime);
                    PlayerPrefs.Save();
                }

                // Hadiah Cyber Credits balapan
                int earnedCredits = 300 + (collectedNodes * 20);
                DailyRewardManager.AddCredits(earnedCredits);
            }
        }

        public void TogglePause()
        {
            // Jangan izinkan pause saat countdown atau sudah selesai
            if (State == GameState.Finished) return;
            if (State == GameState.Countdown) return;

            if (State == GameState.Paused)
            {
                State = GameState.Racing;
                Time.timeScale = 1f;
            }
            else
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
            }
        }

        public void RestartRace()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            LoadSceneSafe("CyberpunkMainMenu");
        }

        /// <summary>
        /// Muat scene dengan nama, aman jika scene belum didaftarkan di Build Settings.
        /// </summary>
        public static void LoadSceneSafe(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"[RacingGameManager] Scene '{sceneName}' tidak ditemukan di Build Settings! " +
                               "Jalankan Tools > Cyberpunk Racing > Generate Semua Scene terlebih dahulu.");
            }
        }
    }
}
