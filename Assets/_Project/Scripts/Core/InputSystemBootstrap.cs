using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BIT.Core
{
    // Reemplaza en runtime cualquier StandaloneInputModule que haya quedado
    // en escenas guardadas antes de la migración al New Input System.
    public static class InputSystemBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            PatchScene();
            SceneManager.sceneLoaded += (_, _) => PatchScene();
        }

        static void PatchScene()
        {
            var modules = Object.FindObjectsByType<StandaloneInputModule>(FindObjectsSortMode.None);
            foreach (var module in modules)
            {
                var go = module.gameObject;
                Object.Destroy(module);
                if (go.GetComponent<InputSystemUIInputModule>() == null)
                    go.AddComponent<InputSystemUIInputModule>();
            }

            // Create EventSystem if none exists (game scene has no EventSystem by default)
            if (EventSystem.current == null)
            {
                var evGO = new GameObject("EventSystem");
                evGO.AddComponent<EventSystem>();
                evGO.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
