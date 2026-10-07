using UnityEngine;
using UnityEngine.UI;

namespace ArcaneArchive.Interaction
{
    /// <summary>
    /// The single screen-space owner for world-interaction prompts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        public static InteractionPromptUI Instance { get; private set; }

        [SerializeField] private Text promptLabel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CreateLabelIfNeeded();
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string message)
        {
            if (promptLabel == null)
            {
                return;
            }

            promptLabel.text = message;
            promptLabel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (promptLabel != null)
            {
                promptLabel.gameObject.SetActive(false);
            }
        }

        private void CreateLabelIfNeeded()
        {
            if (promptLabel != null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            GameObject labelObject = new GameObject(
                "PromptLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(transform, false);

            RectTransform labelTransform = (RectTransform)labelObject.transform;
            labelTransform.anchorMin = new Vector2(0.5f, 0f);
            labelTransform.anchorMax = new Vector2(0.5f, 0f);
            labelTransform.pivot = new Vector2(0.5f, 0f);
            labelTransform.anchoredPosition = new Vector2(0f, 32f);
            labelTransform.sizeDelta = new Vector2(600f, 48f);

            promptLabel = labelObject.GetComponent<Text>();
            promptLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptLabel.fontSize = 24;
            promptLabel.alignment = TextAnchor.MiddleCenter;
            promptLabel.color = Color.white;
        }
    }
}
