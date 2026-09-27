using System.Reflection;
using System.Collections.Generic;
using System.IO;
using BeforeTheAxes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeforeTheAxes.Editor
{
    [InitializeOnLoad]
    public static class WoodcutterPatrolProbe
    {
        private static WoodcutterAI woodcutter;
        private static float nextSample;
        private static Vector3 previousPosition;
        private static int samples;
        private static ForestGuardianController player;
        private static bool switchedToWalk;
        private static float walkStartedAt;
        private static bool runtimeProbeRequested;

        // Opt-in endurance trace. This is deliberately separate from the short state probe:
        // it keeps the real scene running for 20 seconds and writes compact, inspectable
        // measurements instead of guessing from parameters.
        private static bool twentySecondTraceRequested;
        private static bool avoidanceProbeRequested;
        private static readonly List<string> traceLines = new List<string>();
        private static readonly List<string> traceViolations = new List<string>();
        private static string tracePath;
        private static float traceStartedAt;
        private static float traceNextSampleAt;
        private static float tracePreviousSampleAt;
        private static Vector3 tracePreviousPosition;
        private static Quaternion tracePreviousLegRotation;
        private static bool traceHasPreviousLegRotation;
        private static int tracePreviousStateHash;
        private static float tracePreviousNormalized;
        private static float traceLastNormalizedAdvanceAt;
        private static float traceMovingWrongStateSince = -1f;
        private static float traceMovingIdleSince = -1f;
        private static float traceRootStuckSince = -1f;
        private static bool traceLastRootStuckWasAnimation;
        private static int traceSampleCount;
        private static GameObject avoidanceProbeObstacle;
        private static float avoidanceProbeStartedAt;
        private static bool avoidanceProbeRemoved;
        private static string avoidanceProbePath;
        private static readonly List<string> avoidanceProbeLines = new List<string>();
        private static float avoidanceProbeNextSampleAt;
        private static Vector3 avoidanceProbeDirection;
        private static bool avoidanceProbePatrolFed;
        private static bool avoidanceProbeChaseFed;
        private static Transform[] tracePoseBones;
        private static Quaternion[] tracePreviousPoseRotations;
        private static bool traceHasPreviousPose;
        private static float tracePoseStillSince = -1f;

        static WoodcutterPatrolProbe()
        {
            runtimeProbeRequested = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-woodcutter-runtime-probe") >= 0;
            twentySecondTraceRequested = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-woodcutter-20s-trace") >= 0;
            avoidanceProbeRequested = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-woodcutter-avoidance-probe") >= 0;
            if (!runtimeProbeRequested && !twentySecondTraceRequested && !avoidanceProbeRequested) return;
            // The editor domain reloads on entering Play Mode. This initializer runs again after
            // that reload, which is why it can attach to the real game loop rather than a stale
            // pre-play callback.
            EditorApplication.delayCall += () =>
            {
                if (avoidanceProbeRequested)
                {
                    if (EditorApplication.isPlaying) AttachAvoidanceProbe();
                    else BeginAvoidanceProbe();
                }
                else if (twentySecondTraceRequested)
                {
                    if (EditorApplication.isPlaying) AttachTwentySecondTrace();
                    else BeginTwentySecondTrace();
                }
                else if (EditorApplication.isPlaying) AttachRuntimeProbe();
                else BeginRuntimeProbe();
            };
        }

        private static void BeginAvoidanceProbe()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PlayableForest.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void AttachAvoidanceProbe()
        {
            woodcutter = Object.FindFirstObjectByType<WoodcutterAI>();
            player = Object.FindFirstObjectByType<ForestGuardianController>();
            if (woodcutter == null || player == null) throw new System.InvalidOperationException("WOODCUTTER_AVOIDANCE_PROBE missing actors");
            player.transform.position = woodcutter.transform.position - woodcutter.transform.forward * 1000f;
            player.enabled = false;
            woodcutter.enabled = false; // Manual calls below isolate the shared Move steering resolver.
            avoidanceProbeDirection = woodcutter.transform.right;
            avoidanceProbeObstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            avoidanceProbeObstacle.name = "Avoidance Probe Tree";
            avoidanceProbeObstacle.transform.position = woodcutter.transform.position + avoidanceProbeDirection * .75f + Vector3.up * .8f;
            avoidanceProbeObstacle.transform.localScale = new Vector3(.5f, 1.6f, .5f);
            avoidanceProbeStartedAt = Time.time;
            avoidanceProbeRemoved = false;
            avoidanceProbeLines.Clear();
            avoidanceProbePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "WoodcutterAvoidanceProbe.log"));
            Directory.CreateDirectory(Path.GetDirectoryName(avoidanceProbePath));
            avoidanceProbeNextSampleAt = .4f;
            avoidanceProbePatrolFed = false;
            avoidanceProbeChaseFed = false;
            EditorApplication.update -= AvoidanceProbeTick;
            EditorApplication.update += AvoidanceProbeTick;
        }

        private static void AvoidanceProbeTick()
        {
            float elapsed = Time.time - avoidanceProbeStartedAt;
            if (!avoidanceProbePatrolFed && elapsed >= .4f)
            {
                woodcutter.ResolveSteeringForEditor(avoidanceProbeDirection);
                avoidanceProbePatrolFed = true;
                avoidanceProbeLines.Add($"t={elapsed:F2} phase=patrol-input hit={woodcutter.LastAvoidanceHit} side={woodcutter.LastAvoidanceSide}");
            }
            if (!avoidanceProbeChaseFed && elapsed >= .95f)
            {
                woodcutter.ResolveSteeringForEditor(avoidanceProbeDirection);
                avoidanceProbeChaseFed = true;
                avoidanceProbeLines.Add($"t={elapsed:F2} phase=chase-input hit={woodcutter.LastAvoidanceHit} side={woodcutter.LastAvoidanceSide}");
            }
            if (!avoidanceProbeRemoved && elapsed >= 1.2f)
            {
                Object.Destroy(avoidanceProbeObstacle);
                avoidanceProbeRemoved = true;
                avoidanceProbeLines.Add($"t={elapsed:F2} obstacle=removed");
            }
            if (elapsed < avoidanceProbeNextSampleAt) return;
            avoidanceProbeLines.Add($"t={elapsed:F2} avoiding={woodcutter.IsAvoidingObstacle} count={woodcutter.AvoidanceCount} hit={woodcutter.LastAvoidanceHit} side={woodcutter.LastAvoidanceSide}");
            avoidanceProbeNextSampleAt += .2f;
            if (elapsed < 1.8f) return;
            bool hit = avoidanceProbePatrolFed && avoidanceProbeChaseFed && woodcutter.AvoidanceCount >= 2 && woodcutter.LastAvoidanceHit == "Avoidance Probe Tree";
            bool cleared = !woodcutter.IsAvoidingObstacle;
            avoidanceProbeLines.Add($"SUMMARY hit={hit} cleared={cleared} count={woodcutter.AvoidanceCount}");
            File.WriteAllLines(avoidanceProbePath, avoidanceProbeLines);
            EditorApplication.update -= AvoidanceProbeTick;
            if (!hit || !cleared) throw new System.InvalidOperationException("WOODCUTTER_AVOIDANCE_PROBE failed hit/clear contract");
            EditorApplication.Exit(0);
        }

        private static void BeginTwentySecondTrace()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PlayableForest.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void AttachTwentySecondTrace()
        {
            woodcutter = Object.FindFirstObjectByType<WoodcutterAI>();
            player = Object.FindFirstObjectByType<ForestGuardianController>();
            if (woodcutter == null || player == null)
                throw new System.InvalidOperationException("WOODCUTTER_20S_TRACE missing scene actors");

            // Keep the real AI in uninterrupted Patrol. The disabled controller prevents the
            // player from falling or colliding into view while the woodcutter covers corners.
            player.transform.position = woodcutter.transform.position - woodcutter.transform.forward * 1000f;
            player.enabled = false;

            string logsDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(logsDirectory);
            tracePath = Path.Combine(logsDirectory, "Woodcutter20sTrace.log");
            traceLines.Clear();
            traceViolations.Clear();
            traceViolationKinds.Clear();
            traceStartedAt = Time.time;
            traceNextSampleAt = Time.time + .2f;
            tracePreviousSampleAt = Time.time;
            tracePreviousPosition = woodcutter.transform.position;
            tracePreviousStateHash = int.MinValue;
            tracePreviousNormalized = 0f;
            traceLastNormalizedAdvanceAt = Time.time;
            traceMovingWrongStateSince = -1f;
            traceMovingIdleSince = -1f;
            traceRootStuckSince = -1f;
            traceHasPreviousLegRotation = false;
            Animator traceAnimator = woodcutter.GetComponentInChildren<Animator>();
            tracePoseBones = new[]
            {
                traceAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                traceAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                traceAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                traceAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                traceAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                traceAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm)
            };
            tracePreviousPoseRotations = new Quaternion[tracePoseBones.Length];
            traceHasPreviousPose = false;
            tracePoseStillSince = -1f;
            traceSampleCount = 0;
            traceLines.Add("WOODCUTTER_20S_TRACE begin duration=20.0 sample=0.2 player=1000m-away mode=real-play");
            EditorApplication.update -= TwentySecondTraceTick;
            EditorApplication.update += TwentySecondTraceTick;
        }

        private static void TwentySecondTraceTick()
        {
            if (woodcutter == null) return;
            if (Time.time < traceNextSampleAt) return;

            Animator animator = woodcutter.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                RecordTraceViolation("missing Animator");
                FinishTwentySecondTrace();
                return;
            }

            float now = Time.time;
            float elapsed = now - traceStartedAt;
            float sampleDeltaTime = Mathf.Max(.0001f, now - tracePreviousSampleAt);
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool idle = state.IsName("Base Layer.ITHappy Idle");
            bool walk = state.IsName("Base Layer.ITHappy Walk");
            bool run = state.IsName("Base Layer.ITHappy Run");
            float requested = woodcutter.RequestedMovementSpeed;
            float applied = woodcutter.LastAnimationSpeed;
            Vector3 position = woodcutter.transform.position;
            CharacterController controller = woodcutter.GetComponent<CharacterController>();
            Vector3 target = GetCurrentPatrolTarget();
            Vector3 rootDelta = position - tracePreviousPosition;
            rootDelta.y = 0f;
            Transform leg = FindLeg(woodcutter.transform);
            float legPoseDelta = 0f;
            if (leg != null)
            {
                if (traceHasPreviousLegRotation) legPoseDelta = Quaternion.Angle(tracePreviousLegRotation, leg.localRotation);
                tracePreviousLegRotation = leg.localRotation;
                traceHasPreviousLegRotation = true;
            }

            bool transition = animator.IsInTransition(0);
            bool movingRequest = requested > .05f;
            bool patrolRequest = movingRequest && requested <= 1.30f;
            bool runRequest = requested > 1.30f;
            bool stateExpected = !movingRequest ? idle : patrolRequest ? walk : run;
            float animatorSpeedParameter = animator.GetFloat("Speed");
            float[] poseDeltas = SampleAnimatedPoseDeltas();
            float poseTotalDelta = 0f;
            for (int i = 0; i < poseDeltas.Length; i++) poseTotalDelta += poseDeltas[i];
            bool normalizedChanging = state.fullPathHash != tracePreviousStateHash || Mathf.Abs(state.normalizedTime - tracePreviousNormalized) > .0005f;
            UpdateFrozenPose(now, (walk || run) && !transition, normalizedChanging, poseTotalDelta);
            UpdateWrongStateTimers(now, movingRequest, patrolRequest, runRequest, idle, walk, run, transition);
            UpdateNormalizedAdvance(now, state, transition);
            UpdateRootStuck(now, sampleDeltaTime, movingRequest, rootDelta.magnitude, legPoseDelta, transition, controller);

            int visibleRenderers = 0;
            Renderer[] renderers = woodcutter.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) if (renderer.enabled && renderer.isVisible) visibleRenderers++;
            traceLines.Add($"t={elapsed:F2} ai={woodcutter.State} req={requested:F2} applied={applied:F2} param={animatorSpeedParameter:F2} hash={state.fullPathHash} idle={idle} walk={walk} run={run} expected={stateExpected} trans={transition} norm={state.normalizedTime:F3} animEnabled={animator.enabled} culling={animator.cullingMode} renderers={visibleRenderers}/{renderers.Length} avoiding={woodcutter.IsAvoidingObstacle} avoidCount={woodcutter.AvoidanceCount} pos=({position.x:F2},{position.y:F2},{position.z:F2}) target=({target.x:F2},{target.y:F2},{target.z:F2}) ccVel={controller.velocity.magnitude:F3} flags={controller.collisionFlags} rootDelta={rootDelta.magnitude:F3} lUp={poseDeltas[0]:F2} lLow={poseDeltas[1]:F2} rUp={poseDeltas[2]:F2} rLow={poseDeltas[3]:F2} lArm={poseDeltas[4]:F2} rArm={poseDeltas[5]:F2} poseTotal={poseTotalDelta:F2} legDelta={legPoseDelta:F2}");
            traceSampleCount++;
            tracePreviousPosition = position;
            tracePreviousSampleAt = now;
            traceNextSampleAt += .2f;
            if (elapsed >= 20f) FinishTwentySecondTrace();
        }

        private static void UpdateWrongStateTimers(float now, bool movingRequest, bool patrolRequest, bool runRequest, bool idle, bool walk, bool run, bool transition)
        {
            // A CrossFade is allowed .08s. Every classification below has a larger grace so
            // only a state which genuinely remains wrong becomes a violation.
            bool wrongMoving = movingRequest && !transition && !(patrolRequest ? walk : runRequest ? run : false);
            if (wrongMoving)
            {
                if (traceMovingWrongStateSince < 0f) traceMovingWrongStateSince = now;
                if (now - traceMovingWrongStateSince >= .08f) RecordTraceViolationOnce("movement-state", $"wrong movement state for {now - traceMovingWrongStateSince:F2}s req={(patrolRequest ? "patrol" : "run")}");
            }
            else traceMovingWrongStateSince = -1f;

            bool movingIdle = movingRequest && idle && !transition;
            if (movingIdle)
            {
                if (traceMovingIdleSince < 0f) traceMovingIdleSince = now;
                if (now - traceMovingIdleSince >= .2f) RecordTraceViolationOnce("moving-idle", $"moving request held Idle for {now - traceMovingIdleSince:F2}s");
            }
            else traceMovingIdleSince = -1f;
        }

        private static void UpdateNormalizedAdvance(float now, AnimatorStateInfo state, bool transition)
        {
            if (state.fullPathHash != tracePreviousStateHash || transition || Mathf.Abs(state.normalizedTime - tracePreviousNormalized) > .0005f)
                traceLastNormalizedAdvanceAt = now;
            else if (now - traceLastNormalizedAdvanceAt >= .6f)
                RecordTraceViolationOnce("normalized-stall", $"normalizedTime stayed {state.normalizedTime:F3} for {now - traceLastNormalizedAdvanceAt:F2}s");
            tracePreviousStateHash = state.fullPathHash;
            tracePreviousNormalized = state.normalizedTime;
        }

        private static float[] SampleAnimatedPoseDeltas()
        {
            float[] deltas = new float[tracePoseBones.Length];
            for (int i = 0; i < tracePoseBones.Length; i++)
            {
                Transform bone = tracePoseBones[i];
                if (bone == null) continue;
                if (traceHasPreviousPose) deltas[i] = Quaternion.Angle(tracePreviousPoseRotations[i], bone.localRotation);
                tracePreviousPoseRotations[i] = bone.localRotation;
            }
            traceHasPreviousPose = true;
            return deltas;
        }

        private static void UpdateFrozenPose(float now, bool locomotionState, bool normalizedChanging, float poseTotalDelta)
        {
            // .2 degrees total across six primary limbs is far below the movement visible at a
            // .2 second sample. It must persist .8 seconds while normalized time advances before
            // we label the rendered pose frozen.
            bool visuallyFrozen = locomotionState && normalizedChanging && poseTotalDelta < .2f;
            if (visuallyFrozen)
            {
                if (tracePoseStillSince < 0f) tracePoseStillSince = now;
                if (now - tracePoseStillSince >= .8f)
                    RecordTraceViolationOnce("visual-pose-frozen", $"Walk/Run normalized time advances but six-bone pose delta stayed {poseTotalDelta:F3}deg for {now - tracePoseStillSince:F2}s");
            }
            else tracePoseStillSince = -1f;
        }

        private static void UpdateRootStuck(float now, float sampleDeltaTime, bool movingRequest, float rootDistance, float legPoseDelta, bool transition, CharacterController controller)
        {
            // At patrol speed an .2s sample should cover ~.25m. .015m allows steep terrain
            // and controller settling without masking a full second of no world movement.
            bool rootStuck = movingRequest && !transition && rootDistance < .015f;
            if (rootStuck)
            {
                if (traceRootStuckSince < 0f)
                {
                    traceRootStuckSince = now - sampleDeltaTime;
                    traceLastRootStuckWasAnimation = legPoseDelta > .25f;
                }
                if (now - traceRootStuckSince >= 1f)
                {
                    string kind = traceLastRootStuckWasAnimation ? "collision/root-blocked" : "animation-or-controller-stall";
                    RecordTraceViolationOnce("root-stuck", $"root moved {rootDistance:F3}m while requested movement for {now - traceRootStuckSince:F2}s classification={kind} contacts={DescribeContacts(controller)}");
                }
            }
            else traceRootStuckSince = -1f;
        }

        private static Vector3 GetCurrentPatrolTarget()
        {
            FieldInfo pointsField = typeof(WoodcutterAI).GetField("patrolPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo indexField = typeof(WoodcutterAI).GetField("patrolIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            Transform[] points = (Transform[])pointsField.GetValue(woodcutter);
            int index = (int)indexField.GetValue(woodcutter);
            return points != null && index >= 0 && index < points.Length ? points[index].position : Vector3.zero;
        }

        private static string DescribeContacts(CharacterController controller)
        {
            if (controller == null) return "no-controller";
            float radius = Mathf.Max(.01f, controller.radius - .01f);
            Vector3 center = controller.transform.TransformPoint(controller.center);
            float halfLine = Mathf.Max(0f, controller.height * .5f - radius);
            Collider[] overlaps = Physics.OverlapCapsule(center + Vector3.up * halfLine, center - Vector3.up * halfLine, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            List<string> names = new List<string>();
            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.transform.IsChildOf(controller.transform)) continue;
                string name = overlap.name;
                if (!names.Contains(name)) names.Add(name);
            }
            return names.Count == 0 ? "none" : string.Join("|", names);
        }

        private static readonly HashSet<string> traceViolationKinds = new HashSet<string>();
        private static void RecordTraceViolationOnce(string kind, string detail)
        {
            if (traceViolationKinds.Add(kind)) RecordTraceViolation(detail);
        }

        private static void RecordTraceViolation(string detail)
        {
            string line = $"VIOLATION t={Time.time - traceStartedAt:F2} {detail}";
            traceViolations.Add(line);
            traceLines.Add(line);
        }

        private static void FinishTwentySecondTrace()
        {
            EditorApplication.update -= TwentySecondTraceTick;
            traceLines.Add($"SUMMARY samples={traceSampleCount} duration={Time.time - traceStartedAt:F2} violations={traceViolations.Count}");
            foreach (string violation in traceViolations) traceLines.Add("SUMMARY " + violation);
            File.WriteAllLines(tracePath, traceLines);
            Debug.Log($"WOODCUTTER_20S_TRACE complete samples={traceSampleCount} violations={traceViolations.Count} path={tracePath}");
            EditorApplication.Exit(0);
        }

        private static void BeginRuntimeProbe()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PlayableForest.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void AttachRuntimeProbe()
        {
            woodcutter = Object.FindFirstObjectByType<WoodcutterAI>();
            player = Object.FindFirstObjectByType<ForestGuardianController>();
            if (woodcutter == null || player == null)
                throw new System.InvalidOperationException("WOODCUTTER_RUNTIME_PROBE missing scene actors");
            CharacterController playerController = player.GetComponent<CharacterController>();
            if (playerController != null) playerController.enabled = false;
            player.transform.position = woodcutter.transform.position + woodcutter.transform.forward * 8f;
            previousPosition = woodcutter.transform.position;
            nextSample = Time.time + .2f;
            samples = 0;
            switchedToWalk = false;
            walkStartedAt = -1f;
            EditorApplication.update -= RuntimeTick;
            EditorApplication.update += RuntimeTick;
            Debug.Log("WOODCUTTER_RUNTIME_PROBE attached phase=run");
        }

        private static void RuntimeTick()
        {
            if (woodcutter == null || Time.time < nextSample) return;
            if (!switchedToWalk && samples >= 5)
            {
                player.transform.position = woodcutter.transform.position - woodcutter.transform.forward * 100f;
                switchedToWalk = true;
                walkStartedAt = Time.time;
                Debug.Log("WOODCUTTER_RUNTIME_PROBE phase=walk");
            }
            Animator animator = woodcutter.GetComponentInChildren<Animator>();
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            string expected = switchedToWalk ? "Base Layer.ITHappy Walk" : "Base Layer.ITHappy Run";
            bool correctState = state.IsName(expected);
            Debug.Log($"WOODCUTTER_RUNTIME_PROBE sample={samples} phase={(switchedToWalk ? "walk" : "run")} req={woodcutter.RequestedMovementSpeed:F2} applied={woodcutter.LastAnimationSpeed:F2} param={animator.GetFloat("Speed"):F2} animatorSpeed={animator.speed:F2} full={state.fullPathHash} isExpected={correctState} transition={animator.IsInTransition(0)} normalized={state.normalizedTime:F3}");
            // The editor update callback can run before the scene's Update on the exact switch
            // frame. Give the real AI update plus its .08 second CrossFade a .30 second window;
            // after that, a wrong evaluated state is a genuine failure.
            if (!correctState && switchedToWalk && Time.time - walkStartedAt < .30f)
            {
                nextSample = Time.time + .05f;
                return;
            }
            if (!correctState) throw new System.InvalidOperationException($"WOODCUTTER_RUNTIME_PROBE FAIL expected {expected}");
            previousPosition = woodcutter.transform.position;
            nextSample += .2f;
            if (++samples < 12) return;
            Debug.Log("WOODCUTTER_RUNTIME_PROBE PASS");
            EditorApplication.update -= RuntimeTick;
            EditorApplication.Exit(0);
        }

        // This is deliberately an editor-batch probe, not a parameter-only unit test. It
        // samples the Animator state after Unity has evaluated the same CrossFade path the
        // woodcutter uses at runtime. Run with -executeMethod
        // BeforeTheAxes.Editor.WoodcutterPatrolProbe.RunBatch.
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PlayableForest.unity");
            WoodcutterAI ai = Object.FindFirstObjectByType<WoodcutterAI>();
            if (ai == null) throw new System.InvalidOperationException("WOODCUTTER_ANIM_PROBE missing WoodcutterAI");

            Animator animator = ai.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new System.InvalidOperationException("WOODCUTTER_ANIM_PROBE missing Animator");

            animator.Rebind();
            animator.Update(0f);
            SampleAndAssert(ai, animator, 1f, "walk", "Base Layer.ITHappy Walk");
            SampleAndAssert(ai, animator, 2f, "run", "Base Layer.ITHappy Run");
            Debug.Log("WOODCUTTER_ANIM_PROBE PASS");
            EditorApplication.Exit(0);
        }

        private static void SampleAndAssert(WoodcutterAI ai, Animator animator, float appliedSpeed, string phase, string expectedName)
        {
            ai.SetLocomotionStateForEditor(appliedSpeed);
            // CrossFadeInFixedTime is .08 seconds, therefore .12 deterministically completes it.
            animator.Update(.12f);
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool expected = state.IsName(expectedName);
            Debug.Log($"WOODCUTTER_ANIM_PROBE phase={phase} requested={appliedSpeed:F2} applied={ai.LastAnimationSpeed:F2} param={animator.GetFloat("Speed"):F2} full={state.fullPathHash} short={state.shortNameHash} isExpected={expected} transition={animator.IsInTransition(0)} normalized={state.normalizedTime:F3}");
            if (!expected)
                throw new System.InvalidOperationException($"WOODCUTTER_ANIM_PROBE FAIL phase={phase}: expected {expectedName}, got full={state.fullPathHash}");
        }

        public static void Run()
        {
            Debug.Log("PATROL_PROBE queued");
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.delayCall += StartPlay;
        }

        private static void StartPlay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PlayableForest.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            woodcutter = Object.FindFirstObjectByType<WoodcutterAI>();
            player = Object.FindFirstObjectByType<ForestGuardianController>();
            if (woodcutter == null) { Debug.LogError("PATROL_PROBE missing woodcutter"); Stop(); return; }
            if (player == null) { Debug.LogError("PATROL_PROBE missing player"); Stop(); return; }
            CharacterController playerController = player.GetComponent<CharacterController>();
            if (playerController != null) playerController.enabled = false;
            // Put a human in the woodcutter's direct forward view: actual AI Chase must request Run.
            player.transform.position = woodcutter.transform.position + woodcutter.transform.forward * 8f;
            previousPosition = woodcutter.transform.position;
            nextSample = Time.time;
            EditorApplication.update += Tick;
            Debug.Log("ANIMATOR_PROBE playing phase=run");
        }

        private static void Tick()
        {
            if (woodcutter == null || Time.time < nextSample) return;
            if (!switchedToWalk && Time.time >= 1.25f)
            {
                // Break line of sight while keeping the last-known position: actual AI Search must walk.
                player.transform.position = woodcutter.transform.position - woodcutter.transform.forward * 100f;
                switchedToWalk = true;
                Debug.Log("ANIMATOR_PROBE phase=walk");
            }
            CharacterController controller = woodcutter.GetComponent<CharacterController>();
            Animator animator = woodcutter.GetComponentInChildren<Animator>();
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            Transform leg = FindLeg(woodcutter.transform);
            Vector3 position = woodcutter.transform.position;
            Vector3 delta = position - previousPosition;
            Vector3 desired = GetDesiredDirection();
            Debug.Log($"ANIMATOR_PROBE t={Time.time:F2} phase={(switchedToWalk ? "walk" : "run")} pos={position:F3} delta={delta:F3} desired={desired:F3} requested={woodcutter.RequestedMovementSpeed:F2} applied={woodcutter.LastAnimationSpeed:F2} param={animator.GetFloat("Speed"):F2} full={state.fullPathHash} short={state.shortNameHash} walk={state.IsName("Base Layer.ITHappy Walk")} run={state.IsName("Base Layer.ITHappy Run")} transition={animator.IsInTransition(0)} norm={state.normalizedTime:F3} legX={(leg == null ? -999f : leg.localEulerAngles.x):F2}");
            previousPosition = position;
            nextSample += .25f;
            if (++samples >= 12) Stop();
        }

        private static Vector3 GetDesiredDirection()
        {
            FieldInfo pointsField = typeof(WoodcutterAI).GetField("patrolPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo indexField = typeof(WoodcutterAI).GetField("patrolIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            Transform[] points = (Transform[])pointsField.GetValue(woodcutter);
            int index = (int)indexField.GetValue(woodcutter);
            Vector3 direction = points[index].position - woodcutter.transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.zero;
        }

        private static Transform FindLeg(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                if (child.name.ToLowerInvariant().Contains("leg")) return child;
            return null;
        }

        private static void Stop()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }
}
