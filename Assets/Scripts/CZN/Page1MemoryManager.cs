using System.Collections;
using UnityEngine;

public class Page1MemoryManager : MonoBehaviour
{
    private const int TotalMemoryCount = 4;

    [Header("Dialogue")]
    [Tooltip(
        "Controls the opening narration, memory hints, " +
        "typewriter effect and dialogue UI."
    )]
    [SerializeField]
    private Page1DialogueController dialogueController;

    [Header("Next Story Sequence")]
    [SerializeField]
    private SeedButterflyManager seedButterflyManager;

    [Header("Run Memory")]
    [SerializeField]
    private GameObject runMemoryRoot;

    [SerializeField]
    private Animator runDogAnimator;

    [Header("Sleep Memory")]
    [SerializeField]
    private GameObject sleepMemoryRoot;

    [SerializeField]
    private Animator sleepDogAnimator;

    [Header("Door Memory")]
    [SerializeField]
    private GameObject doorMemoryRoot;

    [SerializeField]
    private Animator doorDogAnimator;

    [Header("Food Memory")]
    [SerializeField]
    private GameObject foodMemoryRoot;

    [SerializeField]
    private Animator foodDogAnimator;

    [Header("Paw Markers")]
    [SerializeField]
    private GameObject ballPawMarker;

    [SerializeField]
    private GameObject bedPawMarker;

    [SerializeField]
    private GameObject collarPawMarker;

    [SerializeField]
    private GameObject foodPawMarker;

    [Header("Sparkle Effects")]
    [SerializeField]
    private GameObject ballSparkleEffect;

    [SerializeField]
    private GameObject bedSparkleEffect;

    [SerializeField]
    private GameObject collarSparkleEffect;

    [SerializeField]
    private GameObject foodSparkleEffect;

    [Header("First Tap Tutorial")]
    [SerializeField]
    private GameObject firstTapHandHint;

    [Tooltip("Require the player to tap the ball first.")]
    [SerializeField]
    private bool requireBallFirst = true;

    [Header("Transition Timing")]
    [SerializeField]
    private float appearDuration = 0.35f;

    [SerializeField]
    private float disappearDuration = 0.2f;

    [Tooltip(
        "The fourth selected memory remains visible " +
        "for this duration before disappearing."
    )]
    [SerializeField]
    private float finalMemoryDuration = 10f;

    [Header("Run Around Ball")]
    [Tooltip(
        "72 degrees per second means one complete circle " +
        "in approximately five seconds."
    )]
    [SerializeField]
    private float runRotationSpeed = 72f;

    [Header("Memory Audio")]
    [Tooltip(
        "Used for short sounds such as entering a memory."
    )]
    [SerializeField]
    private AudioSource memoryOneShotSource;

    [Tooltip(
        "Used for action sounds that continue until " +
        "another memory is selected."
    )]
    [SerializeField]
    private AudioSource memoryLoopSource;

    [Tooltip(
        "Played once whenever the player enters a memory."
    )]
    [SerializeField]
    private AudioClip memoryEnterClip;

    [Tooltip(
        "Looped while Lumi runs around the ball."
    )]
    [SerializeField]
    private AudioClip runLoopClip;

    [Tooltip(
        "Looped while Lumi sleeps."
    )]
    [SerializeField]
    private AudioClip sleepLoopClip;

    [Tooltip(
        "Looped while Lumi waits excitedly by the door."
    )]
    [SerializeField]
    private AudioClip doorBarkClip;

    [Tooltip(
        "Looped while Lumi eats."
    )]
    [SerializeField]
    private AudioClip foodLoopClip;

    private bool ballCompleted;
    private bool bedCompleted;
    private bool collarCompleted;
    private bool foodCompleted;

    private bool introCompleted;
    private bool isSwitching;
    private bool rotateRunMemory;
    private bool allMemoriesFinished;

    private int completedMemoryCount;

    private GameObject currentMemoryRoot;
    private Vector3 currentOriginalScale;

    private Vector3 runOriginalScale;
    private Vector3 sleepOriginalScale;
    private Vector3 doorOriginalScale;
    private Vector3 foodOriginalScale;

    private Quaternion runOriginalRotation;

