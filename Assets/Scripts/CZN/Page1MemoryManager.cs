using System.Collections;
using TMPro;
using UnityEngine;

public class Page1MemoryManager : MonoBehaviour
{
    private const int TotalMemoryCount = 4;

    [Header("Next Story Sequence")]
    [SerializeField]
    private SeedButterflyManager seedButterflyManager;

    [Header("Run Memory")]
    [SerializeField] private GameObject runMemoryRoot;
    [SerializeField] private Animator runDogAnimator;

    [Header("Sleep Memory")]
    [SerializeField] private GameObject sleepMemoryRoot;
    [SerializeField] private Animator sleepDogAnimator;

    [Header("Door Memory")]
    [SerializeField] private GameObject doorMemoryRoot;
    [SerializeField] private Animator doorDogAnimator;

    [Header("Food Memory")]
    [SerializeField] private GameObject foodMemoryRoot;
    [SerializeField] private Animator foodDogAnimator;

    [Header("Paw Markers")]
    [SerializeField] private GameObject ballPawMarker;
    [SerializeField] private GameObject bedPawMarker;
    [SerializeField] private GameObject collarPawMarker;
    [SerializeField] private GameObject foodPawMarker;

    [Header("Sparkle Effects")]
    [SerializeField] private GameObject ballSparkleEffect;
    [SerializeField] private GameObject bedSparkleEffect;
    [SerializeField] private GameObject collarSparkleEffect;
    [SerializeField] private GameObject foodSparkleEffect;

    [Header("First Tap Tutorial")]
    [SerializeField] private GameObject firstTapHandHint;

    [Tooltip("Require the player to tap the ball first.")]
    [SerializeField] private bool requireBallFirst = true;

    [Header("Text")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Transition Timing")]
    [SerializeField] private float appearDuration = 0.35f;
    [SerializeField] private float disappearDuration = 0.2f;

    [Tooltip("The final memory will disappear after this duration.")]
    [SerializeField] private float finalMemoryDuration = 10f;

    [Header("Run Around Ball")]
    [Tooltip("72 degrees per second means one complete circle in 5 seconds.")]
    [SerializeField] private float runRotationSpeed = 72f;

    [Header("Memory Audio")]
    [Tooltip(
        "Used for short sounds such as entering a memory and barking."
    )]
    [SerializeField] private AudioSource memoryOneShotSource;

    [Tooltip(
        "Used for sounds that continue until another memory is selected."
    )]
    [SerializeField] private AudioSource memoryLoopSource;

    [Tooltip(
        "Played whenever the player enters a memory."
    )]
    [SerializeField] private AudioClip memoryEnterClip;

    [Tooltip(
        "Looped while the running memory is active."
    )]
    [SerializeField] private AudioClip runLoopClip;

    [Tooltip(
        "Looped while the sleeping memory is active."
    )]
    [SerializeField] private AudioClip sleepLoopClip;

    [Tooltip(
        "Played once when the door memory appears."
    )]
    [SerializeField] private AudioClip doorBarkClip;

    [Tooltip(
        "Looped while the eating memory is active."
    )]
    [SerializeField] private AudioClip foodLoopClip;

    private bool ballCompleted;
    private bool bedCompleted;
    private bool collarCompleted;
    private bool foodCompleted;

    private int completedMemoryCount;

    private bool isSwitching;
    private bool rotateRunMemory;
    private bool allMemoriesFinished;

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

        SetObjectActive(ballPawMarker, true);
        SetObjectActive(bedPawMarker, true);
        SetObjectActive(collarPawMarker, true);
        SetObjectActive(foodPawMarker, true);

        SetObjectActive(ballSparkleEffect, true);
        SetObjectActive(bedSparkleEffect, true);
        SetObjectActive(collarSparkleEffect, true);
        SetObjectActive(foodSparkleEffect, true);

        SetObjectActive(firstTapHandHint, true);

        InitialiseAudioSources();
    }

