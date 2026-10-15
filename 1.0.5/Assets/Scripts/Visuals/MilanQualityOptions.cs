using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Visuals
{
    // Only changes the two authored quality profiles; geometry, animation and physics stay active.
    public sealed class MilanQualityOptions : MonoBehaviour
    {
        void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            // A tested window size on the target Mac; explicit launch dimensions take precedence.
            if (!Application.isEditor && Array.IndexOf(args, "-screen-width") < 0 &&
                Array.IndexOf(args, "-screen-height") < 0 && Array.IndexOf(args, "-screen-fullscreen") < 0)
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            int option = Array.IndexOf(args, "-milanQuality");
            string requested = option >= 0 && option + 1 < args.Length ? args[option + 1] : "Ultra";
            Select(string.Equals(requested, "Ultra", StringComparison.OrdinalIgnoreCase) ? "Ultra" : "Balanced");
        }
        void Update()
        {
            if (Keyboard.current?.f8Key.wasPressedThisFrame == true) Select("Balanced");
            if (Keyboard.current?.f9Key.wasPressedThisFrame == true) Select("Ultra");
        }
        public void Select(string name)
        {
            int index = Array.IndexOf(QualitySettings.names, name);
            if (index < 0) { Debug.LogError("Missing authored Milan quality profile: " + name); return; }
            QualitySettings.SetQualityLevel(index, true);
            Application.targetFrameRate = 60;
            if (TryGetComponent<ArcaneArchive.UI.RecoveryMenu>(out var menu)) menu.ShowQualityHint(name);
        }
    }
}
