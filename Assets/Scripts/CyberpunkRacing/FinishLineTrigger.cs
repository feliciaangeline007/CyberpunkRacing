using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Gerbang Garis Akhir (Finish Line Gate) di Ujung Lintasan Highway.
    /// Memanggil OnFinishLineCrossed pada RacingGameManager saat mobil melewatinya.
    /// </summary>
    public class FinishLineTrigger : MonoBehaviour
    {
        private bool _triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
            {
                _triggered = true;
                RacingGameManager.Instance?.OnFinishLineCrossed();
            }
        }
    }
}
