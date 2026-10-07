using UnityEngine;

namespace CyberpunkRacing
{
    /// <summary>
    /// Pengendali Mobil Balap Cyberpunk Berkecepatan Tinggi (Arcade):
    /// - Fisika arcade responsif dan stabil, anti-terbalik.
    /// - Kontrol lengkap: gas/rem/belok/drift/nitro/reset.
    /// - Efek visual partikel asap & api ban (dibuat secara prosedural).
    /// - Sinkronisasi warna cat dari Main Menu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Performa & Mesin")]
        public float acceleration    = 26f;
        public float maxSpeedKmh     = 160f;
        public float maxReverseKmh   = 40f;
        public float steerAngle      = 40f;
        public float steerSpeed      = 12f;
        public float brakeStrength   = 36f;
        public float downforce       = 22f;
        public bool  autoThrottle    = true;

        [Header("Sistem Nitro")]
        public float maxNitro             = 100f;
        public float currentNitro         = 100f;
        public float nitroSpeedMultiplier = 1.45f;
        public float nitroAccelMultiplier = 1.85f;
        public float nitroDrainRate       = 30f;
        public float nitroRegenRate       = 12f;

        [Header("Drift & Handling")]
        public float driftSteerMultiplier = 1.65f;
        public float driftLateralGrip     = 3.5f;
        public float normalLateralGrip    = 18.0f;

        [Header("Tabrakan & Pemulihan")]
        public float crashSpeedLossMin      = 0.35f;
        public float crashSpeedLossMax      = 0.65f;
        public float wallScrapeFriction     = 5.5f;
        public float crashRecoveryDuration  = 0.45f;
        private float _crashCooldown        = 0f;

        [Header("Visual Roda & Bodi")]
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;
        public Transform carBodyVisual;
        public Renderer  carBodyRenderer;

        [Header("Audio")]
        public AudioSource engineAudio;
        public float minPitch = 0.85f;
        public float maxPitch = 2.45f;

        [Header("Keselamatan Lintasan")]
        public float fallRespawnY = -8f;

        // Properti publik untuk HUD & Kamera
        public float CurrentSpeedKmh => _rb != null ? _rb.linearVelocity.magnitude * 3.6f : 0f;
        public float SpeedRatio      => Mathf.Clamp01(CurrentSpeedKmh / maxSpeedKmh);
        public float NitroRatio      => maxNitro > 0f ? Mathf.Clamp01(currentNitro / maxNitro) : 0f;
        public bool  IsNitroActive   { get; private set; }
        public bool  IsDrifting      { get; private set; }

        private Rigidbody _rb;
        private float _currentSteer   = 0f;
        private float _wheelSpinAngle = 0f;

        // Partikel efek visual
        private ParticleSystem _driftSmokeL;
        private ParticleSystem _driftSmokeR;
        private ParticleSystem _tireSparksL;
        private ParticleSystem _tireSparksR;
        private ParticleSystem _nitroFlameL;
        private ParticleSystem _nitroFlameR;
        private Light          _nitroLight;

        // Respawn aman
        private Vector3    _lastSafePosition;
        private Quaternion _lastSafeRotation;

        // Tekstur partikel berbagi antar instance
        private static Texture2D _sharedSmokeTex;
        private static Texture2D _sharedSparkTex;

        private static readonly Color[] PaintPalette = {
            new Color(0f,    0.9f, 1f),
            new Color(1f,    0.1f, 0.8f),
            new Color(1f,    0.8f, 0.1f),
            new Color(0.12f, 0.14f, 0.18f)
        };

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass                   = 1200f;
            _rb.centerOfMass           = new Vector3(0f, -0.45f, 0.05f);
            _rb.interpolation          = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.constraints            = RigidbodyConstraints.FreezeRotationX
                                       | RigidbodyConstraints.FreezeRotationZ;

            RecordSafePosition();

