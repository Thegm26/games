using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Focused proof that three real hazards exhaust lives and lock combat.</summary>
[InitializeOnLoad]
public static class GameOverHudQa
{
    const string PendingKey = "ImmortalTomato.GameOverHudQa.Pending";
    const string ExitKey = "ImmortalTomato.GameOverHudQa.Exit";
    const string ResultKey = "ImmortalTomato.GameOverHudQa.Result";
    static TomatoGame tomato;
    static int hazards;
    static bool running;
    static double lastActionAt;
    static string ReportPath => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "QA", "Runtime", "game_over_hud_report.log");

    static GameOverHudQa() => EditorApplication.update += Tick;

    [MenuItem("Immortal Tomato/QA/Run Game Over HUD QA")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, "Immortal Tomato Game Over HUD QA started " + DateTime.UtcNow.ToString("O") + Environment.NewLine);
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
                Require(tomato != null, "Main scene did not create TomatoGame.");
                Require(tomato.Lives == 3 && tomato.MaxLives == 3 && !tomato.IsGameOver, "Tomato did not begin with three lives.");
                Require(tomato.CombatHud != null && tomato.CombatHud.GetComponent<CanvasScaler>() != null, "Responsive combat HUD was not created.");
                Require(tomato.CombatHud.GetComponent<CanvasScaler>().uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize, "Combat HUD does not use ScaleWithScreenSize.");
                EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                Require(eventSystem != null && eventSystem.GetComponent<StandaloneInputModule>() != null, "Combat HUD did not create an input EventSystem for its buttons.");
                running = true;
                Write("[SETUP] HUD exists and starts with 3/3 lives.");
                return;
            }
            if (hazards < 3 && !tomato.IsLocked && EditorApplication.timeSinceStartup - lastActionAt > .1d)
            {
                int before = tomato.Lives;
                tomato.HazardRespawn(tomato.CurrentCheckpoint);
                hazards++;
                lastActionAt = EditorApplication.timeSinceStartup;
                Require(tomato.Lives == before - 1, "A real hazard did not consume exactly one life.");
                tomato.HazardRespawn(tomato.CurrentCheckpoint);
                Require(tomato.Lives == before - 1, "Overlapping hazard contact consumed a second life while locked.");
                Write($"[HAZARD] {hazards} lives={tomato.Lives}/3.");
            }
            if (hazards < 3) return;
            Require(tomato.IsGameOver && tomato.Lives == 0 && tomato.IsLocked && !tomato.Body.simulated, "Third hazard did not enter locked Game Over state.");
            Require(tomato.GameOverOverlay != null && tomato.GameOverOverlay.activeInHierarchy, "Game Over overlay was not visible.");
            Require(tomato.GameOverOverlay.GetComponentsInChildren<Button>(true).Length >= 2, "Game Over restart/menu controls were not configured.");
            TomatoRuntimeTelemetry beforeShot = tomato.GetTelemetry();
            tomato.QueueHeadlessQaShoot();
            TomatoRuntimeTelemetry afterShot = tomato.GetTelemetry();
            Require(beforeShot.shootActionStarts == afterShot.shootActionStarts && !afterShot.shootActionActive && !afterShot.isReloading, "Shooting remained available at Game Over.");
            Write("[PASS] lives=3->2->1->0, duplicate contacts ignored, overlay/buttons present, shooting locked.");
            SessionState.SetInt(ResultKey, 0);
            SessionState.SetBool(ExitKey, true);
            SessionState.SetBool(PendingKey, false);
            EditorApplication.isPlaying = false;
        }
        catch (Exception exception)
        {
            Write("[FAIL] " + exception.Message);
            SessionState.SetInt(ResultKey, 1); SessionState.SetBool(ExitKey, true); SessionState.SetBool(PendingKey, false); EditorApplication.isPlaying = false;
        }
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Write(string value) => File.AppendAllText(ReportPath, value + Environment.NewLine);
}
