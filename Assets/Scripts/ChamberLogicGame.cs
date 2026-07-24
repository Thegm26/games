using System.Collections;
using System.Collections.Generic;
using ChamberLogic;
using UnityEngine;

public sealed class ChamberLogicGame : MonoBehaviour
{
    private const int StartingHealth = 3;

    [Header("Authored scene references")]
    [SerializeField] private Transform playerWeapon;
    [SerializeField] private Transform dealerEntity;
    [SerializeField] private Transform dealerRightHand;
    [SerializeField] private Transform dealerLeftHand;
    [SerializeField] private Transform dealerRightGrip;
    [SerializeField] private Transform dealerLeftGrip;
    [SerializeField] private Transform dealerSelfRightGrip;
    [SerializeField] private Transform dealerSelfLeftGrip;
    [SerializeField] private Transform weaponTableAnchor;
    [SerializeField] private Transform openingLoadAnchor;
    [SerializeField] private Transform shellTray;
    [SerializeField] private Transform trayCarrier;
    [SerializeField] private Transform shellRevealCameraAnchor;
    [SerializeField] private Transform openingPumpHandGrip;
    [SerializeField] private Transform playerAimDealerAnchor;
    [SerializeField] private Transform playerAimSelfAnchor;
    [SerializeField] private Transform dealerAimPlayerAnchor;
    [SerializeField] private Transform dealerAimSelfAnchor;
    [SerializeField] private Transform weaponPump;
    [SerializeField] private Transform duelCamera;
    [SerializeField] private Light muzzleFlash;
    [SerializeField] private Light overheadLight;
    [SerializeField] private Light dealerRimLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource mechanicalSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource musicLayerSource;
    [SerializeField] private AudioSource dollMusicSource;
    [SerializeField] private AudioSource dollVoiceSource;
    [SerializeField] private AudioClip horrorMusicClip;
    [SerializeField] private AudioClip horrorMusicLayerClip;
    [SerializeField] private AudioClip dollMusicClip;
    [SerializeField] private AudioClip dollVoiceClip;
    [SerializeField] private AudioClip dollFallClip;
    [SerializeField] private AudioClip liveShotClip;
    [SerializeField] private AudioClip blankClickClip;
    [SerializeField] private AudioClip shellLoadClip;
    [SerializeField] private TextMesh roundRevealText;
    [SerializeField] private List<GameObject> shellProps = new List<GameObject>();

    private ChamberRound round;
    private readonly List<string> report = new List<string>();
    private readonly List<Vector3> shellRestPositions = new List<Vector3>();
    private readonly List<Quaternion> shellRestRotations = new List<Quaternion>();
    private readonly List<Transform> shellRestParents = new List<Transform>();
    private readonly System.Random seedSource = new System.Random();
    private int playerHealth;
    private int dealerHealth;
    private bool playerTurn;
    private bool resolving;
    private bool introPlaying;
    private Vector3 cameraRestPosition;
    private Quaternion cameraRestRotation;
    private Vector3 entityRestPosition;
    private Quaternion entityRestRotation;
    private Vector3 entityRestScale;
    private Transform rightHandRestParent;
    private Transform leftHandRestParent;
    private Vector3 rightHandRestPosition;
    private Vector3 leftHandRestPosition;
    private Quaternion rightHandRestRotation;
    private Quaternion leftHandRestRotation;
    private bool lastDealerHitWasSelfInflicted;
    private Vector3 pumpRestPosition;
    private Quaternion pumpRestRotation;
    private Transform trayCarrierRestParent;
    private Vector3 trayCarrierRestPosition;
    private Quaternion trayCarrierRestRotation;
    private HandPoseRig rightHandRig;
    private HandPoseRig leftHandRig;

    private sealed class HandPoseRig
    {
        public readonly Transform[] fingerBones;
        public readonly Quaternion[] fingerRestRotations;
        public readonly Transform[] thumbBones;
        public readonly Quaternion[] thumbRestRotations;

        public HandPoseRig(Transform root)
        {
            fingerBones = new[]
            {
                FindDescendant(root, "IndexFinger"), FindDescendant(root, "Index2"), FindDescendant(root, "Index3"),
                FindDescendant(root, "MiddleFinger"), FindDescendant(root, "Middle2"), FindDescendant(root, "Middle3"),
                FindDescendant(root, "RingFinger"), FindDescendant(root, "Ring2"), FindDescendant(root, "Ring3"),
                FindDescendant(root, "LittleFinger"), FindDescendant(root, "Little2"), FindDescendant(root, "Little3")
            };
            thumbBones = new[] { FindDescendant(root, "Thumb"), FindDescendant(root, "Thumb2") };
            fingerRestRotations = CaptureRotations(fingerBones);
            thumbRestRotations = CaptureRotations(thumbBones);
        }