    private void Awake()
    {
        InitialiseMemoryRoot(
            runMemoryRoot,
            out runOriginalScale
        );

        if (runMemoryRoot != null)
        {
            runOriginalRotation =
                runMemoryRoot.transform.localRotation;
        }

        InitialiseMemoryRoot(
            sleepMemoryRoot,
            out sleepOriginalScale
        );

        InitialiseMemoryRoot(
            doorMemoryRoot,
            out doorOriginalScale
        );

        InitialiseMemoryRoot(
            foodMemoryRoot,
            out foodOriginalScale
        );

        SetObjectActive(
            ballPawMarker,
            true
        );

        SetObjectActive(
            bedPawMarker,
            true
        );

        SetObjectActive(
            collarPawMarker,
            true
        );

        SetObjectActive(
            foodPawMarker,
            true
        );

        SetObjectActive(
            ballSparkleEffect,
            true
        );

        SetObjectActive(
            bedSparkleEffect,
            true
        );

        SetObjectActive(
            collarSparkleEffect,
            true
        );

        SetObjectActive(
            foodSparkleEffect,
            true
        );

        /*
         * The hand must not appear before the
         * introduction tells the player to interact.
         */
        SetObjectActive(
            firstTapHandHint,
            false
        );

        InitialiseAudioSources();
    }

    private IEnumerator Start()
    {
        introCompleted = false;

        /*
         * Keep the hand hidden while the opening
         * narration is being typed.
         */
        SetObjectActive(
            firstTapHandHint,
            false
        );

        if (dialogueController != null)
        {
            /*
             * Wait until every intro line has finished.
             *
             * The final line should tell the player:
             * "Begin with Lumi's favourite ball.
             * Tap the object marked by her paw print."
             */
            yield return
                dialogueController
                    .PlayIntroSequence();
        }
        else
        {
            Debug.LogWarning(
                "Page1DialogueController has not been assigned. " +
                "The opening narration will be skipped."
            );
        }

        /*
         * The final instruction has finished typing.
         * Memory interaction can now begin.
         */
        introCompleted = true;

        /*
         * Show the hand only after the final intro
         * instruction has completely appeared.
         */
        SetObjectActive(
            firstTapHandHint,
            true
        );

        Debug.Log(
            "Page 1 opening narration has finished. " +
            "The ball hand hint is now visible."
        );
    }

    private void Update()
    {
        if (rotateRunMemory &&
            runMemoryRoot != null &&
            runMemoryRoot.activeSelf)
        {
            runMemoryRoot.transform.Rotate(
                0f,
                runRotationSpeed * Time.deltaTime,
                0f,
                Space.Self
            );
        }
    }

    private void OnDisable()
    {
        rotateRunMemory = false;

        StopMemoryLoop();

        if (memoryOneShotSource != null)
        {
            memoryOneShotSource.Stop();
        }
    }

    private void InitialiseAudioSources()
    {
        if (memoryOneShotSource != null)
        {
            memoryOneShotSource.playOnAwake =
                false;

            memoryOneShotSource.loop =
                false;

            memoryOneShotSource.spatialBlend =
                0f;
        }

        if (memoryLoopSource != null)
        {
            memoryLoopSource.playOnAwake =
                false;

            memoryLoopSource.loop =
                true;

            memoryLoopSource.spatialBlend =
                0f;
        }
    }

    public bool TryPlayMemory(
        MemoryType memoryType)
    {
        /*
         * Do not allow the player to click any
         * memory while the introduction is playing.
         */
        if (!introCompleted)
        {
            return false;
        }

        if (isSwitching ||
            allMemoriesFinished)
        {
            return false;
        }

        /*
         * The ball must be selected first.
         */
        if (completedMemoryCount == 0 &&
            requireBallFirst &&
            memoryType != MemoryType.Ball)
        {
            return false;
        }

        if (IsMemoryCompleted(
            memoryType))
        {
            return false;
        }

        if (!HasRequiredReferences(
            memoryType))
        {
            return false;
        }

        MarkCompleted(
            memoryType
        );

        StartCoroutine(
            SwitchMemory(
                memoryType
            )
        );

        return true;
    }

    private void InitialiseMemoryRoot(
        GameObject memoryRoot,
        out Vector3 originalScale)
    {
        originalScale =
            Vector3.one;

        if (memoryRoot == null)
        {
            return;
        }

        originalScale =
            memoryRoot.transform.localScale;

        memoryRoot.SetActive(
            false
        );
    }

    private bool IsMemoryCompleted(
        MemoryType memoryType)
    {
        switch (memoryType)
        {
            case MemoryType.Ball:
                return ballCompleted;

            case MemoryType.Bed:
                return bedCompleted;

            case MemoryType.Collar:
                return collarCompleted;

            case MemoryType.Food:
                return foodCompleted;

            default:
                return true;
        }
    }

