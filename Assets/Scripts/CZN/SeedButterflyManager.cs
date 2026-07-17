using System.Collections;
using TMPro;
using UnityEngine;

public class SeedButterflyManager : MonoBehaviour
{
    [Header("Seed")]
    [SerializeField] private GameObject seedObject;

    [Tooltip("The point where the seed first appears above Lumi's bed.")]
    [SerializeField] private Transform seedRevealPoint;

    [Tooltip("The final position of the seed inside the AR scene.")]
    [SerializeField] private Transform seedDisplayPoint;

    [SerializeField] private Collider seedClickCollider;
    [SerializeField] private GameObject seedTapHint;
    [SerializeField] private GameObject seedSparkle;

    [Header("Seed Timing")]
    [Tooltip("How long to wait before the seed appears.")]
    [SerializeField] private float revealDelay = 3f;

    [SerializeField] private float seedRevealDuration = 0.6f;
    [SerializeField] private float seedMoveDuration = 1.2f;
    [SerializeField] private float displayPauseDuration = 0.8f;

    [Header("Butterfly")]
    [SerializeField] private GameObject butterflyObject;
    [SerializeField] private Animator butterflyAnimator;

    [SerializeField]
    private string butterflyFlyState =
        "Base Layer.Move";

    [Tooltip("Place this point near the window.")]
    [SerializeField] private Transform butterflyStartPoint;

    [Tooltip(
        "Place this point between the window and the seed " +
        "to create a curved flight path."
    )]
    [SerializeField] private Transform butterflyControlPoint;

    [Tooltip(
        "The final position of the butterfly beside the seed."
    )]
    [SerializeField] private Transform butterflyEndPoint;

    [SerializeField] private float butterflyFlightDuration = 2.5f;

    [Header("Sequence Audio")]
    [Tooltip(
        "Used for short sounds such as the seed reveal " +
        "and seed selection sounds."
    )]
    [SerializeField] private AudioSource sequenceOneShotSource;

    [Tooltip(
        "Used for the butterfly wing sound that continues looping."
    )]
    [SerializeField] private AudioSource butterflyLoopSource;

    [Tooltip("Played when the seed appears.")]
    [SerializeField] private AudioClip seedRevealClip;

    [Tooltip("Played when the player selects the seed.")]
    [SerializeField] private AudioClip seedSelectClip;

    [Tooltip("Looped while the butterfly is visible.")]
    [SerializeField] private AudioClip butterflyWingLoopClip;

    [Header("Text")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text instructionText;

    private Vector3 seedOriginalScale;
    private Vector3 butterflyOriginalScale;

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

        if (butterflyObject != null)
        {
            butterflyOriginalScale =
                butterflyObject.transform.localScale;

            butterflyObject.SetActive(false);
        }

        SetObjectActive(seedTapHint, false);
        SetObjectActive(seedSparkle, false);

        InitialiseAudioSources();
    }

    private void OnDisable()
    {
        if (sequenceOneShotSource != null)
        {
            sequenceOneShotSource.Stop();
        }

        StopButterflyWingLoop();
    }

