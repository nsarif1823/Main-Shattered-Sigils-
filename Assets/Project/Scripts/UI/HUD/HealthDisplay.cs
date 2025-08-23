using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Aeloria.UI
{
    /// <summary>
    /// Displays health bar with smooth animations and effects
    /// Supports damage preview, heal effects, and shield display
    /// </summary>
    public class HealthDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider damagePreviewSlider;  // Red bar that shows recent damage
        [SerializeField] private Image fillImage;
        [SerializeField] private Image damagePreviewImage;
        [SerializeField] private Text healthText;
        [SerializeField] private GameObject shieldOverlay;

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float damagePreviewDelay = 0.5f;
        [SerializeField] private float pulseIntensity = 1.2f;

        [Header("Colors")]
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private Color healFlashColor = Color.green;

        // State
        private float currentHealth;
        private float maxHealth;
        private float targetHealth;
        private float displayHealth;
        private Coroutine damagePreviewCoroutine;

        private void Start()
        {
            // Initialize sliders
            if (healthSlider != null)
            {
                healthSlider.minValue = 0;
            }
            if (damagePreviewSlider != null)
            {
                damagePreviewSlider.minValue = 0;
            }
        }

        /// <summary>
        /// Update health display with smooth animation
        /// </summary>
        public void UpdateHealth(float current, float max)
        {
            // Store values
            currentHealth = current;
            maxHealth = max;
            targetHealth = current;

            // Update slider max values
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
            }
            if (damagePreviewSlider != null)
            {
                damagePreviewSlider.maxValue = max;
            }

            // Update text immediately
            UpdateHealthText();

            // Handle damage preview
            if (current < displayHealth)
            {
                ShowDamagePreview();
            }
            else if (current > displayHealth)
            {
                ShowHealEffect();
            }
        }

        private void Update()
        {
            // Smooth health bar movement
            if (displayHealth != targetHealth)
            {
                displayHealth = Mathf.Lerp(displayHealth, targetHealth, Time.deltaTime * smoothSpeed);

                if (healthSlider != null)
                {
                    healthSlider.value = displayHealth;
                }

                // Update color based on health percentage
                UpdateHealthColor();
            }
        }

        /// <summary>
        /// Show red preview bar for damage taken
        /// </summary>
        private void ShowDamagePreview()
        {
            if (damagePreviewSlider == null) return;

            // Set damage preview to current displayed health
            damagePreviewSlider.value = displayHealth;

            // Start coroutine to fade damage preview
            if (damagePreviewCoroutine != null)
            {
                StopCoroutine(damagePreviewCoroutine);
            }
            damagePreviewCoroutine = StartCoroutine(AnimateDamagePreview());

            // Flash effect
            StartCoroutine(FlashColor(damageFlashColor));
        }

        /// <summary>
        /// Animate the damage preview bar
        /// </summary>
        private IEnumerator AnimateDamagePreview()
        {
            yield return new WaitForSeconds(damagePreviewDelay);

            float startValue = damagePreviewSlider.value;
            float timer = 0;

            while (timer < 1f)
            {
                timer += Time.deltaTime * 2f;
                damagePreviewSlider.value = Mathf.Lerp(startValue, targetHealth, timer);
                yield return null;
            }

            damagePreviewSlider.value = targetHealth;
        }

        /// <summary>
        /// Show heal effect with pulse animation
        /// </summary>
        private void ShowHealEffect()
        {
            StartCoroutine(FlashColor(healFlashColor));
            StartCoroutine(PulseEffect());
        }

        /// <summary>
        /// Flash the health bar color
        /// </summary>
        private IEnumerator FlashColor(Color flashColor)
        {
            if (fillImage == null) yield break;

            Color originalColor = fillImage.color;
            fillImage.color = flashColor;

            float timer = 0;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                fillImage.color = Color.Lerp(flashColor, originalColor, timer / 0.3f);
                yield return null;
            }

            fillImage.color = originalColor;
        }

        /// <summary>
        /// Pulse effect for healing
        /// </summary>
        private IEnumerator PulseEffect()
        {
            if (healthSlider == null) yield break;

            Vector3 originalScale = healthSlider.transform.localScale;
            Vector3 targetScale = originalScale * pulseIntensity;

            float timer = 0;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                healthSlider.transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / 0.2f);
                yield return null;
            }

            timer = 0;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                healthSlider.transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / 0.2f);
                yield return null;
            }

            healthSlider.transform.localScale = originalScale;
        }

        /// <summary>
        /// Update health bar color based on percentage
        /// </summary>
        private void UpdateHealthColor()
        {
            if (fillImage == null || healthGradient == null) return;

            float healthPercent = currentHealth / maxHealth;
            fillImage.color = healthGradient.Evaluate(healthPercent);
        }

        /// <summary>
        /// Update health text display
        /// </summary>
        private void UpdateHealthText()
        {
            if (healthText != null)
            {
                healthText.text = $"{Mathf.Ceil(currentHealth)}/{Mathf.Ceil(maxHealth)}";
            }
        }

        /// <summary>
        /// Show or hide shield overlay
        /// </summary>
        public void SetShieldActive(bool active)
        {
            if (shieldOverlay != null)
            {
                shieldOverlay.SetActive(active);
            }
        }
    }
}