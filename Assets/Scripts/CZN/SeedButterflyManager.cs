using System.Collections;
using TMPro;
using UnityEngine;

public class SeedButterflyManager : MonoBehaviour
{
    [Header("Seed")]
    [SerializeField] private GameObject seedObject;
    [SerializeField] private Transform seedRevealPoint;
    [SerializeField] private Transform seedFocusPoint;
    [SerializeField] private Collider seedClickCollider;
    [SerializeField] private GameObject seedTapHint;
    [SerializeField] private GameObject seedSparkle;

    [Header("Seed Timing")]
    [SerializeField] private float revealDelay = 0.8f;
    [SerializeField] private float seedRevealDuration = 0.6f;
    [SerializeField] private float seedMoveDuration = 1.2f;
    [SerializeField] private float focusPauseDuration = 0.8f;

    [Header("Butterfly")]
    [SerializeField] private GameObject butterflyObject;
    [SerializeField] private Animator butterflyAnimator;

    [SerializeField]
    private string butterflyFlyState = "Base Layer.Move";

    [SerializeField] private Transform butterflyStartPoint;
    [SerializeField] private Transform butterflyControlPoint;
    [SerializeField] private Transform butterflyEndPoint;

    [SerializeField] private float butterflyFlightDuration = 2.5f;

    [Header("Text")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text instructionText;

    private Vector3 seedOriginalScale;

    private bool sequenceStarted;
    private bool seedRevealed;
    private bool seedSelected;

    private void Awake()
    {
        if (seedObject != null)
        {
            seedOriginalScale =
                seedObject.transform.localScale;

            seedObject.SetActive(false);
        }

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled = false;
        }

        SetObjectActive(seedTapHint, false);
        SetObjectActive(seedSparkle, false);
        SetObjectActive(butterflyObject, false);
    }

    public void BeginSequence()
    {
        if (sequenceStarted)
        {
            return;
        }

        sequenceStarted = true;

        StartCoroutine(
            RevealSeedRoutine()
        );
    }

    private IEnumerator RevealSeedRoutine()
    {
        SetInstruction("");

        SetSubtitle(
            "Something above Lumi's bed began to glow."
        );

        if (seedSparkle != null &&
            seedRevealPoint != null)
        {
            seedSparkle.transform.position =
                seedRevealPoint.position;

            seedSparkle.SetActive(true);
        }

        yield return new WaitForSeconds(
            Mathf.Max(0f, revealDelay)
        );

        if (seedObject == null ||
            seedRevealPoint == null)
        {
            Debug.LogError(
                "Seed Object or Seed Reveal Point is missing."
            );

            yield break;
        }

        seedObject.transform.position =
            seedRevealPoint.position;

        seedObject.transform.rotation =
            seedRevealPoint.rotation;

        seedObject.transform.localScale =
            Vector3.zero;

        seedObject.SetActive(true);

        yield return ScaleObject(
            seedObject.transform,
            Vector3.zero,
            seedOriginalScale,
            seedRevealDuration
        );

        seedObject.transform.localScale =
            seedOriginalScale;

        seedRevealed = true;

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled = true;
        }

        SetObjectActive(seedTapHint, true);

        SetInstruction(
            "Tap the seed above Lumi's bed."
        );

