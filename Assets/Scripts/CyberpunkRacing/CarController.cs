using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Pengendali Mobil Balap Cyberpunk Berkecepatan Tinggi (Arcade Muscle Car):
    /// - Menggunakan fisika arcade responsif yang stabil, anti-terbalik (FreezeRotationX/Z), dan bebas tersangkut.
    /// - Kontrol manual penuh (W/S/A/D atau Panah), mendukung drift (Space), nitro (Shift/N), dan reset lintasan (R).
    /// - Aman saat countdown 3-2-1 (mobil tidak meluncur sendiri sebelum 'GO!').
    /// - Sistem luncur dinding otomatis (Wall Slide) agar mobil tidak macet saat menyenggol pagar neon.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Performa & Mesin")]
        public float acceleration = 26f;
        public float maxSpeedKmh = 160f;
        public float maxReverseKmh = 40f;
        public float steerAngle = 40f;
        public float steerSpeed = 12f;
        public float brakeStrength = 36f;
        public float downforce = 22f;
        public bool autoThrottle = false; // Default: Manual kontrol (W / Panah Atas)

        [Header("Sistem Nitro")]
        public float maxNitro = 100f;
        public float currentNitro = 100f;
        public float nitroSpeedMultiplier = 1.45f; // Hingga ~232 KM/H
        public float nitroAccelMultiplier = 1.85f;
        public float nitroDrainRate = 30f;
        public float nitroRegenRate = 12f;

        [Header("Drift & Handling")]
        public float driftSteerMultiplier = 1.65f;
        public float driftLateralGrip = 3.5f;
        public float normalLateralGrip = 18.0f; // Cengkeraman kuat saat mengemudi normal (tidak licin)

        [Header("Visual Roda & Bodi")]
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;
        public Transform carBodyVisual;

        [Header("Audio")]
        public AudioSource engineAudio;
        public float minPitch = 0.85f;
        public float maxPitch = 2.45f;

        [Header("Keselamatan Lintasan")]
        public float fallRespawnY = -8f;

        // Properti Publik untuk HUD & Kamera
        public float CurrentSpeedKmh => _rb != null ? _rb.linearVelocity.magnitude * 3.6f : 0f;
        public float SpeedRatio => Mathf.Clamp01(CurrentSpeedKmh / maxSpeedKmh);
        public float NitroRatio => maxNitro > 0f ? Mathf.Clamp01(currentNitro / maxNitro) : 0f;
        public bool IsNitroActive { get; private set; }
        public bool IsDrifting { get; private set; }

        private Rigidbody _rb;
        private float _currentSteer = 0f;
        private float _wheelSpinAngle = 0f;

        // Visual Partikel Nitro & Asap
        private ParticleSystem _driftSmoke;
        private ParticleSystem _nitroFlameL;
        private ParticleSystem _nitroFlameR;
        private Light _nitroLight;

        // Checkpoint & Safe Respawn
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;
        private static Texture2D _sharedSmokeTex;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = 1200f;
            _rb.centerOfMass = new Vector3(0f, -0.45f, 0.05f);
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // KUNCI: Kunci rotasi X dan Z agar mobil tidak pernah terbalik atau jungkir balik!
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            RecordSafePosition();
            SetupVisualEffects();
        }

        private void Start()
        {
            if (engineAudio != null && !engineAudio.isPlaying)
            {
                engineAudio.loop = true;
                engineAudio.Play();
            }
        }

        private void Update()
        {
            // Input manual reset lintasan (R)
            if (CarInputManager.Instance != null && CarInputManager.Instance.ResetRequested)
            {
                RespawnToSafePosition();
            }

            UpdateAudio();
            AnimateWheelsAndBody();
        }

        private void FixedUpdate()
        {
            ApplyMovementPhysics();
            CheckTrackSafety();
        }

        private void ApplyMovementPhysics()
        {
            var input = CarInputManager.Instance;
            float gas = input != null ? input.Throttle : 0f;
            float steer = input != null ? input.Steer : 0f;
            bool brake = input != null && input.Brake;
            bool nitroWanted = input != null && input.Nitro;
            bool handbrake = input != null && input.Handbrake;

            // Mode Auto Throttle opsional
            if (autoThrottle && !brake && Mathf.Abs(gas) < 0.05f)
            {
                gas = 1f;
            }

            // KUNCI PERBAIKAN GAMEPLAY: Kunci mobil saat fase COUNTDOWN atau FINISHED
            var gm = RacingGameManager.Instance;
            bool canDrive = gm == null || gm.State == GameState.Racing;
            if (!canDrive)
            {
                gas = 0f;
                brake = true;
                nitroWanted = false;
            }

            // Status Nitro
            IsNitroActive = canDrive && nitroWanted && currentNitro > 2f && gas > 0.05f && !brake;
            if (IsNitroActive)
            {
                currentNitro = Mathf.Max(0f, currentNitro - nitroDrainRate * Time.fixedDeltaTime);
            }
            else
            {
                currentNitro = Mathf.Min(maxNitro, currentNitro + nitroRegenRate * Time.fixedDeltaTime);
            }

            Vector3 currentVel = _rb.linearVelocity;
            float currentSpeedMs = currentVel.magnitude;
            float forwardVelocity = Vector3.Dot(currentVel, transform.forward);

            // Batas kecepatan saat ini
            float speedCapMs = (maxSpeedKmh / 3.6f) * (IsNitroActive ? nitroSpeedMultiplier : 1f);
            float accelRate = acceleration * (IsNitroActive ? nitroAccelMultiplier : 1f);

            // 1. Tenaga Mesin / Rem / Mundur
            if (brake)
            {
                // Jika sedang melaju maju kencang: rem kuat
                if (forwardVelocity > 1.2f)
                {
                    float bScale = handbrake ? 0.45f : 1.0f;
                    Vector3 brakeForce = -transform.forward * (brakeStrength * bScale * _rb.mass);
                    _rb.AddForce(brakeForce, ForceMode.Force);
                }
                // Jika mobil sudah berhenti atau mundur: mundur secara halus
                else if (canDrive && gas < -0.1f)
                {
                    float reverseCap = maxReverseKmh / 3.6f;
                    if (forwardVelocity > -reverseCap)
                    {
                        _rb.AddForce(-transform.forward * (acceleration * 0.7f * _rb.mass), ForceMode.Force);
                    }
                }
            }
            else if (gas > 0.05f && canDrive)
            {
                // Kurva akselerasi halus: semakin mendekati kecepatan puncak, akselerasi melandai alami
                float speedRatio = Mathf.Clamp01(forwardVelocity / speedCapMs);
                float effectiveAccel = accelRate * (1f - speedRatio * 0.45f);

                if (forwardVelocity < speedCapMs)
                {
                    _rb.AddForce(transform.forward * (gas * effectiveAccel * _rb.mass), ForceMode.Force);
                }
            }
            else if (gas < -0.05f && canDrive)
            {
                // Tekan S / Panah Bawah: Rem dulu jika maju, mundur jika berhenti
                if (forwardVelocity > 0.8f)
                {
                    _rb.AddForce(-transform.forward * (brakeStrength * _rb.mass), ForceMode.Force);
                }
                else
                {
                    float reverseCap = maxReverseKmh / 3.6f;
                    if (forwardVelocity > -reverseCap)
                    {
                        _rb.AddForce(transform.forward * (gas * acceleration * 0.65f * _rb.mass), ForceMode.Force);
                    }
                }
            }
            else
            {
                // Hambatan gelinding alami (coasting drag)
                Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                Vector3 damped = Vector3.MoveTowards(flatVel, Vector3.zero, 3.2f * Time.fixedDeltaTime);
                _rb.linearVelocity = new Vector3(damped.x, _rb.linearVelocity.y, damped.z);
            }

            // 2. Kemudi Responsif & Belok Berkecepatan Tinggi
            IsDrifting = (handbrake || (brake && Mathf.Abs(steer) > 0.15f)) && currentSpeedMs > 5.5f;
            float steerMult = IsDrifting ? driftSteerMultiplier : 1.0f;
            _currentSteer = Mathf.Lerp(_currentSteer, steer * steerMult, steerSpeed * Time.fixedDeltaTime);

            if (currentSpeedMs > 0.25f)
            {
                float turnDir = Mathf.Sign(forwardVelocity);
                if (Mathf.Abs(forwardVelocity) < 0.2f) turnDir = 1f;

                // Dinamika belok adaptif: di kecepatan tinggi tetap bisa bermanuver di tikungan tol
                float speedFactor = Mathf.Clamp(1.2f - (currentSpeedMs / (maxSpeedKmh / 3.6f)) * 0.35f, 0.72f, 1.25f);
                float turnAngle = _currentSteer * steerAngle * turnDir * speedFactor * Time.fixedDeltaTime;

                // Gunakan MoveRotation agar sinkron dengan PhysX interpolation
                Quaternion deltaRot = Quaternion.Euler(0f, turnAngle, 0f);
                _rb.MoveRotation(_rb.rotation * deltaRot);
            }

            // 3. Cengkeraman Samping (Lateral Grip) - Menghilangkan efek 'meluncur di atas es'
            Vector3 localVel = transform.InverseTransformDirection(_rb.linearVelocity);
            float gripSpeed = IsDrifting ? driftLateralGrip : normalLateralGrip;
            localVel.x = Mathf.MoveTowards(localVel.x, 0f, gripSpeed * Time.fixedDeltaTime * 15f);
            _rb.linearVelocity = transform.TransformDirection(localVel);

            // 4. Downforce (Menjaga mobil tetap menempel di aspal saat melaju kencang)
            _rb.AddForce(-Vector3.up * (downforce * currentSpeedMs * _rb.mass * 0.04f), ForceMode.Force);

            // Update Efek Partikel
            UpdateParticleEmissions();
        }

        private void OnCollisionStay(Collision collision)
        {
            // SISTEM WALL-SLIDE: Jika mobil menyenggol pagar pembatas tol layang,
            // alihkan kecepatannya di sepanjang dinding agar tidak macet / tersendat!
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (Mathf.Abs(contact.normal.y) < 0.45f)
                {
                    Vector3 wallTangent = Vector3.Cross(contact.normal, Vector3.up);
                    if (Vector3.Dot(wallTangent, transform.forward) < 0f)
                    {
                        wallTangent = -wallTangent;
                    }
                    float fwdSpd = Vector3.Dot(_rb.linearVelocity, wallTangent);
                    if (fwdSpd > 0.5f)
                    {
                        _rb.linearVelocity = wallTangent * (fwdSpd * 0.95f) + Vector3.up * _rb.linearVelocity.y;
                    }
                    break;
                }
            }
        }

        private void UpdateParticleEmissions()
        {
            if (_driftSmoke != null)
            {
                var em = _driftSmoke.emission;
                em.rateOverTime = IsDrifting ? 45f : (IsNitroActive ? 15f : 0f);
            }

            if (_nitroFlameL != null && _nitroFlameR != null)
            {
                var emL = _nitroFlameL.emission;
                var emR = _nitroFlameR.emission;
                float rate = IsNitroActive ? 55f : 0f;
                emL.rateOverTime = rate;
                emR.rateOverTime = rate;
            }

            if (_nitroLight != null)
            {
                _nitroLight.intensity = IsNitroActive ? (2.5f + Mathf.PingPong(Time.time * 28f, 1f)) : 0f;
            }
        }

        private void CheckTrackSafety()
        {
            // Jika mobil masih menapak aspal jalan, simpan posisi aman
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 3.5f))
            {
                RecordSafePosition();
            }

            // Jika mobil jatuh di bawah batas jurang, kembalikan otomatis ke lintasan
            if (transform.position.y < fallRespawnY)
            {
                RespawnToSafePosition();
            }
        }

        public void RecordSafePosition()
        {
            _lastSafePosition = transform.position;
            _lastSafeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        public void RespawnToSafePosition()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_lastSafePosition + Vector3.up * 0.6f, _lastSafeRotation);
            _currentSteer = 0f;
            // Beri dorongan awal lembut searah lintasan
            _rb.AddForce(transform.forward * 5f, ForceMode.VelocityChange);
        }

        public void AddNitro(float amount)
        {
            currentNitro = Mathf.Clamp(currentNitro + amount, 0f, maxNitro);
        }

        private void AnimateWheelsAndBody()
        {
            float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
            _wheelSpinAngle += speed * 360f * Time.deltaTime / (2f * Mathf.PI * 0.38f);

            float frontSteerYaw = _currentSteer * 28f;

            if (frontLeftWheel != null)
                frontLeftWheel.localRotation = Quaternion.Euler(_wheelSpinAngle, frontSteerYaw, 0f);
            if (frontRightWheel != null)
                frontRightWheel.localRotation = Quaternion.Euler(_wheelSpinAngle, frontSteerYaw, 0f);
            if (rearLeftWheel != null)
                rearLeftWheel.localRotation = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);
            if (rearRightWheel != null)
                rearRightWheel.localRotation = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);

            if (carBodyVisual != null)
            {
                float roll = -_currentSteer * Mathf.Clamp01(speed / 12f) * 4.2f;
                carBodyVisual.localRotation = Quaternion.Euler(0f, 0f, roll);
            }
        }

        private void UpdateAudio()
        {
            if (engineAudio == null) return;
            var input = CarInputManager.Instance;
            float gasInput = input != null ? Mathf.Abs(input.Throttle) : 0f;

            // Berikan feedback suara revving mesin jika gas ditekan saat diam / countdown
            float ratio = Mathf.Max(SpeedRatio, gasInput * 0.45f);
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);
        }

        private void SetupVisualEffects()
        {
            if (_sharedSmokeTex == null)
            {
                _sharedSmokeTex = CreateCircleTexture(64);
            }

            var unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            var smokeMat = new Material(unlitShader) { mainTexture = _sharedSmokeTex };
            smokeMat.SetColor("_BaseColor", new Color(0.4f, 0.75f, 1f, 0.4f));

            // Drift Smoke di bawah roda belakang
            var smokeObj = new GameObject("DriftSmokeRoot");
            smokeObj.transform.SetParent(transform, false);
            smokeObj.transform.localPosition = new Vector3(0f, 0.15f, -1.6f);
            _driftSmoke = smokeObj.AddComponent<ParticleSystem>();

            var main = _driftSmoke.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 60;
            main.startLifetime = 0.65f;
            main.startSpeed = 1.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startColor = new Color(0.5f, 0.8f, 1f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = _driftSmoke.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.8f, 0.1f, 0.4f);

            var em = _driftSmoke.emission;
            em.rateOverTime = 0f;

            var rend = smokeObj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.sharedMaterial = smokeMat;

            // Nitro Flames
            var flameMat = new Material(unlitShader) { mainTexture = _sharedSmokeTex };
            _nitroFlameL = CreateSingleExhaustFlame("Flame_L", new Vector3(-0.52f, 0.35f, -2.15f), flameMat);
            _nitroFlameR = CreateSingleExhaustFlame("Flame_R", new Vector3( 0.52f, 0.35f, -2.15f), flameMat);

            // Point Light Nitro
            var lightObj = new GameObject("NitroLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.38f, -2.3f);
            _nitroLight = lightObj.AddComponent<Light>();
            _nitroLight.type = LightType.Point;
            _nitroLight.range = 5.5f;
            _nitroLight.color = new Color(0f, 0.92f, 1f);
            _nitroLight.intensity = 0f;
            _nitroLight.shadows = LightShadows.None;
        }

        private ParticleSystem CreateSingleExhaustFlame(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 50;
            main.startLifetime = 0.22f;
            main.startSpeed = 8.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = new Color(0f, 0.95f, 1f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.08f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.sharedMaterial = mat;

            return ps;
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            float rSqr = radius * radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float dSqr = dx * dx + dy * dy;
                    float alpha = dSqr <= rSqr ? Mathf.Clamp01(1f - (dSqr / rSqr)) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
