using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Node Energi / Koin Data Cyberpunk yang mengapung di atas lintasan.
    /// Memberikan skor, mengisi nitro, dan bersuara denting saat diambil.
    /// </summary>
    public class CollectibleNode : MonoBehaviour
    {
        public float rotateSpeed = 120f;
        public float bobHeight = 0.25f;
        public float bobSpeed = 2.5f;
        public float nitroRefill = 18f;
        public AudioClip pickupAudio;

        private Vector3 _initialPosition;
        private bool _isCollected = false;

        private void Start()
        {
            _initialPosition = transform.position;
        }

        private void Update()
        {
            if (_isCollected) return;

            float dt = Time.deltaTime;
            transform.Rotate(Vector3.up, rotateSpeed * dt, Space.World);

            Vector3 pos = _initialPosition;
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = pos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
            {
                Collect(other.GetComponentInParent<CarController>());
            }
        }

        private void Collect(CarController car)
        {
            _isCollected = true;

            if (car != null)
            {
                car.AddNitro(nitroRefill);
            }

            if (pickupAudio != null)
            {
                AudioSource.PlayClipAtPoint(pickupAudio, transform.position, 0.9f);
            }
            else
            {
                CyberSoundManager.Instance?.PlayDataChime();
            }

            RacingGameManager.Instance?.OnNodeCollected();
            Destroy(gameObject);
        }
    }
}
