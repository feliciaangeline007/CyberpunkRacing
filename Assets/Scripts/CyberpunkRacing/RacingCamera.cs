using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Kamera Mengikuti Mobil Sinematik (Chase Camera) untuk Balap Cyberpunk.
    /// Dilengkapi FOV Dinamis saat mengebut/nitro dan getaran halus untuk sensasi kecepatan arcade serta getaran benturan saat menabrak.
    /// </summary>
    public class RacingCamera : MonoBehaviour
    {
        public static RacingCamera Instance { get; private set; }

        public Transform target;
        public Vector3 offset = new Vector3(0f, 2.3f, -5.6f);
        public float positionSmoothness = 12f;
        public float rotationSmoothness = 9f;
        public float lookAheadDistance = 14f;

        [Header("Dinamika FOV")]
        public float baseFov = 60f;
        public float maxFov = 75f;
        public float nitroFovBonus = 12f;
        public float shakeIntensity = 0.12f;

        private Camera _cam;
        private CarController _car;
        private float _impactShakeAmount = 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (target != null)
            {
                _car = target.GetComponent<CarController>();
            }
            else
            {
                _car = FindAnyObjectByType<CarController>();
                if (_car != null) target = _car.transform;
            }
        }

        public void TriggerImpactShake(float amount)
        {
            _impactShakeAmount = Mathf.Max(_impactShakeAmount, amount);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. Posisi Kamera di belakang mobil
            Vector3 desiredPosition = target.position + target.rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothness * Time.deltaTime);

            // 2. Pandangan ke titik depan mobil
            Vector3 lookTarget = target.position + target.forward * lookAheadDistance + Vector3.up * 0.9f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothness * Time.deltaTime);

            // 3. Efek Kecepatan (FOV & Shake)
            if (_cam != null && _car != null)
            {
                float targetFov = Mathf.Lerp(baseFov, maxFov, _car.SpeedRatio);
                if (_car.IsNitroActive) targetFov += nitroFovBonus;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, 5f * Time.deltaTime);

                // Redakan guncangan benturan secara bertahap
                _impactShakeAmount = Mathf.MoveTowards(_impactShakeAmount, 0f, 1.8f * Time.deltaTime);

                // Gabungkan guncangan kecepatan (nitro/drift) dengan guncangan tabrakan
                float speedShake = _car.IsNitroActive ? shakeIntensity : (_car.IsDrifting ? shakeIntensity * 0.4f : 0f);
                float totalShake = speedShake + _impactShakeAmount;

                if (totalShake > 0f)
                {
                    float t = Time.unscaledTime * 32f;
                    Vector3 shakeOffset = new Vector3(
                        (Mathf.PerlinNoise(t, 0.25f) - 0.5f) * 2f,
                        (Mathf.PerlinNoise(0.75f, t) - 0.5f) * 2f,
                        0f
                    ) * totalShake;
                    transform.position += transform.rotation * shakeOffset;
                }
            }
        }
    }
}
