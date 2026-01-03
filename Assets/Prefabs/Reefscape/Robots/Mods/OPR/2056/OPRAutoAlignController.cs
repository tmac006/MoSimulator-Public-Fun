using Games.Reefscape.Enums;
using Games.Reefscape.Robots;
using RobotFramework.Controllers.Drivetrain;
using UnityEngine;

namespace Games.Reefscape.Components
{
 
    public class OPRAutoAlignController : MonoBehaviour
    {
        [Header("Setpoint Offsets")]
        [Tooltip("Offset asset for L4 setpoint")]
        [SerializeField] private OPRAutoAlignOffset l4Offset;
        
        [Tooltip("Offset asset for L3 setpoint")]
        [SerializeField] private OPRAutoAlignOffset l3Offset;
        
        [Tooltip("Offset asset for L2 setpoint")]
        [SerializeField] private OPRAutoAlignOffset l2Offset;
        
        [Tooltip("Offset asset for L1 setpoint")]
        [SerializeField] private OPRAutoAlignOffset l1Offset;
        
        [Tooltip("Default offset asset when no specific setpoint is active")]
        [SerializeField] private OPRAutoAlignOffset noneOffset;

        [Header("Wildlife Protection Program")]
        [Tooltip("Additional distance to add to the X offset for avoiding the reef while the robot's superstructure is preparing to score")]
        public float avoidDistance = 0.25f;
        [Tooltip("The amount of time in seconds to avoid the reef for while the robot's superstructure is preparing to score")]
        public float avoidTime = 0.5f;

        [Header("Translation")]
        [Tooltip("The configured maximum velocity of the robot, in ft/s")]
        public float maxVelocity = 17.0f;
        [Tooltip("The maximum deceleration of the auto-align controller, in ft/s/s")]
        public float maxDeceleration = 32.0f;
        [Tooltip("Arbitrary strength force for centering the robot on the targeted reef pole")]
        public float strength = 250f;
        [Tooltip("The tolerance at which the controller signals that the robot is in position")]
        public float positionTolerance = 0.05f;

        [Header("Rotation")]
        [Tooltip("Kp constant to apply to the robot's rotation")]
        public float rotateKp = 1f;

        private DriveController _driveController;
        private ReefscapeRobotBase _base;
        private bool _usePerspective;
        private Vector2 _blueReef;
        private Vector2 _redReef;

        private float _reefDistance = 0f;
        private bool _left = true;
        private bool _inPosition = false;
        private bool _fallingEdge = false;
        private float _waitUntil = 0f;
        private float _currentY = 0.17f;

        protected void Start()
        {
            _driveController = gameObject.GetComponent<DriveController>();
            _base = gameObject.GetComponent<ReefscapeRobotBase>();
            _usePerspective = PlayerPrefs.GetInt("PerspectiveAutoAlign", 1) == 1;
            _blueReef = Vec3ToVec2(GameObject.Find("BlueReef").transform.position);
            _redReef = Vec3ToVec2(GameObject.Find("RedReef").transform.position);
        }

        /// <summary>
        /// Gets the appropriate offset based on the current setpoint.
        /// </summary>
        private OPRAutoAlignOffset GetCurrentOffset()
        {
            OPRAutoAlignOffset offset = _base.CurrentSetpoint switch
            {
                ReefscapeSetpoints.L4 => l4Offset,
                ReefscapeSetpoints.L3 => l3Offset,
                ReefscapeSetpoints.L2 => l2Offset,
                ReefscapeSetpoints.L1 => l1Offset,
                _ => noneOffset
            };
            
            // Fallback chain: setpoint-specific -> noneOffset -> null (handled in FixedUpdate)
            return offset != null ? offset : noneOffset;
        }

