using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Aeloria.UI
{
    /// <summary>
    /// Displays energy bar with regeneration visualization
    /// Shows energy consumption and regeneration effects
    /// </summary>
    public class EnergyDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider energySlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text energyText;
        [SerializeField] private GameObject[] energyPips;  // Individual energy indicators

        [Header("Effects")]
        [SerializeField] private ParticleSystem regenParticles;
        [SerializeField] private Image glowEffect;

        [Header("Colors")]
        [SerializeField] private Gradient energyGradient;
        [SerializeField] private Color insufficientColor = Color.gray;
        [SerializeField] private Color consumeFlashColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float regenPulseSpeed = 2f;

        // State
        private float currentEnergy;
        private float maxEnergy;
        private float displayEnergy;
        private bool isRegenerating;

        private void Start()
        {
            if (energySlider != null)
            {
                energySlider.minValue = 0;
            }
        }

        private void Update()
        {
            // Smooth energy display
            if (displayEnergy != currentEnergy)
            {
                displayEnergy = Mathf.Lerp(displayEnergy, currentEnergy, Time.deltaTime * smoothSpeed);

                if (energySlider != null)
                {
                    energySlider.value = displayEnergy;
                }

                UpdateEnergyColor();
                UpdateEnergyPips();
            }

            // Regen pulse effect
            if (isRegenerating && glowEffect != null)
            {
                float alpha = (Mathf.Sin(Time.time * regenPulseSpeed) + 1f) * 0.5f * 0.3f;
                Color glowColor = glowEffect.color;
                glowColor.a = alpha;
                glowEffect.color = glowColor;
            }
        }

        /// <summary>
        /// Update energy display
        /// </summary>
        public void UpdateEnergy(float current, float max)
        {
            currentEnergy = current;
            maxEnergy = max;

            if (energySlider != null)
            {
                energySlider.maxValue = max;
            }

            UpdateEnergyText();

            // Check if regenerating
            isRegenerating = current < max;

            if (regenParticles != null)
            {
                if (isRegenerating && !regenParticles.isPlaying)
                {
                    regenParticles.Play();
                }
                else if (!isRegenerating && regenParticles.isPlaying)
                {
                    regenParticles.Stop();
                }
            }
        }

        /// <summary>
        /// Show energy consumption effect
        /// </summary>
        public void ShowEnergyConsumed(float amount)
        {
            StartCoroutine(ConsumeFlash());
        }

        /// <summary>
        /// Flash effect when energy is consumed
        /// </summary>
        private IEnumerator ConsumeFlash()
        {
            if (fillImage == null) yield break;

            Color originalColor = fillImage.color;
            fillImage.color = consumeFlashColor;

            float timer = 0;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                fillImage.color = Color.Lerp(consumeFlashColor, originalColor, timer / 0.2f);
                yield return null;
            }

            fillImage.color = originalColor;
        }

        /// <summary>
        /// Update energy bar color based on amount
        /// </summary>
        private void UpdateEnergyColor()
        {
            if (fillImage == null || energyGradient == null) return;

            float energyPercent = currentEnergy / maxEnergy;
            fillImage.color = energyGradient.Evaluate(energyPercent);
        }

        /// <summary>
        /// Update individual energy pip indicators
        /// </summary>
        private void UpdateEnergyPips()
        {
            if (energyPips == null || energyPips.Length == 0) return;

            int activePips = Mathf.FloorToInt((currentEnergy / maxEnergy) * energyPips.Length);

            for (int i = 0; i < energyPips.Length; i++)
            {
                if (energyPips[i] != null)
                {
                    energyPips[i].SetActive(i < activePips);
                }
            }
        }

        /// <summary>
        /// Update energy text display
        /// </summary>
        private void UpdateEnergyText()
        {
            if (energyText != null)
            {
                energyText.text = $"{Mathf.Floor(currentEnergy)}/{Mathf.Floor(maxEnergy)}";
            }
        }

        /// <summary>
        /// Check if there's enough energy for a cost
        /// </summary>
        public bool CanAfford(float cost)
        {
            return currentEnergy >= cost;
        }

        /// <summary>
        /// Show insufficient energy feedback
        /// </summary>
        public void ShowInsufficientEnergy()
        {
            StartCoroutine(InsufficientFlash());
        }

        /// <summary>
        /// Flash when trying to play card without energy
        /// </summary>
        private IEnumerator InsufficientFlash()
        {
            if (fillImage == null) yield break;

            Color originalColor = fillImage.color;

            for (int i = 0; i < 2; i++)
            {
                fillImage.color = insufficientColor;
                yield return new WaitForSeconds(0.1f);
                fillImage.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}