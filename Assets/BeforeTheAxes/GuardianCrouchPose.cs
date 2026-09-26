using System.Collections.Generic;
using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>
    /// A small, procedural crouch layer for the supplied humanoid.  The character package does
    /// not include a crouch clip, so this is applied after its locomotion Animator has evaluated.
    /// </summary>
    public sealed class GuardianCrouchPose : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.5f)] private float blendSeconds = 0.14f;
        [SerializeField, Range(0.1f, 0.6f)] private float hipDrop = 0.46f;
        [SerializeField, Range(0f, 0.2f)] private float hipForwardShift = 0.035f;

        private readonly Dictionary<HumanBodyBones, Quaternion> baseRotations = new Dictionary<HumanBodyBones, Quaternion>();
        private Animator animator;
        private Transform hips;
        private Vector3 hipsBasePosition;
        private Transform leftFoot;
        private Transform rightFoot;
        private Vector3 leftFootBaseRootPosition;
        private Vector3 rightFootBaseRootPosition;
        private float blend;
        private bool crouching;
        private bool initialized;

        public bool IsCrouching => crouching;

        public void Configure(Animator sourceAnimator)
        {
            if (sourceAnimator == null || animator == sourceAnimator) return;
            animator = sourceAnimator;
            initialized = false;
        }

        public void SetCrouching(bool value)
        {
            crouching = value;
        }

        private void LateUpdate()
        {
            EvaluateAfterAnimator(Time.deltaTime);
        }

        // Kept separate from LateUpdate so the editor probe can evaluate the exact post-Animator
        // production pose without guessing at transform values.
        private void EvaluateAfterAnimator(float deltaTime)
        {
            Initialize();
            if (animator == null || !animator.isHuman) return;

            float target = crouching ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, deltaTime / Mathf.Max(0.01f, blendSeconds));
            if (blend <= 0f)
            {
                // Capture the current locomotion pose for the next crouch, after the Animator has
                // returned the bones to its own animation.
                CacheBasePose();
                return;
            }

            ApplyPose(blend);
        }

        private void Initialize()
        {
            if (initialized) return;
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman) return;
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            CacheBasePose();
            initialized = true;
        }

        private void CacheBasePose()
        {
            if (animator == null || !animator.isHuman) return;
            baseRotations.Clear();
            CacheRotation(HumanBodyBones.Hips);
            CacheRotation(HumanBodyBones.Spine);
            CacheRotation(HumanBodyBones.Chest);
            CacheRotation(HumanBodyBones.LeftUpperLeg);
            CacheRotation(HumanBodyBones.RightUpperLeg);
            CacheRotation(HumanBodyBones.LeftLowerLeg);
            CacheRotation(HumanBodyBones.RightLowerLeg);
            CacheRotation(HumanBodyBones.LeftUpperArm);
            CacheRotation(HumanBodyBones.RightUpperArm);
            if (hips != null) hipsBasePosition = hips.localPosition;
            if (leftFoot != null) leftFootBaseRootPosition = transform.InverseTransformPoint(leftFoot.position);
            if (rightFoot != null) rightFootBaseRootPosition = transform.InverseTransformPoint(rightFoot.position);
        }

        private void CacheRotation(HumanBodyBones bone)
        {
            Transform transform = animator.GetBoneTransform(bone);
            if (transform != null) baseRotations[bone] = transform.localRotation;
        }

        private void ApplyPose(float weight)
        {
            // This avatar faces local +Z.  Its mirrored thighs need opposite local-Z rotations
            // to send both knees forwards.  The positive-X hip/spine rotations were measured on
            // the rig: both move the chest along +Z, giving a forward stealth crouch rather than
            // a vertical sit-back.
            // A deep crouch needs the knees to travel forward as the pelvis drops.  The larger
            // mirrored thigh bend accomplishes that without pushing the pelvis backwards.
            ApplyRotation(HumanBodyBones.LeftUpperLeg, new Vector3(0f, 0f, 52f), weight);
            ApplyRotation(HumanBodyBones.RightUpperLeg, new Vector3(0f, 0f, -52f), weight);
            ApplyRotation(HumanBodyBones.LeftLowerLeg, new Vector3(20f, 0f, 0f), weight);
            ApplyRotation(HumanBodyBones.RightLowerLeg, new Vector3(20f, 0f, 0f), weight);

            // Keep the upper body near upright.  These small offsets retain a natural balance
            // over the forward knees, rather than the previous pronounced stealth lean.
            ApplyRotation(HumanBodyBones.Hips, new Vector3(3f, 0f, 0f), weight);
            ApplyRotation(HumanBodyBones.Spine, new Vector3(6f, 0f, 0f), weight);
            ApplyRotation(HumanBodyBones.Chest, new Vector3(3f, 0f, 0f), weight);
            ApplyRotation(HumanBodyBones.LeftUpperArm, new Vector3(4f, 0f, 0f), weight);
            ApplyRotation(HumanBodyBones.RightUpperArm, new Vector3(4f, 0f, 0f), weight);
            if (hips != null) hips.localPosition = hipsBasePosition + (Vector3.down * hipDrop + Vector3.forward * hipForwardShift) * weight;

            // Keep both shoes on their Animator-evaluated ground contact points.  This is done
            // after all parent-bone rotations, so lowering the pelvis cannot pull the feet below
            // terrain or leave them visibly floating during the crouch transition.
            if (leftFoot != null) leftFoot.position = transform.TransformPoint(leftFootBaseRootPosition);
            if (rightFoot != null) rightFoot.position = transform.TransformPoint(rightFootBaseRootPosition);
        }

        private void ApplyRotation(HumanBodyBones bone, Vector3 eulerOffset, float weight)
        {
            Transform transform = animator.GetBoneTransform(bone);
            if (transform == null || !baseRotations.TryGetValue(bone, out Quaternion baseRotation)) return;
            Quaternion target = baseRotation * Quaternion.Euler(eulerOffset);
            transform.localRotation = Quaternion.Slerp(baseRotation, target, weight);
        }
    }
}