        public bool IsComplete
        {
            get
            {
                foreach (var bone in fingerBones) if (bone == null) return false;
                foreach (var bone in thumbBones) if (bone == null) return false;
                return true;
            }
        }
    }

    private void Awake()
    {
        if (duelCamera == null || playerWeapon == null || dealerEntity == null || dealerRightHand == null || dealerLeftHand == null ||
            dealerRightGrip == null || dealerLeftGrip == null || dealerSelfRightGrip == null || dealerSelfLeftGrip == null ||
            weaponTableAnchor == null || openingLoadAnchor == null || shellTray == null || trayCarrier == null ||
            shellRevealCameraAnchor == null || openingPumpHandGrip == null ||
            playerAimDealerAnchor == null || playerAimSelfAnchor == null ||
            dealerAimPlayerAnchor == null || dealerAimSelfAnchor == null || weaponPump == null ||
            audioSource == null || mechanicalSource == null || musicSource == null || musicLayerSource == null ||
            dollMusicSource == null || dollVoiceSource == null || horrorMusicClip == null || horrorMusicLayerClip == null ||
            dollMusicClip == null || dollVoiceClip == null || dollFallClip == null || liveShotClip == null ||
            blankClickClip == null || shellLoadClip == null || shellProps.Count != 6)
        {
            Debug.LogError("Chamber scene references are incomplete. The saved Chamber scene needs repair.");
            enabled = false;
            return;
        }

        cameraRestPosition = duelCamera.localPosition;
        cameraRestRotation = duelCamera.localRotation;
        entityRestPosition = dealerEntity.localPosition;
        entityRestRotation = dealerEntity.localRotation;
        entityRestScale = dealerEntity.localScale;
        rightHandRestParent = dealerRightHand.parent;
        leftHandRestParent = dealerLeftHand.parent;
        rightHandRestPosition = dealerRightHand.localPosition;
        leftHandRestPosition = dealerLeftHand.localPosition;
        rightHandRestRotation = dealerRightHand.localRotation;
        leftHandRestRotation = dealerLeftHand.localRotation;
        rightHandRig = new HandPoseRig(dealerRightHand);
        leftHandRig = new HandPoseRig(dealerLeftHand);
        if (!rightHandRig.IsComplete || !leftHandRig.IsComplete)
        {
            Debug.LogError("The saved doll hands are missing finger bones. Reimport the Simple Hands package.");
            enabled = false;
            return;
        }
        pumpRestPosition = weaponPump.localPosition;
        pumpRestRotation = weaponPump.localRotation;
        trayCarrierRestParent = trayCarrier.parent;
        trayCarrierRestPosition = trayCarrier.localPosition;
        trayCarrierRestRotation = trayCarrier.localRotation;
        foreach (var shell in shellProps)
        {
            if (shell == null)
            {
                Debug.LogError("The tray carrier needs exactly six assigned shell props.");
                enabled = false;
                return;
            }
            shellRestParents.Add(shell.transform.parent);
            shellRestPositions.Add(shell.transform.localPosition);
            shellRestRotations.Add(shell.transform.localRotation);
        }

        musicSource.clip = horrorMusicClip;
        musicSource.loop = true;
        musicSource.volume = 0.24f;
        musicSource.Play();
        musicLayerSource.clip = horrorMusicLayerClip;
        musicLayerSource.loop = true;
        musicLayerSource.volume = 0.14f;
        musicLayerSource.Play();
        dollMusicSource.clip = dollMusicClip;
        dollMusicSource.loop = true;
        dollMusicSource.volume = 0.12f;
        dollMusicSource.Play();
        StartExperiment();
    }

