using System.Collections;
using System.Reflection;
using ChamberLogic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ChamberScenePlayTests
{
    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 4f;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator SceneLoadsWithRequiredGameplayObjects()
    {
        yield return LoadChamber();

        var game = Object.FindAnyObjectByType<ChamberLogicGame>();
        Assert.That(game, Is.Not.Null);
        Assert.That(game.enabled, Is.True, "The saved scene has missing gameplay references.");

        foreach (var objectName in new[]
                 {
                     "The House — Voodoo Doll",
                     "Doll Right Hand",
                     "Doll Left Hand",
                     "Player Shotgun",
                     "Duel Camera",
                     "Weapon Table Anchor",
                     "Mechanical Duel Table",
                     "The Bearer — Skeleton",
                     "Bearer Shell Tray",
                     "Tray Carrier Presentation Anchor",
                     "Tray Carrier Exit Anchor",
                     "Shell Reveal Camera Anchor",
                     "Gun Charge Camera Anchor",
                     "Opening Pump Hand Grip",
                     "Reload"
                 })
            Assert.That(GameObject.Find(objectName), Is.Not.Null, $"{objectName} is missing.");

        Assert.That(FindShells(), Has.Count.EqualTo(6));
        Assert.That(FindNamedTransforms("Player Shotgun"), Has.Count.EqualTo(1));
        Assert.That(Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include), Has.Length.GreaterThanOrEqualTo(5));

        foreach (var lightName in new[]
                 {
                     "Flickering Interrogation Light",
                     "Table Cold Focus",
                     "Dealer Red Backlight"
                 })
            Assert.That(GameObject.Find(lightName).GetComponent<Light>(), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator AuthoredWeaponLayoutAndAimRemainPlausible()
    {
        yield return LoadChamber();

        var table = CombinedBounds(GameObject.Find("Steel Tabletop"));
        var shotgunObject = GameObject.Find("Player Shotgun");
        var shotgun = CombinedBounds(shotgunObject);
        var rightHand = CombinedBounds(GameObject.Find("Doll Right Hand"));
        var leftHand = CombinedBounds(GameObject.Find("Doll Left Hand"));

        Assert.That(shotgun.min.y - table.max.y, Is.InRange(-0.005f, 0.10f));
        Assert.That(Mathf.Abs(shotgun.center.x - table.center.x), Is.LessThan(0.05f));
        Assert.That(Mathf.Abs(shotgun.center.z - table.center.z), Is.LessThan(0.05f));
        Assert.That(Mathf.Max(shotgun.size.x, shotgun.size.y, shotgun.size.z), Is.InRange(0.75f, 0.95f));
        Assert.That(shotgun.size.y, Is.LessThan(0.12f), "The shotgun is not lying flat.");
        Assert.That(rightHand.max.y, Is.LessThan(table.max.y));
        Assert.That(leftHand.max.y, Is.LessThan(table.max.y));

        var shellTray = GameObject.Find("Bearer Shell Tray").transform;
        Assert.That(shellTray.IsChildOf(GameObject.Find("The Bearer — Skeleton").transform), Is.True);
        Assert.That(GameObject.Find("Loader Housing"), Is.Null);
        Assert.That(GameObject.Find("Loader Slot"), Is.Null);
        Assert.That(GameObject.Find("Loader Guide Front"), Is.Null);
        Assert.That(GameObject.Find("Loader Guide Rear"), Is.Null);
        foreach (var shell in FindShells())
        {
            var size = CombinedBounds(shell.gameObject).size;
            Assert.That(Mathf.Max(size.x, size.y, size.z), Is.InRange(0.045f, 0.085f));
            Assert.That(Mathf.Min(size.x, size.y, size.z), Is.InRange(0.010f, 0.030f));
        }

        var weapon = shotgunObject.transform;
        var muzzle = GameObject.Find("Muzzle Flash").transform;
        var camera = GameObject.Find("Duel Camera").transform;
        var face = GameObject.Find("Voodoo Doll Face Target").transform;
        var originalParent = weapon.parent;
        var originalPosition = weapon.position;
        var originalRotation = weapon.rotation;

        AssertAim(weapon, muzzle, "Player Grip — Dealer", face);
        AssertAim(weapon, muzzle, "Player Grip — Self", camera);
        AssertAim(weapon, muzzle, "Dealer Hand Grip — Player", camera);
        AssertAim(weapon, muzzle, "Dealer Hand Grip — Self", face);

        weapon.SetParent(originalParent, true);
        weapon.position = originalPosition;
        weapon.rotation = originalRotation;
    }

    [UnityTest]
    public IEnumerator OpeningPresentsTrayAndHandsChargeWeaponSixTimes()
    {
        yield return LoadChamber();

        var game = Object.FindAnyObjectByType<ChamberLogicGame>();
        var pump = GameObject.Find("Reload").transform;
        var pumpRestPosition = pump.localPosition;
        var weapon = GameObject.Find("Player Shotgun").transform;
        var weaponRestPosition = weapon.position;
        var weaponRestRotation = weapon.rotation;
        var carrier = GameObject.Find("The Bearer — Skeleton").transform;
        var carrierRestPosition = carrier.localPosition;
        var presentationAnchor = GameObject.Find("Tray Carrier Presentation Anchor").transform;
        var exitAnchor = GameObject.Find("Tray Carrier Exit Anchor").transform;
        var tray = GameObject.Find("Bearer Shell Tray").transform;
        var firstShell = GameObject.Find("Shotgun Shell 1 — Live").transform;
        var firstShellLocalPosition = firstShell.localPosition;
        var loadingHand = GameObject.Find("Doll Right Hand").transform;
        var supportHand = GameObject.Find("Doll Left Hand").transform;
        var rearGrip = GameObject.Find("Apparition Right Grip").transform;
        var pumpGrip = GameObject.Find("Opening Pump Hand Grip").transform;

        var maximumPumpTravel = 0f;
        var maximumWeaponLift = 0f;
        var maximumCarrierTravel = 0f;
        var reachedPresentation = false;
        var bothHandsGrippedWeapon = false;
        var shellLeftTray = false;
        var pumpExtended = false;
        var pumpCycles = 0;
        for (var elapsed = 0f; elapsed < 12.5f; elapsed += Time.deltaTime)
        {
            var pumpTravel = Vector3.Distance(pumpRestPosition, pump.localPosition);
            maximumPumpTravel = Mathf.Max(maximumPumpTravel, pumpTravel);
            maximumWeaponLift = Mathf.Max(
                maximumWeaponLift, Vector3.Distance(weaponRestPosition, weapon.position));
            maximumCarrierTravel = Mathf.Max(
                maximumCarrierTravel, Vector3.Distance(carrierRestPosition, carrier.localPosition));
            reachedPresentation |= Vector3.Distance(carrier.position, presentationAnchor.position) < 0.05f;
            bothHandsGrippedWeapon |= loadingHand.parent == rearGrip && supportHand.parent == pumpGrip;
            if (!pumpExtended && pumpTravel > 0.045f)
            {
                pumpExtended = true;
                pumpCycles++;
            }
            else if (pumpExtended && pumpTravel < 0.01f)
            {
                pumpExtended = false;
            }
            foreach (var shell in FindShells())
                shellLeftTray |= shell.parent != tray;
            yield return null;
        }

        Assert.That(maximumPumpTravel, Is.GreaterThan(0.055f));
        Assert.That(maximumWeaponLift, Is.GreaterThan(0.25f));
        Assert.That(maximumCarrierTravel, Is.GreaterThan(0.55f));
        Assert.That(reachedPresentation, Is.True, "The skeleton never presented the tray beside the player.");
        Assert.That(bothHandsGrippedWeapon, Is.True);
        Assert.That(pumpCycles, Is.EqualTo(6), "The shotgun must be racked once for each visible shell.");
        Assert.That(shellLeftTray, Is.False, "A shell was attached to a hand instead of remaining on the tray.");
        Assert.That(firstShell.localPosition, Is.EqualTo(firstShellLocalPosition));

        Assert.That(FindShells(), Has.All.Matches<Transform>(shell => !shell.gameObject.activeSelf));
        Assert.That(carrier.gameObject.activeSelf, Is.False);
        Assert.That(Vector3.Distance(carrier.position, exitAnchor.position), Is.LessThan(0.002f));
        Assert.That(Vector3.Distance(pumpRestPosition, pump.localPosition), Is.LessThan(0.002f));
        Assert.That(Vector3.Distance(weaponRestPosition, weapon.position), Is.LessThan(0.005f));
        Assert.That(Quaternion.Angle(weaponRestRotation, weapon.rotation), Is.LessThan(1f));

        game.StopAllCoroutines();
        Invoke(game, "StartExperiment");
        game.StopAllCoroutines();
        Assert.That(carrier.gameObject.activeSelf, Is.True);
        Assert.That(Vector3.Distance(carrier.localPosition, carrierRestPosition), Is.LessThan(0.002f));
        Assert.That(FindShells(), Has.All.Matches<Transform>(shell => shell.gameObject.activeSelf));
    }

    [UnityTest]
    public IEnumerator BothPlayerChoicesConsumeOneShell()
    {
        yield return LoadChamber();
        var game = PrepareForChoice();
        Invoke(game, "ChooseDealer");
        yield return null;
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(5));

        yield return LoadChamber();
        game = PrepareForChoice();
        Invoke(game, "ChooseSelf");
        yield return null;
        Assert.That(CurrentRound(game).RemainingTotal, Is.EqualTo(5));
    }

    [UnityTest]
    public IEnumerator LiveHitDropsDollAndSurvivorRecovers()
    {
        yield return LoadChamber();
        var game = PrepareForChoice();
        var doll = GameObject.Find("The House — Voodoo Doll").transform;
        var restPosition = doll.localPosition;
        var maximumDownwardOffset = 0f;
        var maximumLateralOffset = 0f;

        game.StartCoroutine((IEnumerator)InvokeWithResult(game, "AnimateShot", true, false, true));
        for (var elapsed = 0f; elapsed < 6.2f; elapsed += Time.deltaTime)
        {
            maximumDownwardOffset = Mathf.Max(
                maximumDownwardOffset, restPosition.y - doll.localPosition.y);
            maximumLateralOffset = Mathf.Max(
                maximumLateralOffset, Mathf.Abs(restPosition.x - doll.localPosition.x));
            yield return null;
        }

        Assert.That(maximumDownwardOffset, Is.GreaterThan(0.25f));
        Assert.That(maximumLateralOffset, Is.LessThan(0.12f));
        Assert.That(Vector3.Distance(restPosition, doll.localPosition), Is.LessThan(0.01f));
    }

    private static IEnumerator LoadChamber()
    {
        yield return SceneManager.LoadSceneAsync("Chamber", LoadSceneMode.Single);
        yield return null;
    }

    private static ChamberLogicGame PrepareForChoice()
    {
        var game = Object.FindAnyObjectByType<ChamberLogicGame>();
        game.StopAllCoroutines();
        Invoke(game, "CompleteOpening");
        return game;
    }

    private static void AssertAim(Transform weapon, Transform muzzle, string anchorName, Transform target)
    {
        var anchor = GameObject.Find(anchorName).transform;
        weapon.SetParent(anchor, false);
        weapon.localPosition = Vector3.zero;
        weapon.localRotation = Quaternion.identity;
        Assert.That(Vector3.Angle(muzzle.forward, target.position - muzzle.position), Is.LessThan(3f),
            $"{anchorName} misses {target.name}.");
    }

    private static System.Collections.Generic.List<Transform> FindShells()
    {
        var shells = new System.Collections.Generic.List<Transform>();
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name.StartsWith("Shotgun Shell ")) shells.Add(item);
        return shells;
    }

    private static System.Collections.Generic.List<Transform> FindNamedTransforms(string name)
    {
        var matches = new System.Collections.Generic.List<Transform>();
        foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name == name) matches.Add(item);
        return matches;
    }

    private static Bounds CombinedBounds(GameObject item)
    {
        Assert.That(item, Is.Not.Null);
        var renderers = item.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty);
        var bounds = renderers[0].bounds;
        for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    private static ChamberRound CurrentRound(ChamberLogicGame game) =>
        (ChamberRound)typeof(ChamberLogicGame)
            .GetField("round", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(game);

    private static void Invoke(ChamberLogicGame game, string methodName) =>
        typeof(ChamberLogicGame)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, null);

    private static object InvokeWithResult(
        ChamberLogicGame game, string methodName, params object[] arguments) =>
        typeof(ChamberLogicGame)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, arguments);
}