    private bool HasRequiredReferences(
        MemoryType memoryType)
    {
        GameObject memoryRoot = null;
        Animator memoryAnimator = null;

        switch (memoryType)
        {
            case MemoryType.Ball:
                memoryRoot =
                    runMemoryRoot;

                memoryAnimator =
                    runDogAnimator;

                break;

            case MemoryType.Bed:
                memoryRoot =
                    sleepMemoryRoot;

                memoryAnimator =
                    sleepDogAnimator;

                break;

            case MemoryType.Collar:
                memoryRoot =
                    doorMemoryRoot;

                memoryAnimator =
                    doorDogAnimator;

                break;

            case MemoryType.Food:
                memoryRoot =
                    foodMemoryRoot;

                memoryAnimator =
                    foodDogAnimator;

                break;
        }

        if (memoryRoot == null ||
            memoryAnimator == null)
        {
            Debug.LogError(
                $"{memoryType} memory is missing " +
                "its Memory Root or Animator."
            );

            return false;
        }

        return true;
    }

    private void MarkCompleted(
        MemoryType memoryType)
    {
        switch (memoryType)
        {
            case MemoryType.Ball:
                ballCompleted = true;
                break;

            case MemoryType.Bed:
                bedCompleted = true;
                break;

            case MemoryType.Collar:
                collarCompleted = true;
                break;

            case MemoryType.Food:
                foodCompleted = true;
                break;
        }

        completedMemoryCount++;

        /*
         * Permanently hide the tutorial hand after
         * the first successful click on the ball.
         */
        if (completedMemoryCount == 1)
        {
            SetObjectActive(
                firstTapHandHint,
                false
            );
        }

        Debug.Log(
            $"Completed memory count: " +
            $"{completedMemoryCount}/" +
            $"{TotalMemoryCount}"
        );
    }