    private void Update()
    {
        var time = Time.unscaledTime;
        if (overheadLight != null)
        {
            var unstable = Mathf.PerlinNoise(time * 7.5f, 0.37f);
            var dropout = Mathf.PerlinNoise(time * 1.3f, 4.1f) > 0.91f ? 0.22f : 1f;
            overheadLight.intensity = (3.0f + unstable * 1.25f) * Mathf.Max(dropout, 0.68f);
        }
        if (dealerRimLight != null) dealerRimLight.intensity = 3.35f + Mathf.Sin(time * 0.72f) * 0.5f;

        if (!introPlaying)
        {
            duelCamera.localPosition = cameraRestPosition + new Vector3(Mathf.Sin(time * 0.37f) * 0.008f, Mathf.Sin(time * 0.83f) * 0.012f, 0f);
            duelCamera.localRotation = cameraRestRotation * Quaternion.Euler(Mathf.Sin(time * 0.44f) * 0.18f, Mathf.Sin(time * 0.29f) * 0.22f, 0f);
        }

        if (!introPlaying && !resolving && playerTurn && round != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseDealer();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseSelf();
        }
        if (Input.GetKeyDown(KeyCode.R)) StartExperiment();
    }

    private void StartExperiment()
    {
        StopAllCoroutines();
        round = new ChamberRound(2, 4, seedSource.Next());
        playerHealth = StartingHealth;
        dealerHealth = StartingHealth;
        playerTurn = false;
        resolving = true;
        introPlaying = true;
        report.Clear();
        report.Add("Round begins: 2 live and 4 blank shells.");
        lastDealerHitWasSelfInflicted = false;
        ResetDealerEntity();
        ReleaseDealerHands();
        SetWeaponAt(weaponTableAnchor);
        weaponPump.localPosition = pumpRestPosition;
        weaponPump.localRotation = pumpRestRotation;
        trayCarrier.gameObject.SetActive(true);
        trayCarrier.SetParent(trayCarrierRestParent, false);
        trayCarrier.localPosition = trayCarrierRestPosition;
        trayCarrier.localRotation = trayCarrierRestRotation;
        ResetShellReveal();
        StartCoroutine(OpeningSequence());
    }

    private IEnumerator OpeningSequence()
    {
        playerWeapon.gameObject.SetActive(true);
        if (roundRevealText != null)
        {
            roundRevealText.gameObject.SetActive(true);
            roundRevealText.text = string.Empty;
        }

        var revealPosition = shellRevealCameraAnchor.localPosition;
        var revealRotation = shellRevealCameraAnchor.localRotation;
        yield return MoveCamera(cameraRestPosition, cameraRestRotation, revealPosition, revealRotation, 0.7f);

        if (roundRevealText != null)
        {
            roundRevealText.text = "6 SHELLS\n2 LIVE   •   4 BLANK";
            roundRevealText.color = new Color(0.95f, 0.67f, 0.42f, 1f);
        }
        Debug.Log("[Chamber] Loading reveal: 2 live shells and 4 blank shells. Their order is hidden.");
        yield return new WaitForSeconds(2.5f);

        if (roundRevealText != null) roundRevealText.text = string.Empty;
        yield return MoveCamera(revealPosition, revealRotation, cameraRestPosition, cameraRestRotation, 0.75f);
        foreach (var shell in shellProps) shell.SetActive(false);
        trayCarrier.gameObject.SetActive(false);

        yield return MoveDealerHandsToWeapon(false, 0.55f);
        yield return MoveWeapon(openingLoadAnchor, 0.68f);
        AttachHand(dealerLeftHand, openingPumpHandGrip);
        yield return new WaitForSeconds(0.15f);

        for (var i = 0; i < shellProps.Count; i++)
        {
            if (roundRevealText != null) roundRevealText.text = $"CHARGING {i + 1} / {shellProps.Count}";
            yield return PumpWeapon(0.38f);
            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.35f);
        yield return MoveWeapon(weaponTableAnchor, 0.72f);
        yield return ReturnDealerHands(0.42f);

        if (roundRevealText != null) roundRevealText.text = "SIX CHARGES COMPLETE\nP(LIVE NEXT) = 2 / 6";
        yield return new WaitForSeconds(2.2f);
        CompleteOpening();
    }

    private void CompleteOpening()
    {
        if (roundRevealText != null) roundRevealText.gameObject.SetActive(false);
        SetWeaponAt(weaponTableAnchor);
        duelCamera.localPosition = cameraRestPosition;
        duelCamera.localRotation = cameraRestRotation;
        introPlaying = false;
        resolving = false;
        playerTurn = true;
        Debug.Log("[Chamber] Your turn — press 1 for the house, 2 for yourself, or R to restart.");
    }

