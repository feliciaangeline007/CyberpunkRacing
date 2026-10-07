using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkRacing
{
    /// <summary>
    /// Input terpusat untuk Mobil Cyberpunk:
    /// Mendukung Keyboard (PC), Tombol Layar Sentuh (Mobile), dan Sensor Kemiringan (Tilt Gyroscope).
    /// </summary>
    public class CarInputManager : MonoBehaviour
    {
        public static CarInputManager Instance { get; private set; }

        [Header("Pengaturan Tilt Steering")]
        public bool useTiltSteering = false;
        public float tiltSensitivity = 28f;
        public bool invertTilt = false;

        // Nilai input aktif (-1 s.d. 1)
        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Brake { get; private set; }
        public bool Nitro { get; private set; }
        public bool Handbrake { get; private set; }

        // Tombol Touch On-Screen (diatur dari HUD)
        public static float TouchThrottleInput = 0f;
        public static float TouchSteerInput = 0f;
        public static bool TouchBrakeHeld = false;
        public static bool TouchNitroHeld = false;

        private float _tiltBiasAngle = 0f;
        private bool _tiltCalibrated = false;
        private float _smoothedTiltSteer = 0f;

        private void Awake()
        {
            Instance = this;
            useTiltSteering = PlayerPrefs.GetInt("UseTiltSteering", Application.isMobilePlatform ? 1 : 0) == 1;
        }

        private void Update()
        {
            float keyboardGas = 0f;
            float keyboardSteer = 0f;
            bool keyboardBrake = false;
            bool keyboardNitro = false;
            bool keyboardHandbrake = false;

            // 1. Keyboard Input
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keyboardGas += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keyboardGas -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keyboardSteer -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keyboardSteer += 1f;
                if (kb.spaceKey.isPressed) keyboardHandbrake = true;
                if (kb.leftShiftKey.isPressed || kb.nKey.isPressed) keyboardNitro = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            keyboardGas = Input.GetAxisRaw("Vertical");
            keyboardSteer = Input.GetAxisRaw("Horizontal");
            keyboardHandbrake = Input.GetKey(KeyCode.Space);
            keyboardNitro = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.N);
#endif

            // 2. Gabungkan Touch On-Screen
            float finalGas = keyboardGas;
            if (Mathf.Abs(TouchThrottleInput) > 0.01f)
            {
                finalGas = TouchThrottleInput;
            }

            float finalSteer = keyboardSteer;
            if (Mathf.Abs(TouchSteerInput) > 0.01f)
            {
                finalSteer = TouchSteerInput;
            }

            // 3. Sensor Kemiringan (Tilt Gyro) jika diaktifkan
            if (useTiltSteering && Mathf.Abs(keyboardSteer) < 0.05f && Mathf.Abs(TouchSteerInput) < 0.05f)
            {
                finalSteer = ReadTiltSteer();
            }

            bool finalBrake = keyboardBrake || TouchBrakeHeld;
            if (finalGas < -0.1f) finalBrake = true;

            bool finalNitro = keyboardNitro || TouchNitroHeld;
            bool finalHandbrake = keyboardHandbrake || (finalBrake && Mathf.Abs(finalSteer) > 0.3f);

            Throttle = Mathf.Clamp(finalGas, -1f, 1f);
            Steer = Mathf.Clamp(finalSteer, -1f, 1f);
            Brake = finalBrake;
            Nitro = finalNitro;
            Handbrake = finalHandbrake;
        }

        private float ReadTiltSteer()
        {
            Vector3 accel = GetRawAcceleration();
            Vector2 inPlane = new Vector2(accel.x, accel.y);

            // Jika HP hampir datar (sqrMagnitude < 0.0625f = 0.25^2), abaikan noise
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
