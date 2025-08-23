using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Aeloria.Core;

namespace Aeloria.UI
{
    /// <summary>
    /// Floating damage/heal number that appears above entities
    /// Auto-returns to pool after animation
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Text numberText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float fadeSpeed = 1f;
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private AnimationCurve scaleCurve;
        [SerializeField] private AnimationCurve moveCurve;

        [Header("Style")]
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color criticalDamageColor = Color.yellow;
        [SerializeField] private Color healColor = Color.green;
        [SerializeField] private Color shieldColor = Color.cyan;

        public enum DamageType
        {
            Normal,
            Critical,
            Heal,
            Shield,
            Poison,
            Fire
        }

        private Vector3 startPosition;
        private float timer;

        /// <summary>
        /// Initialize damage number with value and type
        /// </summary>
        public void Initialize(float value, DamageType type)
        {
            // Set text
            if (numberText != null)
            {
                numberText.text = Mathf.Abs(value).ToString("0");

                // Set color based on type
                switch (type)
                {
                    case DamageType.Normal:
                        numberText.color = normalDamageColor;
                        break;
                    case DamageType.Critical:
                        numberText.color = criticalDamageColor;
                        numberText.text = value.ToString("0") + "!";
                        break;
                    case DamageType.Heal:
                        numberText.color = healColor;
                        numberText.text = "+" + value.ToString("0");
                        break;
                    case DamageType.Shield:
                        numberText.color = shieldColor;
                        break;
                }
            }

            // Reset animation
            startPosition = transform.position;
            timer = 0;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // Start animation
            StartCoroutine(AnimateNumber());
        }

        /// <summary>
        /// Animate the damage number floating up and fading
        /// </summary>
        private IEnumerator AnimateNumber()
        {
            while (timer < lifetime)
            {
                timer += Time.deltaTime;
                float progress = timer / lifetime;

                // Move upward
                Vector3 offset = Vector3.up * floatSpeed * moveCurve.Evaluate(progress) * Time.deltaTime;
                transform.position += offset;

                // Scale animation
                if (scaleCurve != null)
                {
                    float scale = scaleCurve.Evaluate(progress);
                    transform.localScale = Vector3.one * scale;
                }

                // Fade out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - progress;
                }

                yield return null;
            }

            // Return to pool
            UIManager.Instance.ReturnToPool("DamageNumber", gameObject);
        }
    }
}