            // Bungkus setup partikel dalam try-catch agar jika gagal tidak crash game
            try { SetupVisualEffects(); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CarController] Setup efek visual gagal (tidak kritis): {e.Message}");
            }
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

        private void Update()
        {
            if (CarInputManager.Instance != null && CarInputManager.Instance.ResetRequested)
                RespawnToSafePosition();

            UpdateAudio();
            AnimateWheelsAndBody();
        }

        private void FixedUpdate()
        {
            ApplyMovementPhysics();
            CheckTrackSafety();
        }

        // ── Warna Cat ──────────────────────────────────────────────────────────

        private void ApplySavedCarPaint()
        {
            int   colorIdx    = PlayerPrefs.GetInt("SelectedCarColor", 0);
            Color chosenColor = PaintPalette[Mathf.Clamp(colorIdx, 0, PaintPalette.Length - 1)];

            if (carBodyRenderer == null && carBodyVisual != null)
                carBodyRenderer = carBodyVisual.GetComponentInChildren<Renderer>();

            if (carBodyRenderer != null)
            {
                var mat = carBodyRenderer.material;
                if (mat != null)
                {
                    mat.color = chosenColor;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", chosenColor);
                }
            }

            var underglow = transform.Find("NeonUnderglow");
            if (underglow != null)
            {
                var ugRend = underglow.GetComponent<Renderer>();
                if (ugRend != null && ugRend.material != null)
                {
                    ugRend.material.color = chosenColor;
                    if (ugRend.material.HasProperty("_EmissionColor"))
                        ugRend.material.SetColor("_EmissionColor", chosenColor * 2.5f);
                }
            }
        }

        // ── Fisika Gerakan ─────────────────────────────────────────────────────

        private void ApplyMovementPhysics()
        {
            var   input       = CarInputManager.Instance;
            float gas         = input != null ? input.Throttle  : 0f;
            float steer       = input != null ? input.Steer     : 0f;
            bool  brake       = input != null && input.Brake;
            bool  nitroWanted = input != null && input.Nitro;
            bool  handbrake   = input != null && input.Handbrake;

            // Kunci mobil saat Countdown / Finished
            var  gm       = RacingGameManager.Instance;
            bool canDrive = gm == null || gm.State == GameState.Racing;

            if (!canDrive)
            {
                gas         = 0f;
                brake       = true;
                nitroWanted = false;
            }
            else if (autoThrottle && !brake && gas >= -0.05f)
            {
                gas = 1f;
            }

            // Sistem nitro
            IsNitroActive = canDrive && nitroWanted && currentNitro > 2f && gas > 0.05f && !brake;
            currentNitro = IsNitroActive
                ? Mathf.Max(0f, currentNitro - nitroDrainRate * Time.fixedDeltaTime)
                : Mathf.Min(maxNitro, currentNitro + nitroRegenRate * Time.fixedDeltaTime);

            Vector3 vel          = _rb.linearVelocity;
            float   speedMs      = vel.magnitude;
            float   fwdVel       = Vector3.Dot(vel, transform.forward);
            float   speedCapMs   = (maxSpeedKmh / 3.6f) * (IsNitroActive ? nitroSpeedMultiplier : 1f);
            float   accelRate    = acceleration           * (IsNitroActive ? nitroAccelMultiplier : 1f);

            // Tenaga / Rem / Mundur
            if (brake)
            {
                if (fwdVel > 1.2f)
                {
                    float bScale = handbrake ? 0.45f : 1.0f;
                    _rb.AddForce(-transform.forward * (brakeStrength * bScale * _rb.mass), ForceMode.Force);
                }
                else if (canDrive && gas < -0.1f)
                {
                    float revCap = maxReverseKmh / 3.6f;
                    if (fwdVel > -revCap)
                        _rb.AddForce(-transform.forward * (acceleration * 0.7f * _rb.mass), ForceMode.Force);
                }
            }
            else if (gas > 0.05f && canDrive)
            {
                if (_crashCooldown > 0f) _crashCooldown -= Time.fixedDeltaTime;

                float speedRatio    = Mathf.Clamp01(fwdVel / speedCapMs);
                float crashPenalty  = _crashCooldown > 0f ? 0.35f : 1f;
                float effectAccel   = accelRate * (1f - speedRatio * 0.45f) * crashPenalty;

                if (fwdVel < speedCapMs)
                    _rb.AddForce(transform.forward * (gas * effectAccel * _rb.mass), ForceMode.Force);
            }
            else if (gas < -0.05f && canDrive)
            {
                if (fwdVel > 0.8f)
                {
                    _rb.AddForce(-transform.forward * (brakeStrength * _rb.mass), ForceMode.Force);
                }
                else
                {
                    float revCap = maxReverseKmh / 3.6f;
                    if (fwdVel > -revCap)
                        _rb.AddForce(transform.forward * (gas * acceleration * 0.65f * _rb.mass), ForceMode.Force);
                }
            }
            else
            {
                // Natural deceleration saat tidak ada input
                Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                Vector3 damped  = Vector3.MoveTowards(flatVel, Vector3.zero, 3.2f * Time.fixedDeltaTime);
                _rb.linearVelocity = new Vector3(damped.x, _rb.linearVelocity.y, damped.z);
            }

            // Kemudi
            IsDrifting   = (handbrake || (brake && Mathf.Abs(steer) > 0.15f)) && speedMs > 5.5f;
            float steerMult = IsDrifting ? driftSteerMultiplier : 1.0f;
            _currentSteer   = Mathf.Lerp(_currentSteer, steer * steerMult, steerSpeed * Time.fixedDeltaTime);

            if (speedMs > 0.25f)
            {
                float turnDir     = Mathf.Sign(fwdVel);
                if (Mathf.Abs(fwdVel) < 0.2f) turnDir = 1f;

                float speedFactor = Mathf.Clamp(1.2f - (speedMs / (maxSpeedKmh / 3.6f)) * 0.35f, 0.72f, 1.25f);
                float turnAngle   = _currentSteer * steerAngle * turnDir * speedFactor * Time.fixedDeltaTime;
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turnAngle, 0f));
            }

            // Lateral grip
            Vector3 localVel  = transform.InverseTransformDirection(_rb.linearVelocity);
            float   gripSpeed = IsDrifting ? driftLateralGrip : normalLateralGrip;
            localVel.x        = Mathf.MoveTowards(localVel.x, 0f, gripSpeed * Time.fixedDeltaTime * 15f);
            _rb.linearVelocity= transform.TransformDirection(localVel);

            // Downforce
            _rb.AddForce(-Vector3.up * (downforce * speedMs * _rb.mass * 0.04f), ForceMode.Force);

            UpdateParticleEmissions();
        }

        // ── Tabrakan ───────────────────────────────────────────────────────────

        private void OnCollisionEnter(Collision collision)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < 3.0f) return;

            ContactPoint contact     = collision.GetContact(0);
            float        frontalFactor = Mathf.Abs(Vector3.Dot(contact.normal, transform.forward));
            float        speedLoss   = Mathf.Lerp(crashSpeedLossMin, crashSpeedLossMax, frontalFactor);

            _rb.linearVelocity *= (1f - speedLoss);

            if (IsNitroActive)
                currentNitro = Mathf.Max(0f, currentNitro - 15f);

            _crashCooldown = crashRecoveryDuration;

            float shakeAmt = Mathf.Clamp01(impactSpeed / 24f) * 0.45f;
            RacingCamera.Instance?.TriggerImpactShake(shakeAmt);
            CyberSoundManager.Instance?.PlayCrashSound();
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
                        wallTangent = -wallTangent;

                    float fwdSpd = Vector3.Dot(_rb.linearVelocity, wallTangent);
                    if (fwdSpd > 0.5f)
                    {
                        float drag = Mathf.Clamp01(1f - wallScrapeFriction * Time.fixedDeltaTime);
                        _rb.linearVelocity = wallTangent * (fwdSpd * drag)
                                           + Vector3.up * _rb.linearVelocity.y;
                    }
                    break;
                }
            }
        }

        // ── Keselamatan Lintasan ───────────────────────────────────────────────

        private void CheckTrackSafety()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 3.5f))
                RecordSafePosition();

            if (transform.position.y < fallRespawnY)
                RespawnToSafePosition();
        }

        public void RecordSafePosition()
        {
            _lastSafePosition = transform.position;
            _lastSafeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        public void RespawnToSafePosition()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity= Vector3.zero;
            transform.SetPositionAndRotation(
                _lastSafePosition + Vector3.up * 0.6f, _lastSafeRotation);
            _currentSteer = 0f;
            _rb.AddForce(transform.forward * 5f, ForceMode.VelocityChange);
        }

        public void AddNitro(float amount)
        {
            currentNitro = Mathf.Clamp(currentNitro + amount, 0f, maxNitro);
        }

        // ── Animasi Roda & Bodi ────────────────────────────────────────────────

        private void AnimateWheelsAndBody()
        {
            float speed         = _rb != null ? _rb.linearVelocity.magnitude : 0f;
            _wheelSpinAngle    += speed * 360f * Time.deltaTime / (2f * Mathf.PI * 0.38f);
            float frontSteerYaw = _currentSteer * 28f;

            if (frontLeftWheel  != null) frontLeftWheel.localRotation  = Quaternion.Euler(_wheelSpinAngle, frontSteerYaw, 0f);
            if (frontRightWheel != null) frontRightWheel.localRotation = Quaternion.Euler(_wheelSpinAngle, frontSteerYaw, 0f);
            if (rearLeftWheel   != null) rearLeftWheel.localRotation   = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);
            if (rearRightWheel  != null) rearRightWheel.localRotation  = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);

            if (carBodyVisual != null)
            {
                float roll = -_currentSteer * Mathf.Clamp01(speed / 12f) * 4.2f;
                carBodyVisual.localRotation = Quaternion.Euler(0f, 0f, roll);
            }
        }

        private void UpdateAudio()
        {
            if (engineAudio == null) return;
            var   input    = CarInputManager.Instance;
            float gasInput = input != null ? Mathf.Abs(input.Throttle) : 0f;
            float ratio    = Mathf.Max(SpeedRatio, gasInput * 0.45f);
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);
        }

        // ── Efek Visual Partikel ───────────────────────────────────────────────

        private void UpdateParticleEmissions()
        {
            float smokeRate  = 0f;
            float sparksRate = 0f;

            if (IsDrifting)
            {
                smokeRate  = 55f;
                sparksRate = 30f;
            }
            else if (IsNitroActive)
            {
                smokeRate = 20f;
            }
            else if (CurrentSpeedKmh < 15f
                && CarInputManager.Instance != null
                && CarInputManager.Instance.Throttle > 0.8f)
            {
                smokeRate  = 35f;
                sparksRate = 12f;
            }

            SetEmission(_driftSmokeL,  smokeRate);
            SetEmission(_driftSmokeR,  smokeRate);
            SetEmission(_tireSparksL,  sparksRate);
            SetEmission(_tireSparksR,  sparksRate);

            float flameRate = IsNitroActive ? 65f : 0f;
            SetEmission(_nitroFlameL, flameRate);
            SetEmission(_nitroFlameR, flameRate);

            if (_nitroLight != null)
                _nitroLight.intensity = IsNitroActive
                    ? (3.2f + Mathf.PingPong(Time.time * 30f, 1.2f))
                    : 0f;
        }

        private static void SetEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = rate;
        }

        private void SetupVisualEffects()
        {
            if (_sharedSmokeTex == null) _sharedSmokeTex = CreateCloudTexture(128);
            if (_sharedSparkTex == null) _sharedSparkTex = CreateSparkTexture(32);

            // Cari shader yang tersedia, urut dari yang paling diinginkan
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                              ?? Shader.Find("Particles/Standard Unlit")
                              ?? Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Sprites/Default")
                              ?? Shader.Find("Hidden/InternalErrorShader");

            if (unlitShader == null)
            {
                Debug.LogWarning("[CarController] Tidak ada shader partikel yang ditemukan — efek visual dilewati.");
                return;
            }

            var smokeMat = CreateParticleMaterial(unlitShader, _sharedSmokeTex, new Color(0.65f, 0.85f, 1.0f, 0.55f));
            var sparkMat = CreateParticleMaterial(unlitShader, _sharedSparkTex, new Color(0.2f,  0.95f, 1.0f, 1.0f));
            var flameMat = CreateParticleMaterial(unlitShader, _sharedSmokeTex, new Color(0f,    0.95f, 1.0f, 0.95f));

            Vector3 posL = new Vector3(-0.75f, 0.08f, -1.35f);
            Vector3 posR = new Vector3( 0.75f, 0.08f, -1.35f);

            _driftSmokeL = CreateSmokeSystem("Smoke_L", posL, smokeMat);
            _driftSmokeR = CreateSmokeSystem("Smoke_R", posR, smokeMat);
            _tireSparksL = CreateSparkSystem("Sparks_L", posL, sparkMat);
            _tireSparksR = CreateSparkSystem("Sparks_R", posR, sparkMat);
            _nitroFlameL = CreateFlameSystem("Flame_L", new Vector3(-0.52f, 0.35f, -2.15f), flameMat);
            _nitroFlameR = CreateFlameSystem("Flame_R", new Vector3( 0.52f, 0.35f, -2.15f), flameMat);

            var lightObj = new GameObject("NitroLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.38f, -2.3f);
            _nitroLight           = lightObj.AddComponent<Light>();
            _nitroLight.type      = LightType.Point;
            _nitroLight.range     = 6.0f;
            _nitroLight.color     = new Color(0f, 0.95f, 1f);
            _nitroLight.intensity = 0f;
            _nitroLight.shadows   = LightShadows.None;
        }

        private static Material CreateParticleMaterial(Shader shader, Texture2D tex, Color color)
        {
            var mat = new Material(shader);
            mat.mainTexture = tex;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            return mat;
        }

        private ParticleSystem CreateSmokeSystem(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;

            var ps   = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop             = true;
            main.playOnAwake      = true;
            main.maxParticles     = 80;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
            main.startRotation    = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor       = new Color(0.6f, 0.85f, 1f, 0.45f);
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.gravityModifier  = -0.06f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.22f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1.4f), new Keyframe(1f, 4.2f));
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOL = ps.colorOverLifetime;
            colorOL.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.1f, 0.9f, 1f),  0f),
                    new GradientColorKey(new Color(0.7f, 0.85f, 0.95f), 0.35f),
                    new GradientColorKey(new Color(0.4f, 0.45f, 0.55f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.65f, 0.12f),
                    new GradientAlphaKey(0.35f, 0.55f),
                    new GradientAlphaKey(0f,    1f)
                });
            colorOL.color = grad;

            var rotOL = ps.rotationOverLifetime;
            rotOL.enabled = true;
            rotOL.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) { rend.sharedMaterial = mat; rend.renderMode = ParticleSystemRenderMode.Billboard; }

            return ps;
        }

        private ParticleSystem CreateSparkSystem(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;

            var ps   = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop            = true;
            main.playOnAwake     = true;
            main.maxParticles    = 50;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor      = new Color(0.2f, 0.95f, 1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 35f;
            shape.radius    = 0.1f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.sharedMaterial = mat;

            return ps;
        }

        private ParticleSystem CreateFlameSystem(string name, Vector3 localPos, Material mat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var ps   = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop            = true;
            main.playOnAwake     = true;
            main.maxParticles    = 60;
            main.startLifetime   = 0.18f;
            main.startSpeed      = 12f;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor      = new Color(0f, 0.95f, 1f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 6f;
            shape.radius    = 0.06f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.1f)));

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.sharedMaterial = mat;

            return ps;
        }

        // ── Pembuatan Tekstur Prosedural ───────────────────────────────────────

        private static Texture2D CreateCloudTexture(int size)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float ctr  = size * 0.5f;
            var lobes  = new Vector2[] {
                new Vector2(ctr, ctr),
                new Vector2(ctr - size * 0.14f, ctr + size * 0.10f),
                new Vector2(ctr + size * 0.15f, ctr - size * 0.08f),
                new Vector2(ctr + size * 0.06f, ctr + size * 0.16f),
                new Vector2(ctr - size * 0.10f, ctr - size * 0.12f)
            };
            float[] radii = { size * 0.44f, size * 0.32f, size * 0.34f, size * 0.30f, size * 0.28f };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var   pt      = new Vector2(x, y);
                    float density = 0f;
                    for (int i = 0; i < lobes.Length; i++)
                    {
                        float dist = Vector2.Distance(pt, lobes[i]);
                        if (dist < radii[i])
                        {
                            float f = 1f - dist / radii[i];
                            density += f * f;
                        }
                    }
                    float noise    = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.35f;
                    density        = Mathf.Clamp01(density * 0.85f + noise);
                    float boundary = Mathf.Clamp01(1f - Vector2.Distance(pt, new Vector2(ctr, ctr)) / (size * 0.48f));
                    density       *= boundary * boundary;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(density)));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSparkTexture(int size)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float ctr  = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx    = Mathf.Abs(x - ctr) / ctr;
                    float dy    = Mathf.Abs(y - ctr) / ctr;
                    float d     = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d * 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
