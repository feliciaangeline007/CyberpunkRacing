using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CyberpunkRacing
{
    /// <summary>
    /// Input terpusat untuk Mobil Cyberpunk:
    /// - Keyboard PC / Gamepad
    /// - Swipe Gesture Steering (default mobile tanpa gyro)
    /// - Gyroscope / Tilt Steering (jika didukung dan diaktifkan)
    /// - Tombol Sentuh On-Screen dari HUD
    /// </summary>
    public class CarInputManager : MonoBehaviour
    {
        public static CarInputManager Instance { get; private set; }

        [Header("Pengaturan Kemudi Mobile")]
        public bool useTiltSteering = false;
        public float tiltSensitivity = 28f;
        public bool invertTilt = false;
        public float swipeSensitivity = 140f;

        // Nilai input aktif (read-only dari luar)
        public float Throttle      { get; private set; }
        public float Steer         { get; private set; }
        public bool  Brake         { get; private set; }
        public bool  Nitro         { get; private set; }
        public bool  Handbrake     { get; private set; }
        public bool  ResetRequested{ get; private set; }

        // Properti status kemudi untuk HUD
        public bool  IsGyroActive      => useTiltSteering && HasGyroscopeSupport();
        public float CurrentSwipeOffset{ get; private set; } = 0f;

        // Input sentuh on-screen — diset dari RacingHUD setiap frame
        public static float TouchThrottleInput  = 0f;
        public static float TouchSteerButtonInput = 0f;
        public static bool  TouchBrakeHeld      = false;
        public static bool  TouchNitroHeld      = false;
        public static bool  TouchResetTriggered = false;

        // State swipe
        private bool    _isSwiping = false;
        private Vector2 _swipeStartPos;
        private float   _currentSwipeSteer = 0f;

        // State tilt/gyro
        private float _tiltBiasAngle    = 0f;
        private bool  _tiltCalibrated   = false;
        private float _smoothedTiltSteer= 0f;

        private void Awake()
        {
            // Singleton guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            bool gyroPref = PlayerPrefs.GetInt("UseTiltSteering", 0) == 1;
            useTiltSteering = gyroPref && HasGyroscopeSupport();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float hardwareGas      = 0f;
            float hardwareSteer    = 0f;
            bool  hardwareBrake    = false;
            bool  hardwareNitro    = false;
            bool  hardwareHandbrake= false;
            bool  hardwareReset    = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    hardwareGas += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  { hardwareGas -= 1f; hardwareBrake = true; }
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  hardwareSteer -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) hardwareSteer += 1f;
                if (kb.spaceKey.isPressed)                           hardwareHandbrake = true;
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed || kb.nKey.isPressed) hardwareNitro = true;
                if (kb.rKey.wasPressedThisFrame)                     hardwareReset = true;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                float triggerGas   = gp.rightTrigger.ReadValue();
                float triggerBrake = gp.leftTrigger.ReadValue();
                if (triggerGas   > 0.05f) hardwareGas += triggerGas;
                if (triggerBrake > 0.05f) { hardwareGas -= triggerBrake; hardwareBrake = true; }

                float stickX = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.1f) hardwareSteer += stickX;
                if (gp.dpad.left.isPressed)  hardwareSteer -= 1f;
                if (gp.dpad.right.isPressed) hardwareSteer += 1f;

                if (gp.buttonSouth.isPressed)                              hardwareHandbrake = true;
                if (gp.buttonWest.isPressed || gp.rightShoulder.isPressed) hardwareNitro     = true;
                if (gp.selectButton.wasPressedThisFrame)                   hardwareReset     = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                hardwareGas      = Input.GetAxisRaw("Vertical");
                hardwareSteer    = Input.GetAxisRaw("Horizontal");
                hardwareHandbrake= Input.GetKey(KeyCode.Space);
                hardwareNitro    = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.N);
                if (Input.GetKeyDown(KeyCode.R)) hardwareReset = true;
                if (hardwareGas < -0.1f) hardwareBrake = true;
            }
            catch { /* Abaikan saat Input belum siap */ }
