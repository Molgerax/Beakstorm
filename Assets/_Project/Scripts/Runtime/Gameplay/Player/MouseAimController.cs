using Beakstorm.Inputs;
using Beakstorm.Utility;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class MouseAimController : MonoBehaviour
    {
        public static MouseAimController Instance;
    
        [SerializeField] private Transform mouseAim;
        [SerializeField] private Transform aircraft;
        [SerializeField] [Tooltip("Transform of the object on the rig which the camera is attached to")]
        private Transform cameraRig = null;
        [SerializeField] [Tooltip("Transform of the camera itself")]
        private Transform cam = null;

        

        [Header("Options")]
        [SerializeField] [Tooltip("Follow aircraft using fixed update loop")]
        private bool useFixed = true;

        [SerializeField] [Tooltip("How far the boresight and mouse flight are from the aircraft")]
        private float aimDistance = 500f;

        [SerializeField] [Tooltip("How quickly the camera tracks the mouse aim point.")]
        private float camSmoothSpeed = 5f;
        
        private Vector3 frozenDirection = Vector3.forward;
        private bool isMouseAimFrozen = false;

        
        /// <summary>
        /// Get a point along the aircraft's boresight projected out to aimDistance meters.
        /// Useful for drawing a crosshair to aim fixed forward guns with, or to indicate what
        /// direction the aircraft is pointed.
        /// </summary>
        public Vector3 BoresightPos
        {
            get
            {
                return aircraft == null
                    ? transform.forward * aimDistance
                    : (aircraft.transform.forward * aimDistance) + aircraft.transform.position;
            }
        }

        /// <summary>
        /// Get the position that the mouse is indicating the aircraft should fly, projected
        /// out to aimDistance meters. Also meant to be used to draw a mouse cursor.
        /// </summary>
        public Vector3 MouseAimPos
        {
            get
            {
                if (mouseAim != null)
                {
                    return isMouseAimFrozen
                        ? GetFrozenMouseAimPos()
                        : mouseAim.position + (mouseAim.forward * aimDistance);
                }
                else
                {
                    return transform.forward * aimDistance;
                }
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            UpdateCameraPos();
            RotateRig();
        }

        private void RotateRig()
        {
            if (mouseAim == null || cam == null || cameraRig == null)
                return;

            // Freeze the mouse aim direction when the free look key is pressed.
            if (Input.GetKeyDown(KeyCode.C))
            {
                isMouseAimFrozen = true;
                frozenDirection = mouseAim.forward;
            }
            else if  (Input.GetKeyUp(KeyCode.C))
            {
                isMouseAimFrozen = false;
                mouseAim.forward = frozenDirection;
            }

            Vector2 lookInput = PlayerInputs.Instance.LookInput;
            
            // Mouse input.
            float mouseX = lookInput.x * Time.deltaTime;
            float mouseY = -lookInput.y * Time.deltaTime;

            // Rotate the aim target that the plane is meant to fly towards.
            // Use the camera's axes in world space so that mouse motion is intuitive.
            mouseAim.Rotate(cam.right, mouseY, Space.World);
            mouseAim.Rotate(cam.up, mouseX, Space.World);

            // The up vector of the camera normally is aligned to the horizon. However, when
            // looking straight up/down this can feel a bit weird. At those extremes, the camera
            // stops aligning to the horizon and instead aligns to itself.
            Vector3 upVec = (Mathf.Abs(mouseAim.forward.y) > 0.9f) ? cameraRig.up : Vector3.up;

            // Smoothly rotate the camera to face the mouse aim.
            cameraRig.rotation = SmoothDamp.Rotate(cameraRig.rotation,
                                      Quaternion.LookRotation(mouseAim.forward, upVec),
                                      camSmoothSpeed,
                                      Time.deltaTime);
        }

        private Vector3 GetFrozenMouseAimPos()
        {
            if (mouseAim != null)
                return mouseAim.position + (frozenDirection * aimDistance);
            else
                return transform.forward * aimDistance;
        }

        private void UpdateCameraPos()
        {
            if (aircraft != null)
            {
                // Move the whole rig to follow the aircraft.
                transform.position = aircraft.position;
            }
        }
    }
}
