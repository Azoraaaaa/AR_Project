using System.Collections;
using UnityEngine;

public class SeedButterflyManager : MonoBehaviour
{
    private enum SeedStage
    {
        Hidden,
        WaitingForFirstTap,
        ButterflySequence,
        WaitingForCollection,
        Collected
    }

    [Header("Dialogue")]
    [Tooltip(
        "Controls GameHint, StoryBG, typewriter text " +
        "and all Page 1 dialogue."
    )]
    [SerializeField]
    private Page1DialogueController dialogueController;

    [Header("Seed")]
    [SerializeField]
    private GameObject seedObject;

    [Tooltip(
        "The point where the seed first appears " +
        "above Lumi's bed."
    )]
    [SerializeField]
    private Transform seedRevealPoint;

    [Tooltip(
        "The position where the seed waits while " +
        "the butterfly speaks."
    )]
    [SerializeField]
    private Transform seedDisplayPoint;

    [SerializeField]
    private Collider seedClickCollider;

    [SerializeField]
    private GameObject seedTapHint;

    [SerializeField]
    private GameObject seedSparkle;

    [Header("Seed Timing")]
    [Tooltip(
        "How long to wait after all memories finish " +
        "before the seed appears."
    )]
    [SerializeField]
    private float revealDelay = 3f;

    [SerializeField]
    private float seedRevealDuration = 0.6f;

    [SerializeField]
    private float seedMoveDuration = 1.2f;

    [Tooltip(
        "How long the seed waits at SeedDisplayPoint " +
        "before the butterfly begins flying."
    )]
    [SerializeField]
    private float displayPauseDuration = 0.8f;

    [Header("Seed Collection")]
    [Tooltip(
        "Played when the player taps the seed " +
        "for the second time to collect it."
    )]
    [SerializeField]
    private AudioClip seedCollectClip;

    [Tooltip(
        "How long the seed takes to shrink and disappear."
    )]
    [SerializeField]
    private float seedCollectDuration = 0.4f;

    [Header("Butterfly")]
    [SerializeField]
    private GameObject butterflyObject;

    [SerializeField]
    private Animator butterflyAnimator;

    [SerializeField]
    private string butterflyFlyState =
        "Base Layer.Move";

    [Tooltip(
        "Place this point near the window."
    )]
    [SerializeField]
    private Transform butterflyStartPoint;

    [Tooltip(
        "Place this point between the window and the seed " +
        "to create a curved flight path."
    )]
    [SerializeField]
    private Transform butterflyControlPoint;

    [Tooltip(
        "The final position of the butterfly beside the seed."
    )]
    [SerializeField]
    private Transform butterflyEndPoint;

    [SerializeField]
    private float butterflyFlightDuration = 2.5f;

    [Header("Sequence Audio")]
    [Tooltip(
        "Used for short sounds such as seed reveal, " +
        "seed selection and seed collection."
    )]
    [SerializeField]
    private AudioSource sequenceOneShotSource;

    [Tooltip(
        "Used to play the butterfly wing sound once."
    )]
    [SerializeField]
    private AudioSource butterflyLoopSource;

    [Tooltip(
        "Played when the seed appears."
    )]
    [SerializeField]
    private AudioClip seedRevealClip;

    [Tooltip(
        "Played when the seed is selected for the first time."
    )]
    [SerializeField]
    private AudioClip seedSelectClip;

    [Tooltip(
        "Played once when the butterfly appears."
    )]
    [SerializeField]
    private AudioClip butterflyWingLoopClip;

    private Vector3 seedOriginalScale;
    private Vector3 butterflyOriginalScale;

    private bool sequenceStarted;

    private SeedStage seedStage =
        SeedStage.Hidden;

    private void Awake()
    {
        InitialiseSeed();
        InitialiseButterfly();
        InitialiseAudioSources();
    }

    private void OnDisable()
    {
        if (sequenceOneShotSource != null)
        {
            sequenceOneShotSource.Stop();
        }

        StopButterflyWingSound();
    }