#endif

            // Kemudi mobile: gyro vs swipe
            float mobileSteer = IsGyroActive
                ? ReadTiltSteer()
                : ProcessSwipeGestureSteering();

            // Tombol kemudi on-screen override swipe
            if (Mathf.Abs(TouchSteerButtonInput) > 0.1f)
                mobileSteer = TouchSteerButtonInput;

            // Hardware (keyboard/gamepad) menang atas mobile
            float finalSteer = Mathf.Abs(hardwareSteer) > 0.05f ? hardwareSteer : mobileSteer;

            // Gas & rem
            float finalGas = hardwareGas;
            if (Mathf.Abs(TouchThrottleInput) > 0.01f)
                finalGas = TouchThrottleInput;

            bool finalBrake     = hardwareBrake || TouchBrakeHeld || (finalGas < -0.1f);
            bool finalNitro     = hardwareNitro  || TouchNitroHeld;
            bool finalHandbrake = hardwareHandbrake || (finalBrake && Mathf.Abs(finalSteer) > 0.3f);
            bool finalReset     = hardwareReset  || TouchResetTriggered;

            TouchResetTriggered = false;

            Throttle      = Mathf.Clamp(finalGas,   -1f, 1f);
            Steer         = Mathf.Clamp(finalSteer, -1f, 1f);
            Brake         = finalBrake;
            Nitro         = finalNitro;
            Handbrake     = finalHandbrake;
            ResetRequested= finalReset;
        }

        private float ProcessSwipeGestureSteering()
        {
            Vector2 touchPos  = Vector2.zero;
            bool    touchDown = false;
            bool    touchHeld = false;
            bool    touchUp   = false;

#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null)
            {
                for (int i = 0; i < ts.touches.Count; i++)
                {
                    var t = ts.touches[i];
                    Vector2 p = t.position.ReadValue();
                    if (p.x < Screen.width * 0.65f && p.y < Screen.height * 0.85f)
                    {
                        touchPos = p;
                        if (t.press.wasPressedThisFrame)  touchDown = true;
                        if (t.isInProgress)                touchHeld = true;
                        if (t.press.wasReleasedThisFrame) touchUp   = true;
                        break;
                    }
                }
            }

            var mouse = Mouse.current;
            if (!touchHeld && mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 mp = mouse.position.ReadValue();
                if (mp.x < Screen.width * 0.65f && mp.y < Screen.height * 0.85f)
                {
                    touchPos  = mp;
                    if (mouse.leftButton.wasPressedThisFrame) touchDown = true;
                    touchHeld = true;
                }
            }
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame) touchUp = true;

#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch t = Input.GetTouch(i);
                        if (t.position.x < Screen.width * 0.65f)
                        {
                            touchPos = t.position;
                            if (t.phase == TouchPhase.Began)    touchDown = true;
                            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) touchHeld = true;
                            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)   touchUp   = true;
                            break;
                        }
                    }
                }
                else if (Input.GetMouseButton(0) && Input.mousePosition.x < Screen.width * 0.65f)
                {
                    touchPos  = Input.mousePosition;
                    if (Input.GetMouseButtonDown(0)) touchDown = true;
                    touchHeld = true;
                }
                if (Input.GetMouseButtonUp(0)) touchUp = true;
            }
            catch { }
#endif

            if (touchDown)
            {
                _isSwiping     = true;
                _swipeStartPos = touchPos;
            }

            if (_isSwiping && touchHeld)
            {
                float deltaX    = touchPos.x - _swipeStartPos.x;
                float target    = Mathf.Clamp(deltaX / Mathf.Max(60f, swipeSensitivity), -1f, 1f);
                _currentSwipeSteer = Mathf.Lerp(_currentSwipeSteer, target, 18f * Time.deltaTime);
            }
            else if (touchUp || !touchHeld)
            {
                _isSwiping         = false;
                _currentSwipeSteer = Mathf.Lerp(_currentSwipeSteer, 0f, 14f * Time.deltaTime);
                if (Mathf.Abs(_currentSwipeSteer) < 0.01f) _currentSwipeSteer = 0f;
            }

            CurrentSwipeOffset = _currentSwipeSteer;
            return _currentSwipeSteer;
        }

        private float ReadTiltSteer()
        {
            Vector3 accel   = GetRawAcceleration();
            Vector2 inPlane = new Vector2(accel.x, accel.y);

            if (inPlane.sqrMagnitude < 0.0625f)
            {
                _smoothedTiltSteer = Mathf.Lerp(_smoothedTiltSteer, 0f, 10f * Time.deltaTime);
                return _smoothedTiltSteer;
            }

            float angle = Mathf.Atan2(inPlane.x, inPlane.y) * Mathf.Rad2Deg;
            if (!_tiltCalibrated)
            {
                _tiltBiasAngle  = angle;
                _tiltCalibrated = true;
            }

            float delta  = Mathf.DeltaAngle(_tiltBiasAngle, angle);
            float target = Mathf.Clamp(delta / Mathf.Max(1f, tiltSensitivity), -1f, 1f);
            if (invertTilt) target = -target;

            _smoothedTiltSteer = Mathf.Lerp(_smoothedTiltSteer, target, 12f * Time.deltaTime);
            return _smoothedTiltSteer;
        }

        public static bool HasGyroscopeSupport()
        {
            return SystemInfo.supportsGyroscope || SystemInfo.supportsAccelerometer;
        }

        public void CalibrateTilt()
        {
            _tiltCalibrated   = false;
            _smoothedTiltSteer= 0f;
        }

        private static Vector3 GetRawAcceleration()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                var accelerometer = Accelerometer.current;
                if (accelerometer != null)
                {
                    if (!accelerometer.enabled)
                        InputSystem.EnableDevice(accelerometer);
                    return accelerometer.acceleration.ReadValue();
                }
            }
            catch { }
            return Vector3.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.acceleration;
#else
            return Vector3.zero;
#endif
        }
    }
}
