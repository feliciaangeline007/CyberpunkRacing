using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Pengendali Mobil Balap Cyberpunk Berkecepatan Tinggi.
    /// Menggunakan fisika arcade responsif yang stabil, tahan benturan, dan tidak mudah terbalik.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Performa & Mesin")]
        public float acceleration = 24f;
        public float maxSpeedKmh = 145f;
        public float maxReverseKmh = 35f;
        public float steerAngle = 36f;
        public float steerSpeed = 8f;
        public float brakeStrength = 28f;
        public float downforce = 18f;
        public bool autoThrottle = true; // Gaya Asphalt: selalu maju jika tidak rem

        [Header("Sistem Nitro")]
        public float maxNitro = 100f;
        public float currentNitro = 100f;
        public float nitroSpeedMultiplier = 1.45f; // Hingga ~210 KM/H
        public float nitroAccelMultiplier = 1.85f;
        public float nitroDrainRate = 32f;
        public float nitroRegenRate = 10f;

        [Header("Drift & Handling")]
        public float driftSteerMultiplier = 1.6f;
        public float driftLateralGrip = 3.2f;
        public float normalLateralGrip = 9.5f;

        [Header("Visual Roda & Bodi")]
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;
        public Transform carBodyVisual;

        [Header("Audio")]
        public AudioSource engineAudio;
        public float minPitch = 0.85f;
        public float maxPitch = 2.4f;

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

            // Mode Auto Throttle (Asphalt arcade)
            if (autoThrottle && !brake && Mathf.Abs(gas) < 0.05f)
            {
                gas = 1f;
            }

            // Status Nitro
            IsNitroActive = nitroWanted && currentNitro > 2f && gas > 0.05f && !brake;
            if (IsNitroActive)
            {
                currentNitro = Mathf.Max(0f, currentNitro - nitroDrainRate * Time.fixedDeltaTime);
            }
            else
            {
                currentNitro = Mathf.Min(maxNitro, currentNitro + nitroRegenRate * Time.fixedDeltaTime);
            }

            float currentSpeedMs = _rb.linearVelocity.magnitude;
            float forwardVelocity = Vector3.Dot(_rb.linearVelocity, transform.forward);

            // Batas kecepatan saat ini
            float speedCapMs = (maxSpeedKmh / 3.6f) * (IsNitroActive ? nitroSpeedMultiplier : 1f);
            float accelRate = acceleration * (IsNitroActive ? nitroAccelMultiplier : 1f);

            // 1. Tenaga Mesin / Rem
            if (brake)
            {
                float brakeScale = handbrake ? 0.35f : 1f;
                Vector3 brakeVector = -_rb.linearVelocity.normalized * brakeStrength * brakeScale * _rb.mass;
                _rb.AddForce(brakeVector, ForceMode.Force);
            }
            else if (gas > 0.05f)
            {
                if (forwardVelocity < speedCapMs)
                {
                    _rb.AddForce(transform.forward * (gas * accelRate * _rb.mass), ForceMode.Force);
                }
            }
            else if (gas < -0.05f)
            {
                float reverseCap = maxReverseKmh / 3.6f;
                if (forwardVelocity > 0.5f)
                {
                    _rb.AddForce(-transform.forward * (brakeStrength * _rb.mass), ForceMode.Force);
                }
                else if (forwardVelocity > -reverseCap)
                {
                    _rb.AddForce(transform.forward * (gas * acceleration * 0.65f * _rb.mass), ForceMode.Force);
                }
            }
            else
            {
                // Hambatan udara alami
                _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, Vector3.zero, 3.5f * Time.fixedDeltaTime);
            }

            // 2. Kemudi & Drifting
            IsDrifting = (handbrake || (brake && Mathf.Abs(steer) > 0.2f)) && currentSpeedMs > 6f;
            float steerMult = IsDrifting ? driftSteerMultiplier : 1f;
            _currentSteer = Mathf.Lerp(_currentSteer, steer * steerMult, steerSpeed * Time.fixedDeltaTime);

            if (currentSpeedMs > 0.3f)
            {
                float turnDirection = Mathf.Sign(forwardVelocity);
                float turnAngle = _currentSteer * steerAngle * turnDirection * Time.fixedDeltaTime;
                transform.Rotate(Vector3.up, turnAngle);
            }

            // 3. Gaya Cengkeram Samping (Lateral Grip)
            Vector3 sideVelocity = transform.right * Vector3.Dot(_rb.linearVelocity, transform.right);
            float grip = IsDrifting ? driftLateralGrip : normalLateralGrip;
            _rb.AddForce(-sideVelocity * grip * _rb.mass * Time.fixedDeltaTime, ForceMode.Impulse);

            // 4. Downforce (Menjaga ban menempel pada aspal)
            _rb.AddForce(-Vector3.up * (downforce * currentSpeedMs * _rb.mass * 0.05f), ForceMode.Force);

            // 5. Stabilisator Anti-Terbalik
            Vector3 rot = transform.eulerAngles;
            if (rot.z > 35f && rot.z < 325f)
            {
                rot.z = Mathf.MoveTowardsAngle(rot.z, 0f, 75f * Time.fixedDeltaTime);
                transform.eulerAngles = rot;
            }

            // Update Efek Partikel
            UpdateParticleEmissions();
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
            // Jika mobil masih menapak aspal jalan, perbarui posisi aman
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 3.5f))
            {
                RecordSafePosition();
            }

            // Jika mobil jatuh di bawah batas jurang, kembalikan ke lintasan
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
            transform.SetPositionAndRotation(_lastSafePosition + Vector3.up * 0.5f, _lastSafeRotation);
            _currentSteer = 0f;
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
            float ratio = SpeedRatio;
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