        SetSubtitle(
            "A tiny seed appeared above Lumi's bed."
        );
    }

    public bool TrySelectSeed()
    {
        if (!seedRevealed ||
            seedSelected ||
            seedObject == null)
        {
            return false;
        }

        seedSelected = true;

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled = false;
        }

        SetObjectActive(seedTapHint, false);
        SetObjectActive(seedSparkle, false);

        StartCoroutine(
            FocusSeedAndButterflyRoutine()
        );

        return true;
    }

    private IEnumerator FocusSeedAndButterflyRoutine()
    {
        SetInstruction("");

        SetSubtitle(
            "You found the seed Lumi left behind."
        );

        if (seedFocusPoint == null)
        {
            Debug.LogError(
                "Seed Display Point is missing."
            );

            yield break;
        }

        /*
         * Detach the seed while preserving its current
         * world position.
         */
        seedObject.transform.SetParent(
            null,
            true
        );

        Vector3 seedStartPosition =
            seedObject.transform.position;

        Quaternion seedStartRotation =
            seedObject.transform.rotation;

        float elapsed = 0f;

        while (elapsed < seedMoveDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / seedMoveDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            /*
             * Read the scene target position every frame.
             * This allows the destination to continue
             * following the tracked AR page while moving.
             */
            Vector3 currentTargetPosition =
                seedFocusPoint.position;

            Quaternion currentTargetRotation =
                seedFocusPoint.rotation;

            seedObject.transform.position =
                Vector3.Lerp(
                    seedStartPosition,
                    currentTargetPosition,
                    smoothProgress
                );

            seedObject.transform.rotation =
                Quaternion.Slerp(
                    seedStartRotation,
                    currentTargetRotation,
                    smoothProgress
                );

            yield return null;
        }

        /*
         * Attach the seed to the scene display point.
         * It now follows the AR page, not the camera.
         */
        seedObject.transform.SetParent(
            seedFocusPoint,
            false
        );

        seedObject.transform.localPosition =
            Vector3.zero;

        seedObject.transform.localRotation =
            Quaternion.identity;

        yield return new WaitForSeconds(
            Mathf.Max(0f, focusPauseDuration)
        );

        yield return StartCoroutine(
            FlyButterflyRoutine()
        );
    }

    private IEnumerator FlyButterflyRoutine()
    {
        if (butterflyObject == null ||
            butterflyStartPoint == null ||
            butterflyControlPoint == null ||
            butterflyEndPoint == null)
        {
            Debug.LogError(
                "Butterfly object or flight points are missing."
            );

            yield break;
        }

        butterflyObject.transform.SetParent(
            null,
            true
        );

        butterflyObject.transform.position =
            butterflyStartPoint.position;

        butterflyObject.SetActive(true);

        PlayButterflyAnimation();

        SetSubtitle(
            "A butterfly flew in through the window."
        );

        Vector3 start =
            butterflyStartPoint.position;

        Vector3 control =
            butterflyControlPoint.position;

        Vector3 end =
            butterflyEndPoint.position;

        float elapsed = 0f;

        while (elapsed < butterflyFlightDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / butterflyFlightDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            butterflyObject.transform.position =
                CalculateQuadraticBezier(
                    start,
                    control,
                    end,
                    smoothProgress
                );

            yield return null;
        }

        butterflyObject.transform.position =
            end;

        /*
         * Attach the butterfly to its final point.
         * Its Animator continues playing, so the wings
         * continue flapping after the movement stops.
         */
        butterflyObject.transform.SetParent(
            butterflyEndPoint,
            true
        );

        butterflyObject.transform.localPosition =
            Vector3.zero;

        yield return new WaitForSeconds(0.5f);

        SetSubtitle(
            "\"This is a Memory Seed,\" said the butterfly."
        );

        yield return new WaitForSeconds(2.5f);

        SetSubtitle(
            "\"To help it bloom, you must enter " +
            "the Memory Garden.\""
        );

        yield return new WaitForSeconds(3f);

        SetInstruction(
            "Turn to the next page."
        );
    }

    private void PlayButterflyAnimation()
    {
        if (butterflyAnimator == null)
        {
            return;
        }

        butterflyAnimator.enabled = true;
        butterflyAnimator.speed = 1f;
        butterflyAnimator.applyRootMotion = false;

        butterflyAnimator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        int stateHash =
            Animator.StringToHash(
                butterflyFlyState
            );

        if (!butterflyAnimator.HasState(
            0,
            stateHash))
        {
            Debug.LogError(
                $"Butterfly Animator state " +
                $"'{butterflyFlyState}' was not found."
            );

            return;
        }

        butterflyAnimator.Play(
            stateHash,
            0,
            0f
        );

        butterflyAnimator.Update(0f);
    }

    private Vector3 CalculateQuadraticBezier(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float t)
    {
        float inverseT =
            1f - t;

        return
            inverseT * inverseT * start +
            2f * inverseT * t * control +
            t * t * end;
    }

    private IEnumerator ScaleObject(
        Transform target,
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        if (target == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            target.localScale =
                endScale;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            target.localScale =
                Vector3.Lerp(
                    startScale,
                    endScale,
                    smoothProgress
                );

            yield return null;
        }

        target.localScale =
            endScale;
    }

    private void SetObjectActive(
        GameObject targetObject,
        bool isActive)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(isActive);
        }
    }

    private void SetSubtitle(
        string message)
    {
        if (subtitleText != null)
        {
            subtitleText.text =
                message;
        }
    }

    private void SetInstruction(
        string message)
    {
        if (instructionText != null)
        {
            instructionText.text =
                message;
        }
    }
}