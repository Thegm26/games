using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BeforeTheAxes
{
    public readonly struct GuardianInputState
    {
        public GuardianInputState(Vector2 move, bool running) : this(move, running, false)
        {
        }

        public GuardianInputState(Vector2 move, bool running, bool crouching)
        {
            Move = move;
            Running = running;
            Crouching = crouching;
        }

        public Vector2 Move { get; }
        public bool Running { get; }
        public bool Crouching { get; }
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class ForestGuardianController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [Header("Transformation")]
        [SerializeField] private GameObject humanVisual;
        [SerializeField] private GameObject treeFormVisual;
        [SerializeField] private float treeMaxStamina = 8f;
        [SerializeField] private float treeStaminaDrainPerSecond = 1f;
        [SerializeField] private float treeStaminaRecoveryPerSecond = 0.8f;
        [SerializeField] private float treeStaminaRestartThreshold = 2f;
        // Full-size ITHappy locomotion: deliberately conservative to match its short walk/run clips.
        [SerializeField] private float walkSpeed = 1.1f;
        [SerializeField] private float runSpeed = 2.7f;
        [SerializeField] private float crouchSpeed = 0.6f;
        [SerializeField] private float turnSpeed = 12f;
        [Header("Crouch")]
        [SerializeField] private float crouchedControllerHeight = 1.25f;
        [SerializeField] private float crouchTransitionSeconds = 0.12f;
        [Header("Run stamina")]
        [SerializeField] private float maxStamina = 4f;
        [SerializeField] private float staminaDrainPerSecond = 1f;
        [SerializeField] private float staminaRecoveryPerSecond = 0.8f;
        // A small restart threshold prevents Shift from rapidly toggling run/rest at empty stamina.
        [SerializeField] private float staminaRestartThreshold = 0.7f;

        private CharacterController controller;
        private Animator animator;
        private GuardianCrouchPose crouchPose;
        private float verticalVelocity;
        private float stamina;
        private float treeStamina;
        private bool canRun = true;
        private bool canTransformTree = true;
        private bool initialized;
        private float standingControllerHeight;
        private Vector3 standingControllerCenter;
        private float crouchBlend;

        // Exposed for the runtime regression probe and for any future UI input.
        // This value is always sampled after CharacterController.Move.
        public bool IsPhysicallyGrounded { get; private set; }
        public float VerticalVelocity => verticalVelocity;
        public float Stamina => stamina;
        public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;
        public bool IsRunning { get; private set; }
        /// <summary>True when the player is holding sprint with movement input and should use the
        /// run visual, even if stamina has forced the physical movement back to walking.</summary>
        public bool WantsRunAnimation { get; private set; }
        public bool IsCrouching { get; private set; }
        public float CurrentMovementSpeed { get; private set; }
        /// <summary>Actual horizontal displacement per second after CharacterController physics.
        /// Unlike CurrentMovementSpeed, this is zero when movement is blocked or there is no input.</summary>
        public float HorizontalWorldSpeed { get; private set; }
        public float CrouchBlend => crouchBlend;
        public bool IsExhausted => !canRun;
        public bool IsTreeForm { get; private set; }
        public float TreeStamina => treeStamina;
        public float MaxTreeStamina => treeMaxStamina;
        public bool CanTransformTree => canTransformTree;
        /// <summary>Raised immediately before a deliberate human-to-tree visual swap. This is
        /// intentionally separate from <see cref="TreeFormChanged"/> so short confirmation cues
        /// can begin before the tree appears, rather than lag behind it.</summary>
        public event Action TreeFormEntering;

        /// <summary>Raised immediately after the player changes form. Observers can retain sight
        /// when a tree was entered in plain view instead of treating it as instant invisibility.</summary>
        public event Action<bool> TreeFormChanged;

#if UNITY_EDITOR
        // Editor verification can exercise the production Update path without taking over desktop input.
        private static Func<GuardianInputState> editorInputOverride;

        public static void SetEditorInputOverride(Func<GuardianInputState> value)
        {
            editorInputOverride = value;
        }
#endif

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            controller = GetComponent<CharacterController>();
            standingControllerHeight = controller.height;
            standingControllerCenter = controller.center;
            if (humanVisual == null) humanVisual = FindChild("Aminset_Basic")?.gameObject;
            if (treeFormVisual == null) treeFormVisual = FindChild("Tree Form Visual")?.gameObject;
            animator = humanVisual == null ? GetComponentInChildren<Animator>(true) : humanVisual.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.applyRootMotion = false;
            crouchPose = GetComponent<GuardianCrouchPose>();
            if (crouchPose == null) crouchPose = gameObject.AddComponent<GuardianCrouchPose>();
            crouchPose.Configure(animator);
            ApplyVisualState();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            IsPhysicallyGrounded = controller.isGrounded;
            stamina = maxStamina;
            treeStamina = treeMaxStamina;
            initialized = true;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ToggleTreeForm();
            Step(SampleInput(), Time.deltaTime);
        }

        // Movement, grounding, and animation all flow through this one path so editor
        // verification advances the exact same controller logic as a normal frame.
        private void Step(GuardianInputState sampledInput, float deltaTime)
        {
            Initialize();
            UpdateTreeStamina(deltaTime);
            Vector2 input = sampledInput.Move;
            input = Vector2.ClampMagnitude(input, 1f);
            bool hasMovementInput = input.sqrMagnitude > 0.001f;
            if (!canRun && stamina >= staminaRestartThreshold) canRun = true;
            bool wantsCrouch = sampledInput.Crouching && !IsTreeForm;
            // Do not let the player stand into a low branch, rock, or roof. Holding crouch always
            // works; releasing it only expands the capsule when its full standing volume is clear.
            IsCrouching = wantsCrouch || (IsCrouching && !CanStandUp());
            if (crouchPose != null) crouchPose.SetCrouching(IsCrouching);
            UpdateControllerCrouch(deltaTime);
            // Visual intent is deliberately separate from physical sprinting. An exhausted guardian
            // keeps the running animation while Shift is held, but only moves at walk speed.
            bool wantsRunAnimation = sampledInput.Running && !IsTreeForm && !IsCrouching && hasMovementInput;
            bool running = wantsRunAnimation && canRun && stamina > 0f;
            WantsRunAnimation = wantsRunAnimation;
            IsRunning = running;
            if (running)
            {
                stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * deltaTime);
                if (stamina <= 0f) canRun = false;
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * deltaTime);
            }

            Vector3 forward = cameraTransform == null ? Vector3.forward : Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 move = (forward * input.y + right * input.x).normalized;

            if (move.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * deltaTime);
            }

            bool wasGrounded = controller.isGrounded;
            if (wasGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += Physics.gravity.y * deltaTime;
            float movementSpeed = IsCrouching ? crouchSpeed : running ? runSpeed : walkSpeed;
            CurrentMovementSpeed = movementSpeed;
            Vector3 positionBeforeMove = transform.position;
            CollisionFlags flags = controller.Move((move * movementSpeed + Vector3.up * verticalVelocity) * deltaTime);
            Vector3 horizontalDisplacement = Vector3.ProjectOnPlane(transform.position - positionBeforeMove, Vector3.up);
            HorizontalWorldSpeed = deltaTime > 0.0001f ? horizontalDisplacement.magnitude / deltaTime : 0f;
            IsPhysicallyGrounded = (flags & CollisionFlags.Below) != 0 || controller.isGrounded;
            if (IsPhysicallyGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

            // The permanent controller uses ITHappy's humanoid idle, walk and run clips.
            if (!IsTreeForm && animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat("Speed", input.magnitude * (wantsRunAnimation ? 2f : 1f));
            }
        }

        /// <summary>Editor regression seam; advances the same path used by Update.</summary>
        public void StepForEditor(GuardianInputState input, float deltaTime)
        {
            Step(input, deltaTime);
        }

        /// <summary>Toggles the visual form without affecting movement or CharacterController physics.</summary>
        public void ToggleTreeForm()
        {
            if (!IsTreeForm && !canTransformTree) return;
            SetTreeForm(!IsTreeForm);
        }

        private void SetTreeForm(bool treeForm)
        {
            // Start the deliberate entry cue before hiding the human / showing the tree. Exits,
            // including stamina-forced exits, never invoke this event.
            if (treeForm && !IsTreeForm) TreeFormEntering?.Invoke();
            IsTreeForm = treeForm;
            IsCrouching = false;
            if (crouchPose != null) crouchPose.SetCrouching(false);
            ApplyVisualState();
            TreeFormChanged?.Invoke(IsTreeForm);
            if (!IsTreeForm)
            {
                if (animator == null && humanVisual != null) animator = humanVisual.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                    animator.SetFloat("Speed", 0f);
                }
            }
        }

        private void UpdateTreeStamina(float deltaTime)
        {
            if (IsTreeForm)
            {
                treeStamina = Mathf.Max(0f, treeStamina - treeStaminaDrainPerSecond * deltaTime);
                if (treeStamina <= 0f)
                {
                    canTransformTree = false;
                    SetTreeForm(false);
                }
            }
            else
            {
                treeStamina = Mathf.Min(treeMaxStamina, treeStamina + treeStaminaRecoveryPerSecond * deltaTime);
                if (!canTransformTree && treeStamina >= treeStaminaRestartThreshold) canTransformTree = true;
            }
        }

        private void ApplyVisualState()
        {
            if (humanVisual != null) humanVisual.SetActive(!IsTreeForm);
            if (treeFormVisual != null) treeFormVisual.SetActive(IsTreeForm);
        }

        private void UpdateControllerCrouch(float deltaTime)
        {
            float target = IsCrouching ? 1f : 0f;
            crouchBlend = Mathf.MoveTowards(crouchBlend, target, deltaTime / Mathf.Max(0.01f, crouchTransitionSeconds));
            controller.height = Mathf.Lerp(standingControllerHeight, crouchedControllerHeight, crouchBlend);
            Vector3 crouchedCenter = new Vector3(standingControllerCenter.x, crouchedControllerHeight * .5f, standingControllerCenter.z);
            controller.center = Vector3.Lerp(standingControllerCenter, crouchedCenter, crouchBlend);
        }

        private bool CanStandUp()
        {
            if (crouchBlend <= 0f) return true;
            float radius = controller.radius;
            Vector3 standingCenter = transform.TransformPoint(standingControllerCenter);
            float capsuleHalfLine = Mathf.Max(0f, standingControllerHeight * .5f - radius);
            Vector3 bottom = standingCenter - Vector3.up * capsuleHalfLine;
            Vector3 top = standingCenter + Vector3.up * capsuleHalfLine;
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
            {
                // A CharacterController can report one of the guardian's own child colliders in
                // overlap queries. Only external geometry should prevent standing up.
                if (overlap.transform == transform || overlap.transform.IsChildOf(transform)) continue;
                return false;
            }

            return true;
        }

        private Transform FindChild(string childName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName) return child;
            }

            return null;
        }

        private static GuardianInputState SampleInput()
        {
#if UNITY_EDITOR
            if (editorInputOverride != null) return editorInputOverride();
#endif
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return default;

            float horizontal = Pressed(keyboard.aKey, keyboard.leftArrowKey) ? -1f : 0f;
            if (Pressed(keyboard.dKey, keyboard.rightArrowKey)) horizontal += 1f;
            float vertical = Pressed(keyboard.sKey, keyboard.downArrowKey) ? -1f : 0f;
            if (Pressed(keyboard.wKey, keyboard.upArrowKey)) vertical += 1f;

            return new GuardianInputState(
                new Vector2(horizontal, vertical),
                Pressed(keyboard.leftShiftKey, keyboard.rightShiftKey),
                Pressed(keyboard.leftCtrlKey, keyboard.rightCtrlKey) || keyboard.cKey.isPressed);
        }

        private static bool Pressed(KeyControl first, KeyControl second)
        {
            return first.isPressed || second.isPressed;
        }
    }
}
