using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Aeloria.Core;

namespace Aeloria.UI
{
    /// <summary>
    /// Individual card slot in the player's hand
    /// Handles card display, input visualization, and animations
    /// </summary>
    public class CardSlotUI : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex;  // 0-3 for ABXY buttons
        [SerializeField] private KeyCode keyBinding;  // Keyboard key for this slot
        [SerializeField] private string gamepadButton;  // Gamepad button name

        [Header("UI References")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardArtwork;
        [SerializeField] private Image cardFrame;
        [SerializeField] private Text cardNameText;
        [SerializeField] private Text energyCostText;
        [SerializeField] private Text chargesText;
        [SerializeField] private Image energyCostBG;
        [SerializeField] private Image chargesBG;
        [SerializeField] private GameObject emptySlotVisual;
        [SerializeField] private GameObject cardVisual;

        [Header("Input Prompts")]
        [SerializeField] private Image buttonPrompt;  // Shows A/B/X/Y icon
        [SerializeField] private Text keyPromptText;  // Shows keyboard key

        [Header("Visual States")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color unavailableColor = Color.gray;
        [SerializeField] private Color activatingColor = Color.green;

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float drawAnimTime = 0.3f;
        [SerializeField] private AnimationCurve drawCurve;
        [SerializeField] private AnimationCurve exhaustCurve;

        // State
        private object currentCardData;  // Will be CardData when we implement it
        private bool isAvailable = false;
        private bool isHighlighted = false;
        private bool isAnimating = false;
        private Coroutine pulseCoroutine;

        // Properties
        public int SlotIndex => slotIndex;
        public bool HasCard => currentCardData != null;
        public bool IsAvailable => isAvailable && HasCard && !isAnimating;
        public object CardData => currentCardData;

        private void Start()
        {
            // Set initial state
            SetEmpty();

            // Configure input prompt
            UpdateInputPrompt();
        }

        /// <summary>
        /// Update this slot with new card data
        /// </summary>
        public void SetCard(object cardData)
        {
            currentCardData = cardData;

            if (cardData != null)
            {
                // Show card visual
                if (emptySlotVisual) emptySlotVisual.SetActive(false);
                if (cardVisual) cardVisual.SetActive(true);

                // Update card display (will be expanded when we have CardData class)
                UpdateCardDisplay();

                // Animate card draw
                StartCoroutine(AnimateCardDraw());
            }
            else
            {
                SetEmpty();
            }
        }

        /// <summary>
        /// Clear this slot
        /// </summary>
        public void SetEmpty()
        {
            currentCardData = null;

            if (emptySlotVisual) emptySlotVisual.SetActive(true);
            if (cardVisual) cardVisual.SetActive(false);

            isAvailable = false;
            StopPulse();
        }

        /// <summary>
        /// Update card information display
        /// </summary>
        private void UpdateCardDisplay()
        {
            // This will be properly implemented when we have CardData
            // For now, show placeholder data

            if (cardNameText) cardNameText.text = "Card " + (slotIndex + 1);
            if (energyCostText) energyCostText.text = "3";
            if (chargesText) chargesText.text = "2";

            // Temporary: use slot index to vary colors
            Color slotColor = Color.HSVToRGB((slotIndex * 0.25f) % 1f, 0.7f, 1f);
            if (cardBackground) cardBackground.color = slotColor;
        }

        /// <summary>
        /// Update availability based on energy and game state
        /// </summary>
        public void UpdateAvailability(float currentEnergy)
        {
            if (!HasCard)
            {
                isAvailable = false;
                return;
            }

            // Check if we have enough energy (placeholder - will check actual cost)
            float cardCost = 3f;  // Placeholder
            isAvailable = currentEnergy >= cardCost;

            // Update visual state
            UpdateVisualState();
        }

        /// <summary>
        /// Update visual state based on availability and highlight
        /// </summary>
        private void UpdateVisualState()
        {
            if (!HasCard) return;

            Color targetColor = normalColor;

            if (!isAvailable)
            {
                targetColor = unavailableColor;
                StopPulse();
            }
            else if (isHighlighted)
            {
                targetColor = highlightColor;
                StartPulse();
            }
            else
            {
                targetColor = normalColor;
                StopPulse();
            }

            // Apply color to card elements
            if (cardFrame) cardFrame.color = targetColor;
            if (energyCostBG) energyCostBG.color = targetColor;
        }

        /// <summary>
        /// Highlight this slot (on hover or when usable)
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            isHighlighted = highlighted;
            UpdateVisualState();
        }

        /// <summary>
        /// Play card activation animation
        /// </summary>
        public void PlayCard()
        {
            if (!IsAvailable) return;

            StartCoroutine(AnimateCardPlay());
        }

        /// <summary>
        /// Animate card being drawn
        /// </summary>
        private IEnumerator AnimateCardDraw()
        {
            isAnimating = true;

            // Start scaled down and transparent
            transform.localScale = Vector3.zero;
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            float timer = 0;
            while (timer < drawAnimTime)
            {
                timer += Time.deltaTime;
                float progress = timer / drawAnimTime;
                float curveValue = drawCurve != null ? drawCurve.Evaluate(progress) : progress;

                transform.localScale = Vector3.one * curveValue;
                canvasGroup.alpha = curveValue;

                yield return null;
            }

            transform.localScale = Vector3.one;
            canvasGroup.alpha = 1;
            isAnimating = false;
        }

        /// <summary>
        /// Animate card being played
        /// </summary>
        private IEnumerator AnimateCardPlay()
        {
            isAnimating = true;

            // Flash activation color
            if (cardFrame) cardFrame.color = activatingColor;

            // Scale up slightly
            float timer = 0;
            Vector3 originalScale = transform.localScale;

            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                transform.localScale = originalScale * (1f + timer);
                yield return null;
            }

            // Then shrink and fade
            timer = 0;
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                float progress = timer / 0.3f;

                transform.localScale = originalScale * (1.4f - progress * 0.4f);
                canvasGroup.alpha = 1f - progress;

                yield return null;
            }

            // Reset
            transform.localScale = originalScale;
            canvasGroup.alpha = 1;
            isAnimating = false;

            // Card will be replaced by next card from deck
        }

        /// <summary>
        /// Update charges display
        /// </summary>
        public void UpdateCharges(int charges)
        {
            if (chargesText)
            {
                chargesText.text = charges.ToString();

                // Pulse if low charges
                if (charges == 1)
                {
                    StartPulse();
                }
            }
        }

        /// <summary>
        /// Start pulsing animation
        /// </summary>
        private void StartPulse()
        {
            if (pulseCoroutine != null) return;
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }

        /// <summary>
        /// Stop pulsing animation
        /// </summary>
        private void StopPulse()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Pulse animation coroutine
        /// </summary>
        private IEnumerator PulseAnimation()
        {
            while (true)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.05f;
                transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        /// <summary>
        /// Update input prompt based on control scheme
        /// </summary>
        private void UpdateInputPrompt()
        {
            // This will show A/B/X/Y for gamepad or 1/2/3/4 for keyboard
            if (keyPromptText)
            {
                keyPromptText.text = (slotIndex + 1).ToString();
            }

            // Button prompt would show gamepad button sprite
            // We'll implement this when we add controller support
        }

        /// <summary>
        /// Get world position for spell targeting
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }
    }
}