    private IEnumerator SwitchMemory(
        MemoryType memoryType)
    {
        isSwitching = true;
        rotateRunMemory = false;

        StopMemoryLoop();

        /*
         * Hide the previously active memory.
         */
        if (currentMemoryRoot != null &&
            currentMemoryRoot.activeSelf)
        {
            yield return ScaleObject(
                currentMemoryRoot.transform,
                currentMemoryRoot
                    .transform.localScale,
                Vector3.zero,
                disappearDuration
            );

            currentMemoryRoot.SetActive(
                false
            );

            currentMemoryRoot
                .transform.localScale =
                currentOriginalScale;
        }

        GameObject selectedRoot = null;
        Animator selectedAnimator = null;

        GameObject selectedPawMarker = null;
        GameObject selectedSparkleEffect = null;

        Vector3 selectedOriginalScale =
            Vector3.one;

        string stateName = "";

        AudioClip selectedLoopClip = null;

        switch (memoryType)
        {
            case MemoryType.Ball:
                selectedRoot =
                    runMemoryRoot;

                selectedAnimator =
                    runDogAnimator;

                selectedPawMarker =
                    ballPawMarker;

                selectedSparkleEffect =
                    ballSparkleEffect;

                selectedOriginalScale =
                    runOriginalScale;

                stateName =
                    "Base Layer.Run";

                selectedLoopClip =
                    runLoopClip;

                if (runMemoryRoot != null)
                {
                    runMemoryRoot
                        .transform.localRotation =
                        runOriginalRotation;
                }

                break;

            case MemoryType.Bed:
                selectedRoot =
                    sleepMemoryRoot;

                selectedAnimator =
                    sleepDogAnimator;

                selectedPawMarker =
                    bedPawMarker;

                selectedSparkleEffect =
                    bedSparkleEffect;

                selectedOriginalScale =
                    sleepOriginalScale;

                stateName =
                    "Base Layer.Rest";

                selectedLoopClip =
                    sleepLoopClip;

                break;

            case MemoryType.Collar:
                selectedRoot =
                    doorMemoryRoot;

                selectedAnimator =
                    doorDogAnimator;

                selectedPawMarker =
                    collarPawMarker;

                selectedSparkleEffect =
                    collarSparkleEffect;

                selectedOriginalScale =
                    doorOriginalScale;

                stateName =
                    "Base Layer.Jump";

                selectedLoopClip =
                    doorBarkClip;

                break;

            case MemoryType.Food:
                selectedRoot =
                    foodMemoryRoot;

                selectedAnimator =
                    foodDogAnimator;

                selectedPawMarker =
                    foodPawMarker;

                selectedSparkleEffect =
                    foodSparkleEffect;

                selectedOriginalScale =
                    foodOriginalScale;

                stateName =
                    "Base Layer.Eat";

                selectedLoopClip =
                    foodLoopClip;

                break;
        }

        if (selectedRoot == null ||
            selectedAnimator == null)
        {
            Debug.LogError(
                $"{memoryType} memory could not be started."
            );

            isSwitching = false;
            yield break;
        }

        SetObjectActive(
            selectedPawMarker,
            false
        );

        SetObjectActive(
            selectedSparkleEffect,
            false
        );

        selectedRoot.transform.localScale =
            Vector3.zero;

        selectedRoot.SetActive(
            true
        );

        PrepareAndPlayAnimator(
            selectedAnimator,
            stateName
        );

        PlayMemoryOneShot(
            memoryEnterClip
        );

        StartMemoryLoop(
            selectedLoopClip
        );

        currentMemoryRoot =
            selectedRoot;

        currentOriginalScale =
            selectedOriginalScale;

        rotateRunMemory =
            memoryType == MemoryType.Ball;

        yield return ScaleObject(
            selectedRoot.transform,
            Vector3.zero,
            selectedOriginalScale,
            appearDuration
        );

        selectedRoot.transform.localScale =
            selectedOriginalScale;

        /*
         * Display the editable memory narration
         * using the GameHint typewriter effect.
         */
        if (dialogueController != null)
        {
            dialogueController.ShowMemoryHint(
                memoryType
            );
        }

        /*
         * The first three selected memories stay
         * visible until the next object is selected.
         */
        if (completedMemoryCount <
            TotalMemoryCount)
        {
            isSwitching = false;
            yield break;
        }

        /*
         * The fourth memory stays visible for the
         * selected duration before disappearing.
         */
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                finalMemoryDuration
            )
        );

        rotateRunMemory = false;

        StopMemoryLoop();

        yield return ScaleObject(
            selectedRoot.transform,
            selectedRoot
                .transform.localScale,
            Vector3.zero,
            disappearDuration
        );

        selectedRoot.SetActive(
            false
        );

        selectedRoot.transform.localScale =
            selectedOriginalScale;

        currentMemoryRoot = null;

        isSwitching = false;
        allMemoriesFinished = true;

        /*
         * Wait until the final memory narration,
         * voice clip and Hold After are complete
         * before beginning the seed sequence.
         */
        yield return OnAllMemoriesCompleted();
    }

    private void PrepareAndPlayAnimator(
        Animator animator,
        string fullStateName)
    {
        if (animator == null)
        {
            return;
        }

        animator.enabled =
            true;

        animator.speed =
            1f;

        animator.applyRootMotion =
            false;

        animator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        animator.Rebind();

        animator.Update(
            0f
        );

        int stateHash =
            Animator.StringToHash(
                fullStateName
            );

        if (!animator.HasState(
            0,
            stateHash))
        {
            Debug.LogError(
                $"{animator.gameObject.name}: " +
                $"Animator state '{fullStateName}' " +
                "was not found."
            );

            return;
        }

        animator.Play(
            stateHash,
            0,
            0f
        );

        animator.Update(
            0f
        );

        Debug.Log(
            $"{animator.gameObject.name} " +
            $"is playing {fullStateName}."
        );
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
            elapsed +=
                Time.deltaTime;

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

    private IEnumerator OnAllMemoriesCompleted()
    {
        StopMemoryLoop();

        Debug.Log(
            "All four Lumi memories " +
            "have been completed."
        );

        /*
         * Wait until the text has finished typing,
         * the voice clip has finished playing,
         * and Hold After has finished.
         */
        if (dialogueController != null)
        {
            yield return
                dialogueController
                    .PlayAllMemoriesCompletedHint();
        }

        /*
         * Begin the seed sequence only after
         * the final memory narration is complete.
         */
        if (seedButterflyManager != null)
        {
            seedButterflyManager.BeginSequence();
        }
        else
        {
            Debug.LogError(
                "SeedButterflyManager has not been assigned."
            );
        }
    }

    public void HideCurrentMemory()
    {
        rotateRunMemory = false;

        StopMemoryLoop();

        if (currentMemoryRoot != null)
        {
            currentMemoryRoot.SetActive(
                false
            );

            currentMemoryRoot
                .transform.localScale =
                currentOriginalScale;
        }

        currentMemoryRoot = null;
    }

    private void PlayMemoryOneShot(
        AudioClip clip)
    {
        if (memoryOneShotSource == null ||
            clip == null)
        {
            return;
        }

        memoryOneShotSource.PlayOneShot(
            clip
        );
    }

    private void StartMemoryLoop(
        AudioClip clip)
    {
        StopMemoryLoop();

        if (memoryLoopSource == null ||
            clip == null)
        {
            return;
        }

        memoryLoopSource.clip =
            clip;

        memoryLoopSource.loop =
            true;

        memoryLoopSource.Play();
    }

    private void StopMemoryLoop()
    {
        if (memoryLoopSource == null)
        {
            return;
        }

        if (memoryLoopSource.isPlaying)
        {
            memoryLoopSource.Stop();
        }

        memoryLoopSource.clip =
            null;
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