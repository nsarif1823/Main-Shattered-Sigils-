using UnityEngine;
using UnityEngine.UI;

namespace Aeloria.UI
{
    /// <summary>
    /// Displays corruption level and effects
    /// Will be expanded when corruption system is implemented
    /// </summary>
    public class CorruptionDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider corruptionSlider;
        [SerializeField] private Text corruptionText;
        [SerializeField] private Image corruptionFill;

        // Placeholder for now
        private void Start()
        {
            if (corruptionSlider != null)
            {
                corruptionSlider.value = 0;
                corruptionSlider.maxValue = 100;
            }
        }

        public void UpdateCorruption(int current, int max)
        {
            if (corruptionSlider != null)
            {
                corruptionSlider.value = current;
                corruptionSlider.maxValue = max;
            }

            if (corruptionText != null)
            {
                corruptionText.text = $"{current}/{max}";
            }
        }
    }
}