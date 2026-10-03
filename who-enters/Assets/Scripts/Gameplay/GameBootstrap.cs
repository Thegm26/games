using UnityEngine;
using WhoEnters.Core;

namespace WhoEnters.Gameplay
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntime()
        {
            if (Object.FindAnyObjectByType<GameDirector>() != null) return;
            try
            {
                Screen.orientation = ScreenOrientation.Portrait;
                var root = new GameObject("WhoEntersRuntime");
                Object.DontDestroyOnLoad(root);
                root.AddComponent<GameAudio>();
                root.AddComponent<GameDirector>();
                DebugTrace.Log("scene.runtime_created", "scene=Gatehouse;mode=runtime-ui;orientation=Portrait");
            }
            catch (System.Exception exception)
            {
                DebugTrace.Error("bootstrap.failed", exception.ToString());
                throw;
            }
        }
    }
}
