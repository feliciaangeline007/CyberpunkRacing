using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Gerbang Garis Finish di ujung lintasan.
    /// Memanggil OnFinishLineCrossed pada RacingGameManager saat mobil melewatinya.
    /// Pastikan Collider di-set sebagai Trigger di Inspector.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinishLineTrigger : MonoBehaviour
    {
        private bool _triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;

            // Cek apakah yang masuk adalah mobil pemain
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null && !other.CompareTag("Player")) return;

            _triggered = true;
            RacingGameManager.Instance?.OnFinishLineCrossed();
        }
    }
}
