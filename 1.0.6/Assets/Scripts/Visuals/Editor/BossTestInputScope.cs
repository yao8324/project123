using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Visuals.Editor
{
    // Background automation must route queued pointer events to Play Mode. Clone settings
    // in memory, then restore them; never change the project's input settings asset.
    public sealed class BossTestInputScope : IDisposable
    {
        InputSettings previous,temporary;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
        bool cloned;
        public BossTestInputScope()
        {
            previous=InputSystem.settings;
            previousBackground=previous.backgroundBehavior;
            previousEditorBehavior=previous.editorInputBehaviorInPlayMode;
            // Keep the transient default's identity: Input System remembers it across
            // Play Mode. Clone only a persistent asset, which InputManager won't destroy.
            cloned=EditorUtility.IsPersistent(previous);
            temporary=cloned?UnityEngine.Object.Instantiate(previous):previous;
            if(cloned)temporary.hideFlags=HideFlags.HideAndDontSave;
            temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            if(cloned)InputSystem.settings=temporary;
            AssemblyReloadEvents.beforeAssemblyReload+=Dispose;
            EditorApplication.playModeStateChanged+=OnPlayModeChanged;
        }
        void OnPlayModeChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Dispose();}
        public void Dispose()
        {
            AssemblyReloadEvents.beforeAssemblyReload-=Dispose;EditorApplication.playModeStateChanged-=OnPlayModeChanged;
            if(!temporary)return;
            if(cloned)
            {
                if(InputSystem.settings==temporary)InputSystem.settings=previous;
                if(temporary)UnityEngine.Object.DestroyImmediate(temporary);
            }
            else
            {
                temporary.backgroundBehavior=previousBackground;
                temporary.editorInputBehaviorInPlayMode=previousEditorBehavior;
            }
            temporary=null;
        }
    }
}
