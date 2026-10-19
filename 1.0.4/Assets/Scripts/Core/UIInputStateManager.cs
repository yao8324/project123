using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Core
{
    /// <summary>
    /// Owns the transition between first-person controls and a single active UI panel.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class UIInputStateManager : MonoBehaviour
    {
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private GameObject testPanel;

        private GameObject activePanel;

        public GameObject ActivePanel => activePanel;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<FirstPersonController>();
            }

            if (testPanel != null)
            {
                testPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (activePanel != null)
            {
                CloseUI();
                return;
            }

            // Temporary verification shortcut. Business panels will call OpenUI directly.
            if (testPanel != null)
            {
                OpenUI(testPanel);
            }
        }

        public void OpenUI(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            if (activePanel != null && activePanel != panel)
            {
                activePanel.SetActive(false);
            }

            activePanel = panel;
            activePanel.SetActive(true);
            SetGameplayInput(false);
        }

        public void CloseUI()
        {
            if (activePanel == null)
            {
                return;
            }

            activePanel.SetActive(false);
            activePanel = null;
            SetGameplayInput(true);
        }

        private void SetGameplayInput(bool enabled)
        {
            if (playerController == null)
            {
                return;
            }

            playerController.SetGameplayInputEnabled(enabled);
            playerController.SetCursorLocked(enabled);
        }
    }
}
