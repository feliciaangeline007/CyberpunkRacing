using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Pengendali Mobil Balap Cyberpunk Berkecepatan Tinggi (Arcade Muscle Car):
    /// - Menggunakan fisika arcade responsif yang stabil, anti-terbalik (FreezeRotationX/Z), dan bebas tersangkut.
    /// - Kontrol manual penuh (W/S/A/D atau Panah), mendukung drift (Space), nitro (Shift/N), dan reset lintasan (R).
    /// - Efek Visual Asap & Knalpot Tingkat Tinggi (AAA Stylised Synthwave Volumetric Cloud & Tire Sparks).
    /// - Sinkronisasi warna cat mobil pilihan pemain dari Menu Utama.
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
        public bool autoThrottle = false;

        [Header("Sistem Nitro")]
        public float maxNitro = 100f;
        public float currentNitro = 100f;
        public float nitroSpeedMultiplier = 1.45f;
        public float nitroAccelMultiplier = 1.85f;
        public float nitroDrainRate = 30f;
        public float nitroRegenRate = 12f;

        [Header("Drift & Handling")]
        public float driftSteerMultiplier = 1.65f;
        public float driftLateralGrip = 3.5f;
        public float normalLateralGrip = 18.0f;

        [Header("Visual Roda & Bodi")]
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;
        public Transform carBodyVisual;
        public Renderer carBodyRenderer;

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

        // Visual Partikel Asap Volumetrik & Percikan Api Ban
        private ParticleSystem _driftSmokeL;
        private ParticleSystem _driftSmokeR;
        private ParticleSystem _tireSparksL;
        private ParticleSystem _tireSparksR;
        private ParticleSystem _nitroFlameL;
        private ParticleSystem _nitroFlameR;
        private Light _nitroLight;

        // Checkpoint & Safe Respawn
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;

        private static Texture2D _sharedVolumetricSmokeTex;
        private static Texture2D _sharedSparkTex;

        private static readonly Color[] PaintPalette = new Color[]
        {
            new Color(0f, 0.9f, 1f),       // Cyber Cyan
            new Color(1f, 0.1f, 0.8f),     // Neon Magenta
            new Color(1f, 0.8f, 0.1f),     // Volt Gold
            new Color(0.12f, 0.14f, 0.18f) // Carbon Black
        };

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = 1200f;
            _rb.centerOfMass = new Vector3(0f, -0.45f, 0.05f);
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Kunci rotasi X dan Z agar mobil tidak pernah terbalik
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            RecordSafePosition();
            SetupEnhancedVisualEffects();
        }

        private void Start()
        {
            ApplySavedCarPaint();

            if (engineAudio != null && !engineAudio.isPlaying)
            {
                engineAudio.loop = true;
                engineAudio.Play();
            }
        }

        private void ApplySavedCarPaint()
        {
            int colorIdx = PlayerPrefs.GetInt("SelectedCarColor", 0);
            Color chosenColor = PaintPalette[Mathf.Clamp(colorIdx, 0, PaintPalette.Length - 1)];

            if (carBodyRenderer == null && carBodyVisual != null)
            {
                carBodyRenderer = carBodyVisual.GetComponentInChildren<Renderer>();
            }

            if (carBodyRenderer != null)
            {
                var mat = carBodyRenderer.material;
                if (mat != null)
                {
                    mat.color = chosenColor;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", chosenColor);
                }
            }

            // Sesuaikan warna neon underglow mobil jika ada
            var underglow = transform.Find("NeonUnderglow");
            if (underglow != null)
            {
                var ugRend = underglow.GetComponent<Renderer>();
                if (ugRend != null && ugRend.material != null)
                {
                    ugRend.material.color = chosenColor;
                    if (ugRend.material.HasProperty("_EmissionColor"))
                    {
                        ugRend.material.SetColor("_EmissionColor", chosenColor * 2.5f);
                    }
                }
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

            if (autoThrottle && !brake && Mathf.Abs(gas) < 0.05f)
            {
                gas = 1f;
            }

            // Kunci mobil saat fase COUNTDOWN atau FINISHED
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

            float speedCapMs = (maxSpeedKmh / 3.6f) * (IsNitroActive ? nitroSpeedMultiplier : 1f);
            float accelRate = acceleration * (IsNitroActive ? nitroAccelMultiplier : 1f);

            // 1. Tenaga Mesin / Rem / Mundur
            if (brake)
            {
                if (forwardVelocity > 1.2f)
                {
                    float bScale = handbrake ? 0.45f : 1.0f;
                    Vector3 brakeForce = -transform.forward * (brakeStrength * bScale * _rb.mass);
                    _rb.AddForce(brakeForce, ForceMode.Force);
                }
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
                float speedRatio = Mathf.Clamp01(forwardVelocity / speedCapMs);
                float effectiveAccel = accelRate * (1f - speedRatio * 0.45f);

                if (forwardVelocity < speedCapMs)
                {
                    _rb.AddForce(transform.forward * (gas * effectiveAccel * _rb.mass), ForceMode.Force);
                }
            }
            else if (gas < -0.05f && canDrive)
            {
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

                float speedFactor = Mathf.Clamp(1.2f - (currentSpeedMs / (maxSpeedKmh / 3.6f)) * 0.35f, 0.72f, 1.25f);
                float turnAngle = _currentSteer * steerAngle * turnDir * speedFactor * Time.fixedDeltaTime;

                Quaternion deltaRot = Quaternion.Euler(0f, turnAngle, 0f);
                _rb.MoveRotation(_rb.rotation * deltaRot);
            }

            // 3. Cengkeraman Samping (Lateral Grip)
            Vector3 localVel = transform.InverseTransformDirection(_rb.linearVelocity);
            float gripSpeed = IsDrifting ? driftLateralGrip : normalLateralGrip;
            localVel.x = Mathf.MoveTowards(localVel.x, 0f, gripSpeed * Time.fixedDeltaTime * 15f);
            _rb.linearVelocity = transform.TransformDirection(localVel);

            // 4. Downforce
            _rb.AddForce(-Vector3.up * (downforce * currentSpeedMs * _rb.mass * 0.04f), ForceMode.Force);

            // Update Efek Partikel Asap & Api Ban
            UpdateEnhancedParticleEmissions();
        }

        private void OnCollisionStay(Collision collision)
        {
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

        private void UpdateEnhancedParticleEmissions()
        {
            // Tingkat kepulan asap pada roda kiri & kanan
            float smokeRate = 0f;
            float sparksRate = 0f;

            if (IsDrifting)
            {
                smokeRate = 55f;
                sparksRate = 30f;
            }
            else if (IsNitroActive)
            {
                smokeRate = 20f;
            }
            // Burnout saat start
            else if (CurrentSpeedKmh < 15f && CarInputManager.Instance != null && CarInputManager.Instance.Throttle > 0.8f)
            {
                smokeRate = 35f;
                sparksRate = 12f;
            }

            // Asap Roda Kiri
            if (_driftSmokeL != null)
            {
                var emL = _driftSmokeL.emission;
                emL.rateOverTime = smokeRate;
            }

            // Asap Roda Kanan
            if (_driftSmokeR != null)
            {
                var emR = _driftSmokeR.emission;
                emR.rateOverTime = smokeRate;
            }

            // Percikan Api Ban Kiri & Kanan
            if (_tireSparksL != null)
            {
                var emSpkL = _tireSparksL.emission;
                emSpkL.rateOverTime = sparksRate;
            }
            if (_tireSparksR != null)
            {
                var emSpkR = _tireSparksR.emission;
                emSpkR.rateOverTime = sparksRate;
            }

            // Api Nitro
            if (_nitroFlameL != null && _nitroFlameR != null)
            {
                var emL = _nitroFlameL.emission;
                var emR = _nitroFlameR.emission;
                float rate = IsNitroActive ? 65f : 0f;
                emL.rateOverTime = rate;
                emR.rateOverTime = rate;
            }

            // Cahaya Nitro
            if (_nitroLight != null)
            {
                _nitroLight.intensity = IsNitroActive ? (3.2f + Mathf.PingPong(Time.time * 30f, 1.2f)) : 0f;
            }
        }

        private void CheckTrackSafety()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 3.5f))
            {
                RecordSafePosition();
            }

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

            float ratio = Mathf.Max(SpeedRatio, gasInput * 0.45f);
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);
        }

        /// <summary>
        /// Pengaturan Efek Visual Partikel Asap Volumetrik & Api Ban Kelas AAA:
        /// - Tekstur gumpalan awan berlapis organik lembut (bukan lingkaran kaku).
        /// - Mengembang dari ukuran kecil di ban (0.35m) menjadi kabut besar (3.0m).
        /// - Rotasi acak alami dan gradien pemudaran lembut tanpa pop-in.
        /// - Percikan api listrik neon di permukaan aspal saat ban tergelincir.
        /// </summary>
        private void SetupEnhancedVisualEffects()
        {
            if (_sharedVolumetricSmokeTex == null)
            {
                _sharedVolumetricSmokeTex = CreateVolumetricCloudTexture(128);
            }

            if (_sharedSparkTex == null)
            {
                _sharedSparkTex = CreateSparkTexture(32);
            }

            var unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");

            // Material Asap Volumetrik Lembut
            var smokeMat = new Material(unlitShader) { mainTexture = _sharedVolumetricSmokeTex };
            smokeMat.SetColor("_BaseColor", new Color(0.65f, 0.85f, 1.0f, 0.55f));

            // Material Percikan Api Ban
            var sparkMat = new Material(unlitShader) { mainTexture = _sharedSparkTex };
            sparkMat.SetColor("_BaseColor", new Color(0.2f, 0.95f, 1.0f, 1.0f));

            // 1. Asap Roda Belakang Kiri
            Vector3 posL = new Vector3(-0.75f, 0.08f, -1.35f);
            _driftSmokeL = CreateWheelSmokeSystem("Smoke_Rear_L", posL, smokeMat);
            _tireSparksL = CreateWheelSparksSystem("Sparks_Rear_L", posL, sparkMat);

            // 2. Asap Roda Belakang Kanan
            Vector3 posR = new Vector3(0.75f, 0.08f, -1.35f);
            _driftSmokeR = CreateWheelSmokeSystem("Smoke_Rear_R", posR, smokeMat);
            _tireSparksR = CreateWheelSparksSystem("Sparks_Rear_R", posR, sparkMat);

            // 3. Api Knalpot Nitro (Supersonic Jet Flames)
            var flameMat = new Material(unlitShader) { mainTexture = _sharedVolumetricSmokeTex };
            _nitroFlameL = CreateSupersonicExhaust("Flame_L", new Vector3(-0.52f, 0.35f, -2.15f), flameMat);
            _nitroFlameR = CreateSupersonicExhaust("Flame_R", new Vector3(0.52f, 0.35f, -2.15f), flameMat);

            // 4. Point Light Nitro
            var lightObj = new GameObject("NitroLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.38f, -2.3f);
            _nitroLight = lightObj.AddComponent<Light>();
            _nitroLight.type = LightType.Point;
            _nitroLight.range = 6.0f;
            _nitroLight.color = new Color(0f, 0.95f, 1f);
            _nitroLight.intensity = 0f;
            _nitroLight.shadows = LightShadows.None;
        }

        private ParticleSystem CreateWheelSmokeSystem(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;

            var ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 80;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.6f, 0.85f, 1f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.06f; // Asap melayang naik perlahan

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            // Kurva Pembesaran Ukuran Sepanjang Hidup (Billowing Expansion)
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.2f, 1.4f);
            sizeCurve.AddKey(1.0f, 4.2f); // Mengembang hingga ~3 meter di belakang mobil!
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Kurva Warna & Transparansi Halus (Tanpa pop-in)
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.1f, 0.9f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.85f, 0.95f), 0.35f),
                    new GradientColorKey(new Color(0.4f, 0.45f, 0.55f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(0.65f, 0.12f),
                    new GradientAlphaKey(0.35f, 0.55f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            colorOverLife.color = grad;

            // Rotasi Dinamis Berputar
            var rotOverLife = ps.rotationOverLifetime;
            rotOverLife.enabled = true;
            rotOverLife.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
                rend.renderMode = ParticleSystemRenderMode.Billboard;
            }

            return ps;
        }

        private ParticleSystem CreateWheelSparksSystem(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;

            var ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 50;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new Color(0.2f, 0.95f, 1.0f, 1.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.1f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.sharedMaterial = mat;

            return ps;
        }

        private ParticleSystem CreateSupersonicExhaust(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 60;
            main.startLifetime = 0.18f;
            main.startSpeed = 12.0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = new Color(0f, 0.95f, 1f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.06f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sc = new AnimationCurve();
            sc.AddKey(0f, 1.0f);
            sc.AddKey(1f, 0.1f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sc);

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.sharedMaterial = mat;

            return ps;
        }

        /// <summary>
        /// Membuat Tekstur Gumpalan Awan Volumetrik Berbasis Multi-Lobe & Fractal Noise:
        /// Menghasilkan kepulan asap yang realistis, organik, berserat halus, dan bebas sudut tajam.
        /// </summary>
        private static Texture2D CreateVolumetricCloudTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;

            // 4 Pusat sub-puff untuk bentuk awan organik berlapis
            Vector2[] lobes = new Vector2[]
            {
                new Vector2(center, center),
                new Vector2(center - size * 0.14f, center + size * 0.10f),
                new Vector2(center + size * 0.15f, center - size * 0.08f),
                new Vector2(center + size * 0.06f, center + size * 0.16f),
                new Vector2(center - size * 0.10f, center - size * 0.12f)
            };
            float[] lobeRadii = new float[] { size * 0.44f, size * 0.32f, size * 0.34f, size * 0.30f, size * 0.28f };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x, y);
                    float density = 0f;

                    for (int i = 0; i < lobes.Length; i++)
                    {
                        float dist = Vector2.Distance(pt, lobes[i]);
                        float r = lobeRadii[i];
                        if (dist < r)
                        {
                            float falloff = 1f - (dist / r);
                            density += falloff * falloff;
                        }
                    }

                    // Tambahkan noise fractal lembut di tepian
                    float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.35f;
                    density = Mathf.Clamp01(density * 0.85f + noise);

                    // Fade tepi lingkaran luar total
                    float centerDist = Vector2.Distance(pt, new Vector2(center, center));
                    float boundary = Mathf.Clamp01(1f - (centerDist / (size * 0.48f)));
                    density *= boundary * boundary;

                    float alpha = Mathf.Clamp01(density);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSparkTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / center;
                    float dy = Mathf.Abs(y - center) / center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d * 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