        protected void FixedUpdate()
        {
            var currentOffset = GetCurrentOffset();
            
            // Safety check: if no offset is assigned, skip auto-align
            if (currentOffset == null)
            {
                return;
            }
            
            var max_v = maxVelocity * FT_TO_M;
            var max_a = maxDeceleration * FT_TO_M;

            var robot = Vec3ToVec2(_base.transform.position);
            bool c_l = _base.AutoAlignLeftAction.IsPressed();
            bool pressed = c_l || _base.AutoAlignRightAction.IsPressed();
            bool cm_f = _base.GetActiveCamera().transform.forward.x < 0;

            float blueDistance = (_blueReef - robot).magnitude;
            float redDistance = (_redReef - robot).magnitude;
            var reef = blueDistance < redDistance ? _blueReef : _redReef;
            _reefDistance = Mathf.Min(blueDistance, redDistance);

            var face = Rotate2((reef - robot).normalized, SIXTH_PI);
            float rk_w = Mathf.Floor(Mathf.Atan2(face.y, face.x) / THIRD_PI) * THIRD_PI;
            _left = !_usePerspective ? c_l : cm_f ^ (Mathf.Abs(rk_w) < HALF_PI) ^ !c_l;

            bool waiting = Time.time < _waitUntil;
            _currentY = currentOffset.y;
            float rk_x = currentOffset.x + (waiting ? avoidDistance : 0f);
            var pole = Pole(rk_x, rk_w, _left, reef);
            var error = pole - robot;

            _inPosition = error.magnitude < positionTolerance && !waiting;
            _fallingEdge = pressed && (_fallingEdge || _inPosition);

            if (!_fallingEdge && pressed)
            {
                float k = 1f;
                if (error.magnitude < max_v * max_v / (2f * max_a))
                {
                    k = Mathf.Sqrt(2f * max_a * error.magnitude) / max_v;
                }

                var force = error.normalized;

                var att = Pole(SL, rk_w, _left, reef) - pole;
                var proj = Rotate2(robot - pole, -Mathf.Atan2(att.y, att.x));
                if (proj.x > 0.0 && proj.x < SL - rk_x)
                {
                    float t = proj.y / SW / 2f;
                    float m = -strength * t * t * Mathf.Sign(proj.y) / Mathf.Abs(proj.x);
                    force += m * Rotate2(Vector2.down, rk_w);
                }

                float yaw = -Mathf.Deg2Rad * (_base.transform.rotation.eulerAngles.y - 90f);
                float alpha = rotateKp * ((rk_w - yaw + THREE_PI) % TWO_PI - Mathf.PI);

                _driveController.overideInput(force.normalized * k, alpha, DriveController.DriveMode.FieldOriented);
            }
            else
            {
                _waitUntil = Time.time + avoidTime;
            }
        }

        private Vector2 Pole(float x, float angle, bool left, Vector2 reef)
        {
            return reef + Rotate2(new Vector2(-x, _currentY * (left ? 1f : -1f)), angle);
        }

        private Vector2 Rotate2(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private Vector2 Vec3ToVec2(Vector3 vec3)
        {
            return new Vector2(vec3.x, vec3.z);
        }

        /// <summary>
        /// Gets the current distance to the nearest reef.
        /// </summary>
        public float ReefDistance()
        {
            return _reefDistance;
        }

        /// <summary>
        /// Returns true if aligning to the left pole.
        /// </summary>
        public bool Left()
        {
            return _left;
        }

        /// <summary>
        /// Returns true if the robot is in the target position.
        /// </summary>
        public bool InPosition()
        {
            return _inPosition;
        }

        private const float SL = 10f;
        private const float SW = 6f;
        private const float FT_TO_M = 0.3048f;
        private const float SIXTH_PI = Mathf.PI / 6f;
        private const float THIRD_PI = Mathf.PI / 3f;
        private const float HALF_PI = Mathf.PI / 2f;
        private const float TWO_PI = Mathf.PI * 2f;
        private const float THREE_PI = Mathf.PI * 3f;
    }
}

