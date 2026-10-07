using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkRacing
{
    /// <summary>
    /// Input terpusat untuk Mobil Cyberpunk:
    /// - Keyboard PC (W/A/S/D atau Panah, Space untuk Drift, Shift/N untuk Nitro, R untuk Reset)
    /// - Gamepad / Stik Konsol (Triggers untuk Gas/Rem, Stick untuk Belok, Tombol Drift/Nitro)
    /// - Tombol Layar Sentuh Mobile (Pedal Gas, Rem, Nitro, Kemudi Kiri/Kanan, Reset)
    /// - Sensor Kemiringan Opsional (Gyro Tilt Steering)
    /// </summary>
    public class CarInputManager : MonoBehaviour
    {
        public static CarInputManager Instance { get; private set; }

        [Header("Pengaturan Tilt Steering")]
        public bool useTiltSteering = false;
        public float tiltSensitivity = 28f;
        public bool invertTilt = false;

        // Nilai input aktif
        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Brake { get; private set; }
        public bool Nitro { get; private set; }
        public bool Handbrake { get; private set; }
        public bool ResetRequested { get; private set; }

        // Tombol Touch On-Screen (diatur dari HUD)
        public static float TouchThrottleInput = 0f;
        public static float TouchSteerInput = 0f;
        public static bool TouchBrakeHeld = false;
        public static bool TouchNitroHeld = false;
        public static bool TouchResetTriggered = false;

        private float _tiltBiasAngle = 0f;
        private bool _tiltCalibrated = false;
        private float _smoothedTiltSteer = 0f;

        private void Awake()
        {
            Instance = this;
            useTiltSteering = PlayerPrefs.GetInt("UseTiltSteering", 0) == 1;
        }

        private void Update()
        {
            float hardwareGas = 0f;
            float hardwareSteer = 0f;
            bool hardwareBrake = false;
            bool hardwareNitro = false;
            bool hardwareHandbrake = false;
            bool hardwareReset = false;

            // 1. New Input System (Keyboard + Gamepad)
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) hardwareGas += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                {
                    hardwareGas -= 1f;
                    hardwareBrake = true;
                }
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) hardwareSteer -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) hardwareSteer += 1f;
                if (kb.spaceKey.isPressed) hardwareHandbrake = true;
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed || kb.nKey.isPressed) hardwareNitro = true;
                if (kb.rKey.wasPressedThisFrame) hardwareReset = true;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                float triggerGas = gp.rightTrigger.ReadValue();
                float triggerBrake = gp.leftTrigger.ReadValue();
                if (triggerGas > 0.05f) hardwareGas += triggerGas;
                if (triggerBrake > 0.05f)
                {
                    hardwareGas -= triggerBrake;
                    hardwareBrake = true;
                }

                float stickX = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.1f) hardwareSteer += stickX;
                if (gp.dpad.left.isPressed) hardwareSteer -= 1f;
                if (gp.dpad.right.isPressed) hardwareSteer += 1f;

                if (gp.buttonSouth.isPressed) hardwareHandbrake = true;
                if (gp.buttonWest.isPressed || gp.rightShoulder.isPressed) hardwareNitro = true;
                if (gp.selectButton.wasPressedThisFrame) hardwareReset = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                hardwareGas = Input.GetAxisRaw("Vertical");
                hardwareSteer = Input.GetAxisRaw("Horizontal");
                hardwareHandbrake = Input.GetKey(KeyCode.Space);
                hardwareNitro = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.N);
                if (Input.GetKeyDown(KeyCode.R)) hardwareReset = true;
                if (hardwareGas < -0.1f) hardwareBrake = true;
            }
            catch {}
#endif

            // 2. Gabungkan Touch On-Screen (Mobile Pedals & Buttons)
            float finalGas = hardwareGas;
            if (Mathf.Abs(TouchThrottleInput) > 0.01f)
            {
                finalGas = TouchThrottleInput;
            }

            float finalSteer = hardwareSteer;
            if (Mathf.Abs(TouchSteerInput) > 0.01f)
            {
                finalSteer = TouchSteerInput;
            }

            // 3. Sensor Kemiringan (Tilt Gyro) jika diaktifkan
            if (useTiltSteering && Mathf.Abs(hardwareSteer) < 0.05f && Mathf.Abs(TouchSteerInput) < 0.05f)
            {
                finalSteer = ReadTiltSteer();
            }

            bool finalBrake = hardwareBrake || TouchBrakeHeld;
            if (finalGas < -0.1f) finalBrake = true;

            bool finalNitro = hardwareNitro || TouchNitroHeld;
            bool finalHandbrake = hardwareHandbrake || (finalBrake && Mathf.Abs(finalSteer) > 0.3f);
            bool finalReset = hardwareReset || TouchResetTriggered;

            // Reset flag touch one-shot
            TouchResetTriggered = false;

            Throttle = Mathf.Clamp(finalGas, -1f, 1f);
            Steer = Mathf.Clamp(finalSteer, -1f, 1f);
            Brake = finalBrake;
            Nitro = finalNitro;
            Handbrake = finalHandbrake;
            ResetRequested = finalReset;
        }

        private float ReadTiltSteer()
        {
            Vector3 accel = GetRawAcceleration();
            Vector2 inPlane = new Vector2(accel.x, accel.y);

            if (inPlane.sqrMagnitude < 0.0625f)
            {
                _smoothedTiltSteer = Mathf.Lerp(_smoothedTiltSteer, 0f, 10f * Time.deltaTime);
                return _smoothedTiltSteer;
            }

            float angle = Mathf.Atan2(inPlane.x, inPlane.y) * Mathf.Rad2Deg;
            if (!_tiltCalibrated)
            {
                _tiltBiasAngle = angle;
                _tiltCalibrated = true;
            }

            float delta = Mathf.DeltaAngle(_tiltBiasAngle, angle);
            float target = Mathf.Clamp(delta / Mathf.Max(1f, tiltSensitivity), -1f, 1f);
            if (invertTilt) target = -target;

            _smoothedTiltSteer = Mathf.Lerp(_smoothedTiltSteer, target, 12f * Time.deltaTime);
            return _smoothedTiltSteer;
        }

        public void CalibrateTilt()
        {
            _tiltCalibrated = false;
            _smoothedTiltSteer = 0f;
        }

        private static Vector3 GetRawAcceleration()
        {
#if ENABLE_INPUT_SYSTEM
            var accelerometer = Accelerometer.current;
            if (accelerometer != null)
            {
                if (!accelerometer.enabled)
                {
                    InputSystem.EnableDevice(accelerometer);
                }
                return accelerometer.acceleration.ReadValue();
            }
            return Vector3.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.acceleration;
#else
            return Vector3.zero;
#endif
        }
    }
}
