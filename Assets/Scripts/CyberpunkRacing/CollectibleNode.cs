using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Node Energi / Data yang mengapung di atas lintasan.
    /// Memberikan nitro dan menambah skor koleksi saat diambil mobil.
    /// </summary>
    public class CollectibleNode : MonoBehaviour
    {
        public float rotateSpeed = 120f;
        public float bobHeight   = 0.25f;
        public float bobSpeed    = 2.5f;
        public float nitroRefill = 18f;
        public AudioClip pickupAudio;

        private Vector3 _initialPosition;
        private bool    _isCollected = false;

        private void Start()
        {
            _initialPosition = transform.position;
        }

        private void Update()
        {
            if (_isCollected) return;

            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            Vector3 pos = _initialPosition;
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = pos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            // Cari CarController satu kali saja, bukan dua kali
            CarController car = other.GetComponentInParent<CarController>();
            bool isPlayer     = car != null || other.CompareTag("Player");

            if (!isPlayer) return;

            Collect(car);
        }

        private void Collect(CarController car)
        {
            _isCollected = true;

            car?.AddNitro(nitroRefill);

            if (pickupAudio != null)
                AudioSource.PlayClipAtPoint(pickupAudio, transform.position, 0.9f);
            else
                CyberSoundManager.Instance?.PlayDataChime();

            RacingGameManager.Instance?.OnNodeCollected();
            Destroy(gameObject);
        }
    }
}