    private void InitialiseSeed()
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

        SetObjectActive(
            seedTapHint,
            false
        );

        SetObjectActive(
            seedSparkle,
            false
        );

        seedStage =
            SeedStage.Hidden;
    }

    private void InitialiseButterfly()
    {
        if (butterflyObject == null)
        {
            return;
        }

        butterflyOriginalScale =
            butterflyObject.transform.localScale;

        butterflyObject.SetActive(false);
    }

    private void InitialiseAudioSources()
    {
        if (sequenceOneShotSource != null)
        {
            sequenceOneShotSource.playOnAwake =
                false;

            sequenceOneShotSource.loop =
                false;

            sequenceOneShotSource.spatialBlend =
                0f;
        }

        if (butterflyLoopSource != null)
        {
            butterflyLoopSource.playOnAwake =
                false;

            /*
             * The butterfly wing sound only plays once.
             */
            butterflyLoopSource.loop =
                false;

            butterflyLoopSource.spatialBlend =
                0f;
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
        if (seedRevealPoint == null)
        {
            Debug.LogError(
                "Seed Reveal Point has not been assigned."
            );

            yield break;
        }

        /*
         * Keep the all-memories-completed hint visible
         * while waiting for the seed to appear.
         */
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

        seedStage =
            SeedStage.WaitingForFirstTap;

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled =
                true;
        }

        SetObjectActive(
            seedTapHint,
            true
        );

        if (dialogueController != null)
        {
            dialogueController
                .ShowSeedRevealHint();
        }
        else
        {
            Debug.LogWarning(
                "Page1DialogueController has not been assigned."
            );
        }

        Debug.Log(
            "The Memory Seed has appeared and is waiting " +
            "for the first tap."
        );
    }

    public bool TrySelectSeed()
    {
        if (seedObject == null)
        {
            return false;
        }

        /*
         * First tap:
         * move the seed and begin the butterfly sequence.
         */
        if (seedStage ==
            SeedStage.WaitingForFirstTap)
        {
            seedStage =
                SeedStage.ButterflySequence;

            if (seedClickCollider != null)
            {
                seedClickCollider.enabled =
                    false;
            }

            SetObjectActive(
                seedTapHint,
                false
            );

            SetObjectActive(
                seedSparkle,
                false
            );

            PlaySequenceOneShot(
                seedSelectClip
            );

            if (dialogueController != null)
            {
                dialogueController
                    .ShowSeedFirstTapHint();
            }

            StartCoroutine(
                MoveSeedAndButterflyRoutine()
            );

            return true;
        }

        /*
         * Second tap:
         * collect the seed and complete Page 1.
         */
        if (seedStage ==
            SeedStage.WaitingForCollection)
        {
            StartCoroutine(
                CollectSeedRoutine()
            );

            return true;
        }

        return false;
    }

    private IEnumerator MoveSeedAndButterflyRoutine()
    {
        if (seedDisplayPoint == null)
        {
            Debug.LogError(
                "Seed Display Point has not been assigned."
            );

            seedStage =
                SeedStage.WaitingForFirstTap;

            if (seedClickCollider != null)
            {
                seedClickCollider.enabled =
                    true;
            }

            SetObjectActive(
                seedTapHint,
                true
            );

            yield break;
        }

        /*
         * Detach the seed while preserving its
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

            /*
             * Read the destination every frame because
             * the AR Image Target may move.
             */
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

        /*
         * Attach the seed to SeedDisplayPoint.
         * It follows the AR scene, not the camera.
         */
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

            /*
             * Permit seed collection even if the
             * butterfly references are incomplete.
             */
            EnableSeedCollection();

            yield break;
        }

        /*
         * Detach the butterfly while preserving its
         * current world-facing direction.
         */
        butterflyObject.transform.SetParent(
            null,
            true
        );

        Quaternion butterflyFlightRotation =
            butterflyObject.transform.rotation;

        butterflyObject.transform.position =
            butterflyStartPoint.position;

        butterflyObject.transform.rotation =
            butterflyFlightRotation;

        butterflyObject.transform.localScale =
            butterflyOriginalScale;

        butterflyObject.SetActive(true);

        PlayButterflyAnimation();

        /*
         * Play the wing sound once when
         * the butterfly starts flying.
         */
        PlayButterflyWingOnce();

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
             * Keep the same facing direction during
             * the complete flight.
             */
            butterflyObject.transform.rotation =
                butterflyFlightRotation;

            yield return null;
        }

        butterflyObject.transform.position =
            butterflyEndPoint.position;

        butterflyObject.transform.rotation =
            butterflyFlightRotation;

        /*
         * Parent the butterfly to the final point while
         * preserving its current world rotation.
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
         * Do not set localRotation to Quaternion.identity.
         * This prevents the butterfly from turning
         * when it reaches the destination.
         */

        yield return new WaitForSeconds(
            0.5f
        );

        if (dialogueController != null)
        {
            yield return
                dialogueController
                    .PlayButterflyDialogue();
        }
        else
        {
            Debug.LogWarning(
                "Page1DialogueController has not been assigned. " +
                "Butterfly dialogue will be skipped."
            );

            yield return new WaitForSeconds(
                1f
            );
        }

        EnableSeedCollection();
    }

    private void EnableSeedCollection()
    {
        seedStage =
            SeedStage.WaitingForCollection;

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled =
                true;
        }

        SetObjectActive(
            seedTapHint,
            true
        );

        if (dialogueController != null)
        {
            dialogueController
                .ShowCollectSeedHint();
        }

        Debug.Log(
            "Butterfly dialogue has finished. " +
            "The Memory Seed can now be collected."
        );
    }

    private IEnumerator CollectSeedRoutine()
    {
        seedStage =
            SeedStage.Collected;

        if (seedClickCollider != null)
        {
            seedClickCollider.enabled =
                false;
        }

        SetObjectActive(
            seedTapHint,
            false
        );

        SetObjectActive(
            seedSparkle,
            false
        );

        PlaySequenceOneShot(
            seedCollectClip
        );

        if (seedObject != null)
        {
            Vector3 collectionStartScale =
                seedObject.transform.localScale;

            yield return ScaleObject(
                seedObject.transform,
                collectionStartScale,
                Vector3.zero,
                seedCollectDuration
            );

            seedObject.SetActive(false);

            /*
             * Restore the scale in case the scene
             * is reset or replayed later.
             */
            seedObject.transform.localScale =
                seedOriginalScale;
        }

        /*
         * Stop the wing sound if the clip is still playing.
         * If it has already finished, this has no effect.
         */
        StopButterflyWingSound();

        if (butterflyObject != null)
        {
            butterflyObject.SetActive(false);
        }

        if (dialogueController != null)
        {
            dialogueController
                .ShowPageCompletedHint();
        }

        Debug.Log(
            "The Memory Seed has been collected. " +
            "Page 1 is complete."
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

        butterflyAnimator.enabled =
            true;

        butterflyAnimator.speed =
            1f;

        butterflyAnimator.applyRootMotion =
            false;

        butterflyAnimator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        butterflyAnimator.Rebind();

        butterflyAnimator.Update(
            0f
        );

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

        butterflyAnimator.Update(
            0f
        );
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

    private void PlayButterflyWingOnce()
    {
        if (butterflyLoopSource == null ||
            butterflyWingLoopClip == null)
        {
            return;
        }

        /*
         * Stop any previous playback before
         * playing the wing sound.
         */
        butterflyLoopSource.Stop();

        butterflyLoopSource.clip =
            butterflyWingLoopClip;

        /*
         * Play once instead of continuously looping.
         */
        butterflyLoopSource.loop =
            false;

        butterflyLoopSource.Play();
    }

    private void StopButterflyWingSound()
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
}