    private IEnumerator MoveCamera(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, float duration)
    {
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            duelCamera.localPosition = Vector3.Lerp(fromPosition, toPosition, progress);
            duelCamera.localRotation = Quaternion.Slerp(fromRotation, toRotation, progress);
            yield return null;
        }
        duelCamera.localPosition = toPosition;
        duelCamera.localRotation = toRotation;
    }

    private void ResetShellReveal()
    {
        for (var i = 0; i < shellProps.Count; i++)
        {
            if (shellProps[i] == null) continue;
            shellProps[i].SetActive(true);
            if (i < shellRestParents.Count) shellProps[i].transform.SetParent(shellRestParents[i], false);
            if (i < shellRestPositions.Count) shellProps[i].transform.localPosition = shellRestPositions[i];
            if (i < shellRestRotations.Count) shellProps[i].transform.localRotation = shellRestRotations[i];
        }
    }

    private void ChooseDealer() => StartCoroutine(ResolveShot(true, false));
    private void ChooseSelf() => StartCoroutine(ResolveShot(true, true));

    private IEnumerator ResolveShot(bool isPlayer, bool targetsSelf)
    {
        if (introPlaying || resolving || round == null || round.RemainingTotal == 0) yield break;
        resolving = true;
        var liveBefore = round.RemainingLive;
        var totalBefore = round.RemainingTotal;
        var shell = round.Fire();
        var actor = isPlayer ? "You" : "Doll";
        var target = targetsSelf ? actor : (isPlayer ? "Doll" : "you");
        var outcome = shell == Shell.Live ? "LIVE" : "BLANK";
        report.Add($"{actor} aimed at {target}: {outcome} [before: {liveBefore}/{totalBefore} live]");

        yield return AnimateShot(isPlayer, targetsSelf, shell == Shell.Live);
        if (shell == Shell.Live)
        {
            if (targetsSelf)
            {
                if (isPlayer) playerHealth--; else dealerHealth--;
            }
            else
            {
                if (isPlayer) dealerHealth--; else playerHealth--;
            }
            if (dealerHealth <= 0)
            {
                ReleaseDealerHands();
                ApplyDealerDefeatedPose();
            }
        }
        yield return new WaitForSeconds(0.9f);
        if (EndIfNeeded()) yield break;

        var keepTurn = shell == Shell.Blank && targetsSelf;
        if (keepTurn)
        {
            playerTurn = isPlayer;
            resolving = false;
            if (!isPlayer) StartCoroutine(DealerMove());
            yield break;
        }

        playerTurn = !isPlayer;
        resolving = false;
        if (isPlayer) StartCoroutine(DealerMove());
    }

    private IEnumerator DealerMove()
    {
        resolving = true;
        yield return new WaitForSeconds(0.65f);
        var targetsSelf = round.LiveChance < 0.5f;
        yield return new WaitForSeconds(0.25f);
        resolving = false;
        yield return ResolveShot(false, targetsSelf);
    }

    private bool EndIfNeeded()
    {
        if (playerHealth > 0 && dealerHealth > 0 && round.RemainingTotal > 0) return false;
        report.Insert(0, "LEVEL 1: Conditional probability review");
        report.Insert(1, "P(live next | revealed shells) = live remaining / total remaining.");
        report.Insert(2, "Each revealed shell is evidence: remove it, then recalculate.");
        playerTurn = false;
        resolving = false;
        Debug.Log("[Chamber] " + string.Join("\n", report));
        return true;
    }

    private IEnumerator AnimateShot(bool isPlayer, bool targetsSelf, bool live)
    {
        var aimAnchor = isPlayer
            ? (targetsSelf ? playerAimSelfAnchor : playerAimDealerAnchor)
            : (targetsSelf ? dealerAimSelfAnchor : dealerAimPlayerAnchor);

        if (isPlayer)
        {
            ResetDealerEntity();
            yield return new WaitForSeconds(0.22f);
        }
        else
        {
            ResetDealerEntity();
            yield return MoveDealerHandsToWeapon(targetsSelf, 0.52f);
        }

        mechanicalSource.PlayOneShot(shellLoadClip, 0.46f);
        yield return MoveWeapon(aimAnchor, isPlayer ? 0.62f : 0.82f);
        yield return PumpWeapon(isPlayer ? 0.34f : 0.42f);
        if (!isPlayer)
        {
            yield return new WaitForSeconds(0.38f);
        }
        else
        {
            yield return new WaitForSeconds(0.22f);
        }

        audioSource.PlayOneShot(live ? liveShotClip : blankClickClip, live ? 1f : 0.82f);
        if (live && muzzleFlash != null) muzzleFlash.intensity = 9f;
        var aimPosition = playerWeapon.position;
        playerWeapon.position += aimAnchor.forward * (live ? 0.16f : 0.035f);
        if (live && ((!isPlayer && !targetsSelf) || (isPlayer && targetsSelf)))
            yield return CameraImpact();
        else
            yield return new WaitForSeconds(live ? 0.16f : 0.09f);
        if (muzzleFlash != null) muzzleFlash.intensity = 0f;

        playerWeapon.position = aimPosition;
        var dealerWasHit = live && ((isPlayer && !targetsSelf) || (!isPlayer && targetsSelf));
        if (dealerWasHit)
        {
            lastDealerHitWasSelfInflicted = !isPlayer && targetsSelf;
            yield return DealerHitReaction(lastDealerHitWasSelfInflicted);
        }

        yield return MoveWeapon(weaponTableAnchor, 0.72f);
        if (!isPlayer) yield return ReturnDealerHands(0.42f);
        ResetDealerEntity();
    }

    private IEnumerator MoveWeapon(Transform destination, float duration)
    {
        var startPosition = playerWeapon.position;
        var startRotation = playerWeapon.rotation;
        var travelDistance = Vector3.Distance(startPosition, destination.position);
        var turnAngle = Quaternion.Angle(startRotation, destination.rotation);
        var turnDirection = Mathf.Sign(Vector3.Dot(
            Vector3.Cross(startRotation * Vector3.forward, destination.rotation * Vector3.forward),
            Vector3.up));
        if (Mathf.Approximately(turnDirection, 0f))
            turnDirection = Mathf.Sign(destination.position.x - startPosition.x);
        if (Mathf.Approximately(turnDirection, 0f)) turnDirection = 1f;

        // Animate in world space so the old anchor cannot counter-rotate the
        // weapon. The arc pitch and bank make the pickup read as weight being
        // carried between two hands; both fade out before the final grip pose.
        playerWeapon.SetParent(null, true);
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            var arc = Mathf.Sin(progress * Mathf.PI);
            var lift = arc * Mathf.Lerp(0.10f, 0.18f, Mathf.Clamp01(travelDistance / 0.65f));
            playerWeapon.position = Vector3.Lerp(startPosition, destination.position, progress) + Vector3.up * lift;
            playerWeapon.rotation = EvaluateCarriedWeaponRotation(
                startRotation, destination.rotation, progress, travelDistance, turnAngle, turnDirection);
            yield return null;
        }
        SetWeaponAt(destination);
    }

    private static Quaternion EvaluateCarriedWeaponRotation(Quaternion startRotation, Quaternion destinationRotation,
        float progress, float travelDistance, float turnAngle, float turnDirection)
    {
        var arc = Mathf.Sin(progress * Mathf.PI);
        var carriedRotation = Quaternion.Slerp(startRotation, destinationRotation, progress);
        var pitch = -arc * Mathf.Lerp(4f, 9f, Mathf.Clamp01(travelDistance / 0.65f));
        var bank = arc * turnDirection * Mathf.Lerp(4f, 11f, Mathf.Clamp01(turnAngle / 90f));
        return carriedRotation * Quaternion.Euler(pitch, 0f, bank);
    }

    private IEnumerator PumpWeapon(float duration)
    {
        var backPosition = pumpRestPosition + Vector3.back * 0.075f;
        var backDuration = duration * 0.46f;
        mechanicalSource.PlayOneShot(shellLoadClip, 0.62f);
        for (var t = 0f; t < backDuration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / backDuration);
            weaponPump.localPosition = Vector3.Lerp(pumpRestPosition, backPosition, progress);
            weaponPump.localRotation = pumpRestRotation * Quaternion.Euler(progress * 2.5f, 0f, 0f);
            yield return null;
        }
        for (var t = 0f; t < duration - backDuration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / (duration - backDuration));
            weaponPump.localPosition = Vector3.Lerp(backPosition, pumpRestPosition, progress);
            weaponPump.localRotation = pumpRestRotation * Quaternion.Euler((1f - progress) * 2.5f, 0f, 0f);
            yield return null;
        }
        weaponPump.localPosition = pumpRestPosition;
        weaponPump.localRotation = pumpRestRotation;
    }

    private IEnumerator DealerHitReaction(bool selfInflicted)
    {
        dollVoiceSource.pitch = selfInflicted ? 0.82f : 0.72f;
        dollVoiceSource.PlayOneShot(dollVoiceClip, 0.58f);
        var fallenPosition = entityRestPosition + (selfInflicted
            ? new Vector3(0.08f, -0.34f, 0.02f)
            : new Vector3(0f, -0.32f, 0.06f));
        var fallenRotation = entityRestRotation * (selfInflicted
            ? Quaternion.Euler(-6f, 0f, -12f)
            : Quaternion.Euler(-10f, 0f, 0f));
        const float fallDuration = 0.28f;
        for (var t = 0f; t < fallDuration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / fallDuration);
            dealerEntity.localPosition = Vector3.Lerp(entityRestPosition, fallenPosition, progress);
            dealerEntity.localRotation = Quaternion.Slerp(entityRestRotation, fallenRotation, progress);
            yield return null;
        }
        dealerEntity.localPosition = fallenPosition;
        dealerEntity.localRotation = fallenRotation;
        dollVoiceSource.pitch = 0.9f;
        dollVoiceSource.PlayOneShot(dollFallClip, 0.68f);
        yield return new WaitForSeconds(2.5f);

        const float recoveryDuration = 0.55f;
        for (var t = 0f; t < recoveryDuration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / recoveryDuration);
            dealerEntity.localPosition = Vector3.Lerp(fallenPosition, entityRestPosition, progress);
            dealerEntity.localRotation = Quaternion.Slerp(fallenRotation, entityRestRotation, progress);
            yield return null;
        }
        ResetDealerEntity();
    }

    private void ApplyDealerDefeatedPose()
    {
        dealerEntity.localPosition = entityRestPosition + (lastDealerHitWasSelfInflicted
            ? new Vector3(0.08f, -0.34f, 0.02f)
            : new Vector3(0f, -0.32f, 0.06f));
        dealerEntity.localRotation = entityRestRotation * (lastDealerHitWasSelfInflicted
            ? Quaternion.Euler(-6f, 0f, -12f)
            : Quaternion.Euler(-10f, 0f, 0f));
    }

    private IEnumerator MoveDealerHandsToWeapon(bool targetsSelf, float duration)
    {
        var rightTarget = targetsSelf ? dealerSelfRightGrip : dealerRightGrip;
        var leftTarget = targetsSelf ? dealerSelfLeftGrip : dealerLeftGrip;
        var rightStartPosition = dealerRightHand.position;
        var leftStartPosition = dealerLeftHand.position;
        var rightStartRotation = dealerRightHand.rotation;
        var leftStartRotation = dealerLeftHand.rotation;
        dealerRightHand.SetParent(null, true);
        dealerLeftHand.SetParent(null, true);

        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            var lift = Mathf.Sin(progress * Mathf.PI) * 0.07f;
            dealerRightHand.position = Vector3.Lerp(rightStartPosition, rightTarget.position, progress) + Vector3.up * lift;
            dealerLeftHand.position = Vector3.Lerp(leftStartPosition, leftTarget.position, progress) + Vector3.up * lift;
            dealerRightHand.rotation = Quaternion.Slerp(rightStartRotation, rightTarget.rotation, progress);
            dealerLeftHand.rotation = Quaternion.Slerp(leftStartRotation, leftTarget.rotation, progress);
            SetHandPose(dealerRightHand, progress, false);
            SetHandPose(dealerLeftHand, progress, false);
            yield return null;
        }

        AttachHand(dealerRightHand, rightTarget);
        AttachHand(dealerLeftHand, leftTarget);
        SetHandPose(dealerRightHand, 1f, false);
        SetHandPose(dealerLeftHand, 1f, false);
    }

    private IEnumerator ReturnDealerHands(float duration)
    {
        var rightStartPosition = dealerRightHand.position;
        var leftStartPosition = dealerLeftHand.position;
        var rightStartRotation = dealerRightHand.rotation;
        var leftStartRotation = dealerLeftHand.rotation;
        dealerRightHand.SetParent(null, true);
        dealerLeftHand.SetParent(null, true);
        var rightTargetPosition = rightHandRestParent.TransformPoint(rightHandRestPosition);
        var leftTargetPosition = leftHandRestParent.TransformPoint(leftHandRestPosition);
        var rightTargetRotation = rightHandRestParent.rotation * rightHandRestRotation;
        var leftTargetRotation = leftHandRestParent.rotation * leftHandRestRotation;

        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var progress = Mathf.SmoothStep(0f, 1f, t / duration);
            dealerRightHand.position = Vector3.Lerp(rightStartPosition, rightTargetPosition, progress);
            dealerLeftHand.position = Vector3.Lerp(leftStartPosition, leftTargetPosition, progress);
            dealerRightHand.rotation = Quaternion.Slerp(rightStartRotation, rightTargetRotation, progress);
            dealerLeftHand.rotation = Quaternion.Slerp(leftStartRotation, leftTargetRotation, progress);
            SetHandPose(dealerRightHand, 1f - progress, false);
            SetHandPose(dealerLeftHand, 1f - progress, false);
            yield return null;
        }
        ReleaseDealerHands();
    }

    private static void AttachHand(Transform hand, Transform grip)
    {
        hand.SetParent(grip, false);
        hand.localPosition = Vector3.zero;
        hand.localRotation = Quaternion.identity;
    }

    private void ReleaseDealerHands()
    {
        dealerRightHand.SetParent(rightHandRestParent, false);
        dealerRightHand.localPosition = rightHandRestPosition;
        dealerRightHand.localRotation = rightHandRestRotation;
        dealerLeftHand.SetParent(leftHandRestParent, false);
        dealerLeftHand.localPosition = leftHandRestPosition;
        dealerLeftHand.localRotation = leftHandRestRotation;
        SetHandPose(dealerRightHand, 0f, false);
        SetHandPose(dealerLeftHand, 0f, false);
    }

    private void SetHandPose(Transform hand, float curl, bool pinch)
    {
        var rig = hand == dealerRightHand ? rightHandRig : leftHandRig;
        if (rig == null || !rig.IsComplete) return;
        curl = Mathf.Clamp01(curl);
        for (var i = 0; i < rig.fingerBones.Length; i++)
        {
            var finger = i / 3;
            var segment = i % 3;
            var fingerCurl = pinch && finger == 0 ? curl * 0.72f : curl;
            var angle = segment == 0 ? 54f : (segment == 1 ? 68f : 44f);
            rig.fingerBones[i].localRotation = rig.fingerRestRotations[i] * Quaternion.Euler(-angle * fingerCurl, 0f, 0f);
        }
        for (var i = 0; i < rig.thumbBones.Length; i++)
        {
            var angle = i == 0 ? 38f : 52f;
            rig.thumbBones[i].localRotation = rig.thumbRestRotations[i] *
                Quaternion.Euler(-angle * curl, (pinch ? 18f : 10f) * curl, -12f * curl);
        }
    }

    private static Quaternion[] CaptureRotations(Transform[] bones)
    {
        var rotations = new Quaternion[bones.Length];
        for (var i = 0; i < bones.Length; i++) rotations[i] = bones[i] == null ? Quaternion.identity : bones[i].localRotation;
        return rotations;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (var item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item;
        return null;
    }

    private void ResetDealerEntity()
    {
        dealerEntity.localPosition = entityRestPosition;
        dealerEntity.localRotation = entityRestRotation;
        dealerEntity.localScale = entityRestScale;
    }

    private IEnumerator CameraImpact()
    {
        var startPosition = duelCamera.localPosition;
        var startRotation = duelCamera.localRotation;
        for (var t = 0f; t < 0.34f; t += Time.deltaTime)
        {
            var decay = 1f - t / 0.34f;
            duelCamera.localPosition = startPosition + new Vector3(Mathf.Sin(t * 93f), Mathf.Cos(t * 77f), -0.06f) * (0.035f * decay);
            duelCamera.localRotation = startRotation * Quaternion.Euler(Mathf.Sin(t * 81f) * 4.5f * decay, Mathf.Cos(t * 69f) * 3f * decay, 0f);
            yield return null;
        }
        duelCamera.localPosition = startPosition;
        duelCamera.localRotation = startRotation;
    }

    private void SetWeaponAt(Transform anchor)
    {
        playerWeapon.SetParent(anchor, true);
        playerWeapon.localPosition = Vector3.zero;
        playerWeapon.localRotation = Quaternion.identity;
    }

}