    private void InitialiseAudioSources()
    {
        if (sequenceOneShotSource != null)
        {
            sequenceOneShotSource.playOnAwake = false;
            sequenceOneShotSource.loop = false;
            sequenceOneShotSource.spatialBlend = 0f;
        }

        if (butterflyLoopSource != null)
        {
            butterflyLoopSource.playOnAwake = false;
            butterflyLoopSource.loop = true;
            butterflyLoopSource.spatialBlend = 0f;
        }
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

        if (seedRevealPoint == null)
        {
            Debug.LogError(
                "Seed Reveal Point has not been assigned."
            );

            yield break;
        }

        if (seedSparkle != null)
        {
            seedSparkle.transform.SetParent(
                seedRevealPoint,
                false
            );

            seedSparkle.transform.localPosition =
                Vector3.zero;

            seedSparkle.transform.localRotation =
                Quaternion.identity;

            seedSparkle.SetActive(true);
        }

        /*
         * Wait before the seed appears.
         */
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                revealDelay
            )
        );

        if (seedObject == null)
        {
            Debug.LogError(
                "Seed Object has not been assigned."
            );

            yield break;
        }

        /*
         * Play the seed reveal sound.
         */
        PlaySequenceOneShot(
            seedRevealClip
        );

        seedObject.transform.SetParent(
            seedRevealPoint,
            false
        );

        seedObject.transform.localPosition =
            Vector3.zero;

        seedObject.transform.localRotation =
            Quaternion.identity;

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

        SetObjectActive(
            seedTapHint,
            true
        );

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

        /*
         * Play the seed pickup sound.
         */
        PlaySequenceOneShot(
            seedSelectClip
        );

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled = false;
        }

        SetObjectActive(
            seedTapHint,
            false
        );

        SetObjectActive(
            seedSparkle,
            false
        );

        StartCoroutine(
            MoveSeedAndButterflyRoutine()
        );

        return true;
    }

    private IEnumerator MoveSeedAndButterflyRoutine()
    {
        SetInstruction("");

        SetSubtitle(
            "You found the seed Lumi left behind."
        );

        if (seedDisplayPoint == null)
        {
            Debug.LogError(
                "Seed Display Point has not been assigned."
            );

            yield break;
        }

        /*
         * Detach the seed while keeping its
         * current world position and rotation.
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

        float safeDuration =
            Mathf.Max(
                0.01f,
                seedMoveDuration
            );

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / safeDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Vector3 currentEndPosition =
                seedDisplayPoint.position;

            Quaternion currentEndRotation =
                seedDisplayPoint.rotation;

            seedObject.transform.position =
                Vector3.Lerp(
                    seedStartPosition,
                    currentEndPosition,
                    smoothProgress
                );

            seedObject.transform.rotation =
                Quaternion.Slerp(
                    seedStartRotation,
                    currentEndRotation,
                    smoothProgress
                );

            yield return null;
        }

        seedObject.transform.SetParent(
            seedDisplayPoint,
            false
        );

        seedObject.transform.localPosition =
            Vector3.zero;

        seedObject.transform.localRotation =
            Quaternion.identity;

        seedObject.transform.localScale =
            seedOriginalScale;

        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                displayPauseDuration
            )
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
                "Butterfly Object or one of the " +
                "flight points is missing."
            );

            yield break;
        }

        /*
         * Detach the butterfly while keeping its
         * current correct world-facing direction.
         */
        butterflyObject.transform.SetParent(
            null,
            true
        );

        /*
         * Store the butterfly's current correct direction.
         * Do not use ButterflyStartPoint.rotation.
         */
        Quaternion butterflyFlightRotation =
            butterflyObject.transform.rotation;

        /*
         * Move the butterfly to the starting position.
         * Its rotation remains unchanged.
         */
        butterflyObject.transform.position =
            butterflyStartPoint.position;

        butterflyObject.transform.rotation =
            butterflyFlightRotation;

        butterflyObject.transform.localScale =
            butterflyOriginalScale;

        butterflyObject.SetActive(true);

        PlayButterflyAnimation();

        /*
         * Start the looping butterfly wing sound.
         */
        StartButterflyWingLoop();

        SetSubtitle(
            "A butterfly flew in through the window."
        );

        Vector3 startPosition =
            butterflyStartPoint.position;

        float elapsed = 0f;

        float safeDuration =
            Mathf.Max(
                0.01f,
                butterflyFlightDuration
            );

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / safeDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Vector3 currentControlPosition =
                butterflyControlPoint.position;

            Vector3 currentEndPosition =
                butterflyEndPoint.position;

            butterflyObject.transform.position =
                CalculateQuadraticBezier(
                    startPosition,
                    currentControlPosition,
                    currentEndPosition,
                    smoothProgress
                );

            /*
             * Keep the same correct facing direction
             * throughout the entire flight.
             */
            butterflyObject.transform.rotation =
                butterflyFlightRotation;

            yield return null;
        }

        /*
         * Ensure that the butterfly reaches
         * the exact final position.
         */
        butterflyObject.transform.position =
            butterflyEndPoint.position;

        butterflyObject.transform.rotation =
            butterflyFlightRotation;

        /*
         * Attach it to ButterflyEndPoint while
         * preserving the current world rotation.
         */
        butterflyObject.transform.SetParent(
            butterflyEndPoint,
            true
        );

        butterflyObject.transform.localPosition =
            Vector3.zero;

        butterflyObject.transform.localScale =
            butterflyOriginalScale;

        /*
         * Do not use:
         *
         * butterflyObject.transform.localRotation =
         *     Quaternion.identity;
         *
         * Otherwise the butterfly may suddenly turn
         * when it reaches the final point.
         */

        yield return new WaitForSeconds(
            0.5f
        );

        SetSubtitle(
            "\"This is a Memory Seed,\" said the butterfly."
        );

        yield return new WaitForSeconds(
            2.5f
        );

        SetSubtitle(
            "\"To help it bloom, you must enter " +
            "the Memory Garden.\""
        );

        yield return new WaitForSeconds(
            3f
        );

        SetInstruction(
            "Turn to the next page."
        );
    }

    private void PlayButterflyAnimation()
    {
        if (butterflyAnimator == null)
        {
            Debug.LogError(
                "Butterfly Animator has not been assigned."
            );

            return;
        }

        butterflyAnimator.enabled = true;
        butterflyAnimator.speed = 1f;
        butterflyAnimator.applyRootMotion = false;

        butterflyAnimator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        butterflyAnimator.Rebind();
        butterflyAnimator.Update(0f);

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

    private void PlaySequenceOneShot(
        AudioClip clip)
    {
        if (sequenceOneShotSource == null ||
            clip == null)
        {
            return;
        }

        sequenceOneShotSource.PlayOneShot(
            clip
        );
    }

    private void StartButterflyWingLoop()
    {
        if (butterflyLoopSource == null ||
            butterflyWingLoopClip == null)
        {
            return;
        }

        butterflyLoopSource.Stop();

        butterflyLoopSource.clip =
            butterflyWingLoopClip;

        butterflyLoopSource.loop =
            true;

        butterflyLoopSource.Play();
    }

    private void StopButterflyWingLoop()
    {
        if (butterflyLoopSource == null)
        {
            return;
        }

        if (butterflyLoopSource.isPlaying)
        {
            butterflyLoopSource.Stop();
        }

        butterflyLoopSource.clip =
            null;
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
            targetObject.SetActive(
                isActive
            );
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