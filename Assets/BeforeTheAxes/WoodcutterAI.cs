using UnityEngine;

namespace BeforeTheAxes
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class WoodcutterAI : MonoBehaviour
    {
        public enum AlertState { Patrol, Search, Chase }

        [SerializeField] private ForestGuardianController player;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Animator animator;
        [SerializeField] private float sightRange = 14f;
        [SerializeField] private float crouchedSightRange = 7f;
        [SerializeField] private float crouchedCloseSightRange = 2f;
        [SerializeField, Range(1f, 180f)] private float fieldOfView = 100f;
        [Tooltip("Width of the physical line-of-sight test. Trees and props inside this body-width corridor block sight.")]
        [SerializeField, Range(.05f, .5f)] private float sightProbeRadius = .3f;
        [SerializeField] private float patrolSpeed = 1.25f;
        [Header("Patrol behaviour")]
        [Tooltip("Seconds spent observing after arriving at each patrol point. Set to zero for a continuous route.")]
        [SerializeField, Min(0f)] private float patrolWaitSeconds;
        [SerializeField, Range(0f, 180f)] private float patrolLookAroundDegreesPerSecond = 32f;
        [SerializeField] private float chaseSpeed = 3.15f;
        [Header("Stamina")]
        [SerializeField, Min(.1f)] private float maxStamina = 5f;
        [SerializeField, Min(.01f)] private float chaseStaminaDrainPerSecond = 1f;
        [SerializeField, Min(.01f)] private float staminaRecoveryPerSecond = 1.25f;
        [SerializeField, Range(.05f, 1f)] private float runRecoveryThreshold = .3f;
        [SerializeField] private float searchDuration = 4f;
        [SerializeField] private float catchDistance = 1.2f;
        [Header("Tree disguise")]
        [Tooltip("A visible tree moving at ordinary walking speed gives itself away. This uses actual world speed, so stationary trees and tiny collision jitter remain safe.")]
        [SerializeField, Min(.1f)] private float treeRevealRunSpeed = .75f;
        [Header("Obstacle avoidance")]
        [SerializeField, Min(.1f)] private float obstacleProbeRadius = .23f;
        [SerializeField, Min(.2f)] private float obstacleProbeDistance = 1.2f;
        [SerializeField, Range(20f, 85f)] private float sideSteerAngle = 62f;
        [SerializeField, Min(.1f)] private float obstacleClearConfirmation = .22f;
        [SerializeField, Min(.5f)] private float maxObstacleBypassDuration = 4f;
        [Header("Debug")]
        [SerializeField] private bool debugLocomotion = true;

        private CharacterController controller;
        private int patrolIndex;
        private float patrolWaitRemaining;
        private float searchTimer;
        private float verticalVelocity;
        private Vector3 lastKnownPosition;
        private bool treeEnteredInView;
        private int appliedAnimationStateHash = int.MinValue;
        private float nextLocomotionDebugTime;
        private int lastObservedAnimatorStateHash = int.MinValue;
        private int lastObservedAnimatorNextStateHash = int.MinValue;
        private float stamina;
        private bool runExhausted;
        private int avoidanceSide = 1;
        private bool avoidingObstacle;
        private float avoidanceStartedAt;
        private float avoidanceClearSince = -1f;
        private string lastAvoidanceHit = "none";
        private string lastSightBlocker = "clear";
        private float lastSightBlockerDistance = -1f;
        private bool hasLoggedVision;
        private bool lastLoggedGeometryVisible;

        // Animator.HasState and CrossFade expect the full state path, including the layer.
        // The short hashes are kept only for diagnostics so a bad controller path is obvious.
        private static readonly int IdleStatePathHash = Animator.StringToHash("Base Layer.ITHappy Idle");
        private static readonly int WalkStatePathHash = Animator.StringToHash("Base Layer.ITHappy Walk");
        private static readonly int RunStatePathHash = Animator.StringToHash("Base Layer.ITHappy Run");
        private static readonly int IdleStateShortHash = Animator.StringToHash("ITHappy Idle");
        private static readonly int WalkStateShortHash = Animator.StringToHash("ITHappy Walk");
        private static readonly int RunStateShortHash = Animator.StringToHash("ITHappy Run");

        public AlertState State { get; private set; }
        public bool HasDirectSight { get; private set; }
        public bool IsTreeMovementRevealing { get; private set; }
        public bool TreeEnteredInView => treeEnteredInView;
        public float Stamina => stamina;
        public float MaxStamina => maxStamina;
        public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;
        public bool IsRunExhausted => runExhausted;
        public float LastAnimationSpeed { get; private set; }
        public float RequestedMovementSpeed { get; private set; }
        public bool IsAvoidingObstacle => avoidingObstacle;
        public int AvoidanceCount { get; private set; }
        public int LastAvoidanceSide => avoidanceSide;
        public string LastAvoidanceHit => lastAvoidanceHit;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            stamina = maxStamina;
            if (player == null) player = FindFirstObjectByType<ForestGuardianController>();
            if (player != null) player.TreeFormChanged += OnPlayerFormChanged;
            State = AlertState.Patrol;
        }

        private void OnDestroy()
        {
            if (player != null) player.TreeFormChanged -= OnPlayerFormChanged;
        }

        private void Update()
        {
            if (player == null) return;
            UpdateSight();
            switch (State)
            {
                case AlertState.Patrol: Patrol(); break;
                case AlertState.Search: Search(); break;
                case AlertState.Chase: Chase(); break;
            }
        }

        // Animator state changes after Update. Sampling here reports the state and clip the
        // renderer actually receives, rather than merely the parameter we asked for.
        private void LateUpdate()
        {
            if (!debugLocomotion || animator == null || animator.runtimeAnimatorController == null) return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            bool stateChanged = current.fullPathHash != lastObservedAnimatorStateHash ||
                                next.fullPathHash != lastObservedAnimatorNextStateHash;
            bool moving = RequestedMovementSpeed > .05f;
            if (!stateChanged && (!moving || Time.time < nextLocomotionDebugTime)) return;

            LogAnimatorDiagnosis(current, next, stateChanged ? "state-change" : "moving");
            lastObservedAnimatorStateHash = current.fullPathHash;
            lastObservedAnimatorNextStateHash = next.fullPathHash;
            nextLocomotionDebugTime = Time.time + .5f;
        }

        private void OnPlayerFormChanged(bool isTree)
        {
            if (isTree)
            {
                // Test physical view at the exact form-change event. A visible tree remains a target
                // until terrain or another collider actually cuts this line.
                treeEnteredInView = CanSeePlayerGeometry();
                if (treeEnteredInView) { State = AlertState.Chase; lastKnownPosition = player.transform.position; }
            }
            else treeEnteredInView = false;
        }

        private void UpdateSight()
        {
            bool geometryVisible = CanSeePlayerGeometry();
            if (debugLocomotion && (!hasLoggedVision || geometryVisible != lastLoggedGeometryVisible))
            {
                Debug.Log($"[Woodcutter Sight] visible={geometryVisible} blocker={lastSightBlocker} hitDistance={lastSightBlockerDistance:F2}", this);
                hasLoggedVision = true;
                lastLoggedGeometryVisible = geometryVisible;
            }
            // A still (or slowly rolling) disguised tree is ignored. It becomes suspicious only
            // when it moves at a real walking pace in a physically unobstructed view. A tree
            // entered directly in view remains known, as before.
            IsTreeMovementRevealing = player.IsTreeForm && player.HorizontalWorldSpeed >= treeRevealRunSpeed;
            HasDirectSight = !player.IsTreeForm
                ? geometryVisible
                : geometryVisible && (treeEnteredInView || IsTreeMovementRevealing);
            if (HasDirectSight)
            {
                State = AlertState.Chase;
                searchTimer = searchDuration;
                lastKnownPosition = player.transform.position;
            }
            else if (State == AlertState.Chase)
            {
                // A disguised tree becomes hidden only after its formerly visible line is blocked.
                if (player.IsTreeForm && treeEnteredInView && !geometryVisible) treeEnteredInView = false;
                State = AlertState.Search;
                searchTimer = searchDuration;
            }
        }

        private bool CanSeePlayerGeometry()
        {
            Vector3 eye = transform.position + Vector3.up * 1.45f;
            // Crouching only removes sight when it genuinely puts the player behind terrain or
            // scenery: raycast to the lower body, rather than granting an invisibility flag.
            Vector3 target = player.transform.position + Vector3.up * (player.IsCrouching ? 0.52f : 0.9f);
            Vector3 toTarget = target - eye;
            float distance = toTarget.magnitude;
            bool crouchedClose = player.IsCrouching && distance <= crouchedCloseSightRange;
            float effectiveSightRange = player.IsCrouching ? crouchedSightRange : sightRange;
            if ((!crouchedClose && distance > effectiveSightRange) || distance < .01f) return distance < .01f;
            // At arm's length a crouching player cannot remain unnoticed simply by being below
            // the normal FOV ray; real geometry still blocks the raycast below.
            if (!crouchedClose && Vector3.Angle(transform.forward, toTarget) > fieldOfView * .5f) return false;
            // A pin-thin ray slips through the gaps around a visible tree trunk.  Use a
            // body-width cast from the cutter's eye to the player's chest so every physical
            // tree/prop collider inside that corridor is a real line-of-sight blocker.
            RaycastHit[] hits = Physics.SphereCastAll(eye, sightProbeRadius, toTarget / distance, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.transform.IsChildOf(player.transform) || hit.collider.transform == player.transform) return true;
                lastSightBlocker = hit.collider.name;
                lastSightBlockerDistance = hit.distance;
                return false;
            }
            // The player CharacterController can be skipped by a raycast. No intervening hit is a clear physical view.
            lastSightBlocker = "clear";
            lastSightBlockerDistance = -1f;
            return true;
        }

        private void Patrol()
        {
            UpdateStamina(false, Time.deltaTime);
            if (patrolPoints == null || patrolPoints.Length == 0) { Move(Vector3.zero, 0f); return; }

            // A zero wait is a continuous circuit. A lookout may deliberately pause at a point
            // and slowly scan around before moving to the next one.
            if (patrolWaitRemaining > 0f)
            {
                patrolWaitRemaining = Mathf.Max(0f, patrolWaitRemaining - Time.deltaTime);
                transform.Rotate(Vector3.up, patrolLookAroundDegreesPerSecond * Time.deltaTime);
                Move(Vector3.zero, 0f);
                return;
            }

            Vector3 target = patrolPoints[patrolIndex].position;
            for (int checkedPoints = 0; checkedPoints < patrolPoints.Length && FlatDistance(target) < .55f; checkedPoints++)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                if (patrolWaitSeconds > .01f)
                {
                    patrolWaitRemaining = patrolWaitSeconds;
                    Move(Vector3.zero, 0f);
                    return;
                }
                target = patrolPoints[patrolIndex].position;
            }
            Move(target - transform.position, patrolSpeed);
        }

        private void Search()
        {
            UpdateStamina(false, Time.deltaTime);
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f) { State = AlertState.Patrol; return; }
            if (FlatDistance(lastKnownPosition) > .65f) Move(lastKnownPosition - transform.position, patrolSpeed);
            else { transform.Rotate(Vector3.up, 90f * Time.deltaTime); Move(Vector3.zero, 0f); }
        }

        private void Chase()
        {
            if (FlatDistance(player.transform.position) <= catchDistance)
            {
                PlayerCaught caught = player.GetComponent<PlayerCaught>();
                if (caught != null) caught.Catch();
                enabled = false;
                return;
            }
            bool canRun = !runExhausted && stamina > 0f;
            UpdateStamina(canRun, Time.deltaTime);
            Move(lastKnownPosition - transform.position, canRun ? chaseSpeed : patrolSpeed);
        }

        private void Move(Vector3 direction, float speed)
        {
            RequestedMovementSpeed = speed;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude > .001f)
            {
                direction.Normalize();
                direction = SteerAroundObstacle(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            }
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            // CharacterController.velocity can briefly be zero on terrain seams even while a
            // movement command is active. Animate from the requested planar motion so the gait
            // remains continuous; actual collision resolution still owns world movement.
            float actualHorizontalSpeed = Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude;
            LastAnimationSpeed = AnimationSpeedFor(actualHorizontalSpeed, speed, patrolSpeed);
            ApplyLocomotionAnimation(LastAnimationSpeed);
        }

        private Vector3 SteerAroundObstacle(Vector3 desiredDirection)
        {
            if (avoidingObstacle)
            {
                bool frontStillBlocked = DirectionIsBlocked(desiredDirection, out RaycastHit frontHit);
                if (frontStillBlocked)
                {
                    lastAvoidanceHit = frontHit.collider.name;
                    avoidanceClearSince = -1f;
                }
                else if (avoidanceClearSince < 0f)
                {
                    // Do not release steering on one lucky frame at the collider edge. The cutter
                    // must have a clear original path briefly before it can turn back to its target.
                    avoidanceClearSince = Time.time;
                }

                float elapsed = Time.time - avoidanceStartedAt;
                if (elapsed >= maxObstacleBypassDuration)
                {
                    EndObstacleAvoidance("timeout", elapsed);
                }
                else if (!frontStillBlocked && Time.time - avoidanceClearSince >= obstacleClearConfirmation)
                {
                    EndObstacleAvoidance("clear", elapsed);
                    return desiredDirection;
                }
                else
                {
                    return SideSteer(desiredDirection);
                }
            }

            if (!DirectionIsBlocked(desiredDirection, out RaycastHit newFrontHit)) return desiredDirection;
            lastAvoidanceHit = newFrontHit.collider.name;

            Vector3 left = Quaternion.AngleAxis(-sideSteerAngle, Vector3.up) * desiredDirection;
            Vector3 right = Quaternion.AngleAxis(sideSteerAngle, Vector3.up) * desiredDirection;
            bool leftBlocked = DirectionIsBlocked(left, out RaycastHit leftHit);
            bool rightBlocked = DirectionIsBlocked(right, out RaycastHit rightHit);
            if (!leftBlocked && rightBlocked) avoidanceSide = -1;
            else if (leftBlocked && !rightBlocked) avoidanceSide = 1;
            else if (leftBlocked && rightBlocked)
                avoidanceSide = leftHit.distance >= rightHit.distance ? -1 : 1;
            else avoidanceSide = -avoidanceSide; // Choose one clear side, then keep it until clear.

            avoidingObstacle = true;
            avoidanceStartedAt = Time.time;
            avoidanceClearSince = -1f;
            AvoidanceCount++;
            if (debugLocomotion)
                Debug.Log($"[Woodcutter Avoid] start hit={lastAvoidanceHit} side={(avoidanceSide < 0 ? "left" : "right")} probe={obstacleProbeDistance:F2}", this);
            return SideSteer(desiredDirection);
        }

        private Vector3 SideSteer(Vector3 desiredDirection)
        {
            return Quaternion.AngleAxis(sideSteerAngle * avoidanceSide, Vector3.up) * desiredDirection;
        }

        private void EndObstacleAvoidance(string reason, float elapsed)
        {
            if (!avoidingObstacle) return;
            if (debugLocomotion)
                Debug.Log($"[Woodcutter Avoid] {reason} hit={lastAvoidanceHit} side={(avoidanceSide < 0 ? "left" : "right")} duration={elapsed:F2}", this);
            avoidingObstacle = false;
            avoidanceClearSince = -1f;
        }

        private bool DirectionIsBlocked(Vector3 direction, out RaycastHit nearest)
        {
            Vector3 origin = transform.position + Vector3.up * .9f;
            RaycastHit[] hits = Physics.SphereCastAll(origin, obstacleProbeRadius, direction, obstacleProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider is TerrainCollider) continue;
                if (player != null && (hit.collider.transform == player.transform || hit.collider.transform.IsChildOf(player.transform))) continue;
                if (hit.distance < nearestDistance) { nearest = hit; nearestDistance = hit.distance; }
            }
            if (nearestDistance < float.PositiveInfinity) lastAvoidanceHit = nearest.collider.name;
            return nearestDistance < float.PositiveInfinity;
        }

        private float FlatDistance(Vector3 point)
        {
            Vector3 delta = point - transform.position; delta.y = 0f; return delta.magnitude;
        }

        private void UpdateStamina(bool running, float deltaTime)
        {
            if (running)
            {
                stamina = Mathf.Max(0f, stamina - chaseStaminaDrainPerSecond * deltaTime);
                if (stamina <= 0f) runExhausted = true;
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * deltaTime);
                // The threshold may be hit exactly after many small frame updates; allow a tiny
                // tolerance so the woodcutter does not stay falsely exhausted for an extra frame.
                if (runExhausted && stamina >= maxStamina * runRecoveryThreshold - .0001f) runExhausted = false;
            }
        }

        private void ApplyLocomotionAnimation(float movementState)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            animator.SetFloat("Speed", movementState);
            int desiredState = movementState > 1.05f
                ? RunStatePathHash
                : movementState > .05f
                    ? WalkStatePathHash
                    : IdleStatePathHash;

            // Explicitly enter the named state once when locomotion changes. This avoids a
            // transition being missed when CharacterController velocity changes on the same
            // frame as a chase begins, while preserving each clip's normal playback afterward.
            if (desiredState != appliedAnimationStateHash && animator.HasState(0, desiredState))
            {
                animator.CrossFadeInFixedTime(desiredState, .08f, 0);
                appliedAnimationStateHash = desiredState;
            }
        }

        private void LogAnimatorDiagnosis(AnimatorStateInfo current, AnimatorStateInfo next, string reason)
        {
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            string clipSummary = clips.Length == 0 ? "none" : string.Empty;
            for (int i = 0; i < clips.Length; i++)
            {
                if (i > 0) clipSummary += ",";
                clipSummary += $"{clips[i].clip.name}@{clips[i].weight:F2}";
            }

            Debug.Log(
                $"[Woodcutter Animator {reason}] req={RequestedMovementSpeed:F2} lastApplied={LastAnimationSpeed:F2} " +
                $"paramSpeed={animator.GetFloat("Speed"):F2} currentFull={current.fullPathHash} currentShort={current.shortNameHash} " +
                $"nextFull={next.fullPathHash} nextShort={next.shortNameHash} transitioning={animator.IsInTransition(0)} " +
                $"normalized={current.normalizedTime:F3} " +
                $"isIdle={current.IsName("Base Layer.ITHappy Idle")} isWalk={current.IsName("Base Layer.ITHappy Walk")} " +
                $"isRun={current.IsName("Base Layer.ITHappy Run")} " +
                $"hasFull(i/w/r)={animator.HasState(0, IdleStatePathHash)}/{animator.HasState(0, WalkStatePathHash)}/{animator.HasState(0, RunStatePathHash)} " +
                $"hasShort(i/w/r)={animator.HasState(0, IdleStateShortHash)}/{animator.HasState(0, WalkStateShortHash)}/{animator.HasState(0, RunStateShortHash)} " +
                $"clips={clipSummary}", this);
        }

        public static float AnimationSpeedFor(float actualHorizontalSpeed, float requestedSpeed, float walkingSpeed)
        {
            if (requestedSpeed <= .05f) return 0f;
            return requestedSpeed > walkingSpeed + .05f ? 2f : 1f;
        }

        // Deterministic seam for the editor-only numeric verification; production logic calls
        // the same stamina update method from Patrol/Search/Chase.
        public void StepStaminaForEditor(bool running, float deltaTime)
        {
            UpdateStamina(running, deltaTime);
        }

        public void ResetStaminaForEditor()
        {
            stamina = maxStamina;
            runExhausted = false;
        }

        // Test seam: drives the same explicit locomotion-state path used after every movement
        // update, allowing the editor probe to inspect the Animator's real state and active clip.
        public void SetLocomotionStateForEditor(float movementState)
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            ApplyLocomotionAnimation(movementState);
        }

        // Patrol, Search and Chase all call Move(), which resolves this exact steering path.
        // The editor probe uses it to exercise both patrol-speed and chase-speed directions
        // against the same controlled blocker without creating a second avoidance implementation.
        public Vector3 ResolveSteeringForEditor(Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            return direction.sqrMagnitude <= .001f ? Vector3.zero : SteerAroundObstacle(direction.normalized);
        }
    }
}
