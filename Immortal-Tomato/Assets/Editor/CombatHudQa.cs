using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Focused runtime proof for the compact in-game combat HUD. Run in an isolated clone
/// with: Unity -batchmode -quit -projectPath ... -executeMethod CombatHudQa.Run
/// </summary>
[InitializeOnLoad]
public static class CombatHudQa
{
    const string PendingKey = "ImmortalTomato.CombatHudQa.Pending";
    const string ExitKey = "ImmortalTomato.CombatHudQa.Exit";
    const string ResultKey = "ImmortalTomato.CombatHudQa.Result";
    static TomatoGame tomato;
    static GameObject hud;
    static double phaseStarted;
    static Phase phase;
    static bool running;
    static bool sawReload;
    enum Phase { Setup, ManualReload, Damage, Recover, EmptyMagazine, Reload, Complete }

    static string ReportPath => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "QA", "Runtime", "combat_hud_report.log");

    static CombatHudQa() => EditorApplication.update += Tick;

    [MenuItem("Immortal Tomato/QA/Run Combat HUD QA")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, "Immortal Tomato Combat HUD QA started " + DateTime.UtcNow.ToString("O") + Environment.NewLine);
        SessionState.SetBool(PendingKey, true);
        SessionState.EraseBool(ExitKey);
        EditorSceneManager.OpenScene("Assets/Main.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (SessionState.GetBool(ExitKey, false) && !EditorApplication.isPlaying)
        {
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(ResultKey, 1));
            return;
        }
        if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (!running)
            {
                tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
                Require(tomato != null, "TomatoGame was not created.");
                hud = tomato.CombatHud;
                Require(hud != null, "Combat HUD was not created.");
                Canvas canvas = hud.GetComponent<Canvas>();
                CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
                Require(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "HUD is not a screen-space overlay.");
                Require(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize, "HUD is not responsive.");
                AssertHierarchyAndBounds();
                Require(tomato.Lives == 3 && tomato.AmmoInMagazine == tomato.MagazineSize, "HUD controller did not start at full health/ammo.");
                Require(CountFilledHearts() == tomato.MaxLives, "Initial heart icons are not all filled.");
                Write("[SETUP] compact top-left HUD, exactly two gun buttons + hearts, no old text/bar/UziHud objects.");
                Button firstGun = hud.transform.Find("Gun Button 1")?.GetComponent<Button>();
                Require(firstGun != null, "Gun Button 1 is missing.");
                Image gunIcon = firstGun.GetComponent<Image>();
                firstGun.onClick.Invoke();
                Canvas.ForceUpdateCanvases();
                Require(tomato.IsReloading, "Clicking a gun did not start a manual reload.");
                Require(gunIcon != null && gunIcon.color.r > .95f && gunIcon.color.g < .7f, "Clicked gun did not switch to reload tint.");
                phase = Phase.ManualReload;
                phaseStarted = EditorApplication.timeSinceStartup;
                running = true;
                return;
            }

            switch (phase)
            {
                case Phase.ManualReload:
                    Image gunIcon = hud.transform.Find("Gun Button 1")?.GetComponent<Image>();
                    if (EditorApplication.timeSinceStartup - phaseStarted < 4d) return;
                    Require(!tomato.IsReloading && gunIcon.color.g > .8f, "Manual reload did not restore gun tint.");
                    phase = Phase.Damage;
                    break;
                case Phase.Damage:
                    if (tomato.IsLocked) return;
                    tomato.HazardRespawn(tomato.CurrentCheckpoint);
                    Require(tomato.Lives == 2, "Damage did not reduce vitality by exactly one.");
                    Require(CountFilledHearts() == 2, "Damage state did not replace one filled heart.");
                    Write("[DAMAGE] hazard state rendered 2/3 with one empty heart.");
                    phase = Phase.Recover;
                    phaseStarted = EditorApplication.timeSinceStartup;
                    break;
                case Phase.Recover:
                    if (tomato.IsLocked)
                    {
                        Require(EditorApplication.timeSinceStartup - phaseStarted < 4d, "Tomato did not recover in time for reload QA.");
                        return;
                    }
                    tomato.ResetLivesForQa();
                    phase = Phase.EmptyMagazine;
                    phaseStarted = EditorApplication.timeSinceStartup;
                    break;
                case Phase.EmptyMagazine:
                    if (tomato.IsReloading)
                    {
                        phase = Phase.Reload;
                        phaseStarted = EditorApplication.timeSinceStartup;
                        return;
                    }
                    if (!tomato.GetTelemetry().shootActionActive && tomato.AmmoInMagazine > 0)
                        tomato.QueueHeadlessQaShoot();
                    Require(EditorApplication.timeSinceStartup - phaseStarted < 12d, "Could not empty the magazine through public QA input.");
                    break;
                case Phase.Reload:
                    if (tomato.IsReloading)
                    {
                        sawReload |= tomato.ReloadProgress > .05f && tomato.ReloadProgress < .98f;
                        Require(EditorApplication.timeSinceStartup - phaseStarted < 4d, "Reload did not complete in time.");
                        return;
                    }
                    Require(sawReload && tomato.AmmoInMagazine == tomato.MagazineSize, "Reload state did not expose progress and refill the magazine.");
                    Write("[RELOAD] empty magazine, in-progress reload, and full refill verified through public QA input.");
                    Complete();
                    break;
            }
        }
        catch (Exception exception)
        {
            Write("[FAIL] " + exception.Message);
            SessionState.SetInt(ResultKey, 1);
            SessionState.SetBool(ExitKey, true);
            SessionState.SetBool(PendingKey, false);
            EditorApplication.isPlaying = false;
        }
    }

    static void AssertHierarchyAndBounds()
    {
        Canvas.ForceUpdateCanvases();
        Require(hud.transform.Find("Combat Status Card") == null, "Large Combat Status Card still exists.");
        Require(hud.transform.Find("Magazine Label") == null && hud.transform.Find("Magazine Meter Track") == null && hud.transform.Find("Magazine State") == null, "Old ammo/reload UI still exists.");
        Button[] directButtons = hud.GetComponentsInChildren<Button>(true);
        int gunButtons = 0;
        foreach (Button button in directButtons) if (button.transform.parent == hud.transform && button.name.StartsWith("Gun Button ")) gunButtons++;
        Require(gunButtons == 2, "HUD must contain exactly two top-left gun buttons.");
        for (int i = 1; i <= 2; i++)
        {
            RectTransform gun = hud.transform.Find($"Gun Button {i}") as RectTransform;
            Require(gun != null && gun.anchorMin == new Vector2(0f, 1f) && gun.anchorMax == new Vector2(0f, 1f), $"Gun Button {i} is not top-left anchored.");
            Require(gun.sizeDelta.x <= 70f && gun.sizeDelta.y <= 55f, $"Gun Button {i} footprint is too large.");
        }
        Image[] hearts = hud.GetComponentsInChildren<Image>(true);
        int heartCount = 0;
        foreach (Image image in hearts) if (image.transform.parent == hud.transform && image.name.StartsWith("Heart ")) heartCount++;
        Require(heartCount == tomato.MaxLives, "Heart icon count does not match MaxLives.");
        Sprite oldUzi = Resources.Load<Sprite>("UI/UziHud");
        foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true))
        {
            Image image = graphic as Image;
            Require(image == null || image.sprite == null || image.sprite != oldUzi, "Combat HUD still renders the hand-bearing UziHud sprite.");
        }
        for (int i = 1; i <= 2; i++)
            for (int j = 1; j <= tomato.MaxLives; j++)
                Require(!Overlaps(Bounds(hud.transform.Find($"Gun Button {i}") as RectTransform), Bounds(hud.transform.Find($"Heart {j}") as RectTransform), .25f), "Gun and heart icons overlap.");
    }

    static int CountFilledHearts()
    {
        int filled = 0;
        foreach (Image heart in hud.GetComponentsInChildren<Image>(true))
            if (heart.transform.parent == hud.transform && heart.name.StartsWith("Heart ") && heart.sprite != null && !heart.sprite.texture.name.Contains("Empty")) filled++;
        return filled;
    }

    struct Box { public Vector3 min, max; }
    static Box Bounds(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return new Box { min = Vector3.Min(Vector3.Min(corners[0], corners[1]), Vector3.Min(corners[2], corners[3])), max = Vector3.Max(Vector3.Max(corners[0], corners[1]), Vector3.Max(corners[2], corners[3])) };
    }
    static bool Contains(Box inner, Box outer, float tolerance) => inner.min.x >= outer.min.x - tolerance && inner.max.x <= outer.max.x + tolerance && inner.min.y >= outer.min.y - tolerance && inner.max.y <= outer.max.y + tolerance;
    static bool Overlaps(Box a, Box b, float tolerance) => a.min.x < b.max.x - tolerance && a.max.x > b.min.x + tolerance && a.min.y < b.max.y - tolerance && a.max.y > b.min.y + tolerance;
    static void Complete()
    {
        Write("[PASS] compact HUD hierarchy, bounds, non-overlap, click reload tint, damage hearts and automatic reload passed.");
        SessionState.SetInt(ResultKey, 0); SessionState.SetBool(ExitKey, true); SessionState.SetBool(PendingKey, false); EditorApplication.isPlaying = false;
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Write(string value) => File.AppendAllText(ReportPath, value + Environment.NewLine);
}