    private void Start()
    {
        SetSubtitle(
            "Lumi's room is filled with memories."
        );

        SetInstruction(
            "Tap the object shown by the hand."
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
            memoryOneShotSource.playOnAwake = false;
            memoryOneShotSource.loop = false;
            memoryOneShotSource.spatialBlend = 0f;
        }

        if (memoryLoopSource != null)
        {
            memoryLoopSource.playOnAwake = false;
            memoryLoopSource.loop = true;
            memoryLoopSource.spatialBlend = 0f;
        }
    }

    public bool TryPlayMemory(
        MemoryType memoryType)
    {
        if (isSwitching ||
            allMemoriesFinished)
        {
            return false;
        }

        if (completedMemoryCount == 0 &&
            requireBallFirst &&
            memoryType != MemoryType.Ball)
        {
            SetInstruction(
                "Tap the ball first to begin."
            );

            return false;
        }

        if (IsMemoryCompleted(memoryType))
        {
            return false;
        }

        if (!HasRequiredReferences(memoryType))
        {
            return false;
        }

        MarkCompleted(memoryType);

        StartCoroutine(
            SwitchMemory(memoryType)
        );

        return true;
    }

    private void InitialiseMemoryRoot(
        GameObject memoryRoot,
        out Vector3 originalScale)
    {
        originalScale = Vector3.one;

        if (memoryRoot == null)
        {
            return;
        }

        originalScale =
            memoryRoot.transform.localScale;

        memoryRoot.SetActive(false);
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
                memoryRoot = runMemoryRoot;
                memoryAnimator = runDogAnimator;
                break;

            case MemoryType.Bed:
                memoryRoot = sleepMemoryRoot;
                memoryAnimator = sleepDogAnimator;
                break;

            case MemoryType.Collar:
                memoryRoot = doorMemoryRoot;
                memoryAnimator = doorDogAnimator;
                break;

            case MemoryType.Food:
                memoryRoot = foodMemoryRoot;
                memoryAnimator = foodDogAnimator;
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

        if (completedMemoryCount == 1)
        {
            SetObjectActive(
                firstTapHandHint,
                false
            );

            SetInstruction(
                "Tap another object marked with a paw print."
            );
        }
        else if (
            completedMemoryCount <
            TotalMemoryCount)
        {
            SetInstruction(
                "Tap another object marked with a paw print."
            );
        }
        else
        {
            SetInstruction(
                "Watch the final memory."
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

        /*
         * Stop the continuing sound from the
         * previously active memory.
         */
        StopMemoryLoop();

        /*
         * Hide the memory that is currently active.
         */
        if (currentMemoryRoot != null &&
            currentMemoryRoot.activeSelf)
        {
            yield return ScaleObject(
                currentMemoryRoot.transform,
                currentMemoryRoot.transform.localScale,
                Vector3.zero,
                disappearDuration
            );

            currentMemoryRoot.SetActive(false);

            currentMemoryRoot.transform.localScale =
                currentOriginalScale;
        }

        GameObject selectedRoot = null;
        Animator selectedAnimator = null;

        GameObject selectedPawMarker = null;
        GameObject selectedSparkleEffect = null;

        Vector3 selectedOriginalScale =
            Vector3.one;

        string stateName = "";
        string subtitle = "";

        AudioClip selectedLoopClip = null;
        AudioClip selectedOneShotClip = null;

        switch (memoryType)
        {
            case MemoryType.Ball:
                selectedRoot = runMemoryRoot;
                selectedAnimator = runDogAnimator;

                selectedPawMarker = ballPawMarker;
                selectedSparkleEffect =
                    ballSparkleEffect;

                selectedOriginalScale =
                    runOriginalScale;

                stateName =
                    "Base Layer.Run";

                subtitle =
                    "You remember how Lumi chased " +
                    "the ball around the room.";

                selectedLoopClip =
                    runLoopClip;

                runMemoryRoot.transform.localRotation =
                    runOriginalRotation;

                break;

            case MemoryType.Bed:
                selectedRoot = sleepMemoryRoot;
                selectedAnimator = sleepDogAnimator;

                selectedPawMarker = bedPawMarker;
                selectedSparkleEffect =
                    bedSparkleEffect;

                selectedOriginalScale =
                    sleepOriginalScale;

                stateName =
                    "Base Layer.Rest";

                subtitle =
                    "You remember how Lumi rested " +
                    "on the soft bed.";

                selectedLoopClip =
                    sleepLoopClip;

                break;

            case MemoryType.Collar:
                selectedRoot = doorMemoryRoot;
                selectedAnimator = doorDogAnimator;

                selectedPawMarker =
                    collarPawMarker;

                selectedSparkleEffect =
                    collarSparkleEffect;

                selectedOriginalScale =
                    doorOriginalScale;

                stateName =
                    "Base Layer.Jump";

                subtitle =
                    "You remember how Lumi waited " +
                    "excitedly by the door.";

                selectedLoopClip =
                    doorBarkClip;

                break;

            case MemoryType.Food:
                selectedRoot = foodMemoryRoot;
                selectedAnimator = foodDogAnimator;

                selectedPawMarker =
                    foodPawMarker;

                selectedSparkleEffect =
                    foodSparkleEffect;

                selectedOriginalScale =
                    foodOriginalScale;

                stateName =
                    "Base Layer.Eat";

                subtitle =
                    "You remember how Lumi happily " +
                    "ate from the food bowl.";

                selectedLoopClip =
                    foodLoopClip;

                break;
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

        selectedRoot.SetActive(true);

        PrepareAndPlayAnimator(
            selectedAnimator,
            stateName
        );

        /*
         * Play the same short transition sound
         * whenever a memory begins.
         */
        PlayMemoryOneShot(
            memoryEnterClip
        );

        /*
         * Start the selected continuing action sound.
         * A null clip means this memory has no loop.
         */
        StartMemoryLoop(
            selectedLoopClip
        );

        currentMemoryRoot =
            selectedRoot;

        currentOriginalScale =
            selectedOriginalScale;

        rotateRunMemory =
            memoryType == MemoryType.Ball;

        SetSubtitle(subtitle);

        yield return ScaleObject(
            selectedRoot.transform,
            Vector3.zero,
            selectedOriginalScale,
            appearDuration
        );

        selectedRoot.transform.localScale =
            selectedOriginalScale;

        /*
         * Play an optional one-time action sound,
         * such as Lumi barking beside the door.
         */
        PlayMemoryOneShot(
            selectedOneShotClip
        );

        /*
         * The first three selected memories remain
         * active until another object is selected.
         */
        if (completedMemoryCount <
            TotalMemoryCount)
        {
            isSwitching = false;
            yield break;
        }

        /*
         * The final selected memory remains active
         * for the specified duration.
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
            selectedRoot.transform.localScale,
            Vector3.zero,
            disappearDuration
        );

        selectedRoot.SetActive(false);

        selectedRoot.transform.localScale =
            selectedOriginalScale;

        currentMemoryRoot = null;

        isSwitching = false;
        allMemoriesFinished = true;

        OnAllMemoriesCompleted();
    }

    private void PrepareAndPlayAnimator(
        Animator animator,
        string fullStateName)
    {
        if (animator == null)
        {
            return;
        }

        animator.enabled = true;
        animator.speed = 1f;
        animator.applyRootMotion = false;

        animator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        animator.Rebind();
        animator.Update(0f);

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

        animator.Update(0f);

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

    private void OnAllMemoriesCompleted()
    {
        SetInstruction("");

        StopMemoryLoop();

        Debug.Log(
            "All four Lumi memories " +
            "have been completed."
        );

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
            currentMemoryRoot.SetActive(false);

            currentMemoryRoot.transform.localScale =
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