using System.Collections;
using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    [Header("Glow Visual")]
    [Tooltip("Assign the Mesh Renderer from the GlowVisual object.")]
    [SerializeField] private Renderer glowRenderer;

    [Header("Point Light")]
    [Tooltip("Assign the Point Light under this Glow object.")]
    [SerializeField] private Light glowLight;

    [Header("Blink Timing")]
    [Tooltip("How long the glow remains on.")]
    [SerializeField] private float lightOnDuration = 0.5f;

    [Tooltip("How long the glow remains off.")]
    [SerializeField] private float lightOffDuration = 0.3f;

    [Header("Light Settings")]
    [SerializeField] private float lightIntensity = 5f;

    [Header("Scale Pulse")]
    [Tooltip("Enable a subtle growing and shrinking effect.")]
    [SerializeField] private bool useScalePulse = true;

    [SerializeField] private float pulseSpeed = 3f;

    [Tooltip("A value of 0.15 means the object can grow by up to 15%.")]
    [SerializeField] private float scaleAmount = 0.15f;

    [Header("Visual Behaviour")]
    [Tooltip("Temporarily hide the GlowVisual when the light turns off.")]
    [SerializeField] private bool blinkVisualToo = true;

    private Vector3 originalScale;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (glowLight != null)
        {
            glowLight.intensity = lightIntensity;
        }
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (glowLight != null)
        {
            glowLight.enabled = false;
        }

        if (glowRenderer != null)
        {
            glowRenderer.enabled = true;
        }

        transform.localScale = originalScale;
    }

    private void Update()
    {
        if (!useScalePulse)
        {
            return;
        }

        float wave =
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float multiplier =
            1f + wave * scaleAmount;

        transform.localScale =
            originalScale * multiplier;
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // Turn the glow on.
            SetGlowState(true);

            yield return new WaitForSeconds(
                Mathf.Max(0.05f, lightOnDuration)
            );

            // Turn the glow off.
            SetGlowState(false);

            yield return new WaitForSeconds(
                Mathf.Max(0.05f, lightOffDuration)
            );
        }
    }

    private void SetGlowState(bool isOn)
    {
        if (glowLight != null)
        {
            glowLight.enabled = isOn;

            if (isOn)
            {
                glowLight.intensity = lightIntensity;
            }
        }

        if (blinkVisualToo && glowRenderer != null)
        {
            glowRenderer.enabled = isOn;
        }
    }
}