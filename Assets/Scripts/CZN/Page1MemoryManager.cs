using System.Collections;
using TMPro;
using UnityEngine;

public class Page1MemoryManager : MonoBehaviour
{
    [Header("Run Memory")]
    [SerializeField] private GameObject runMemoryRoot;
    [SerializeField] private Animator runDogAnimator;

    [Header("Sleep Memory")]
    [SerializeField] private GameObject sleepMemoryRoot;
    [SerializeField] private Animator sleepDogAnimator;

    [Header("Door Memory")]
    [SerializeField] private GameObject doorMemoryRoot;
    [SerializeField] private Animator doorDogAnimator;

    [Header("Glow Effects")]
    [SerializeField] private GameObject ballGlow;
    [SerializeField] private GameObject bedGlow;
    [SerializeField] private GameObject collarGlow;

    [Header("Subtitle - Optional")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("Transition Timing")]
    [SerializeField] private float appearDuration = 0.35f;
    [SerializeField] private float disappearDuration = 0.2f;

    [Tooltip("final memory will dissapear after")]
    [SerializeField] private float finalMemoryDuration = 10f;

    [Header("Run Around Ball")]
    [Tooltip("72 degrees per second means one complete circle in 5 seconds.")]
    [SerializeField] private float runRotationSpeed = 72f;


    private bool ballCompleted;
    private bool bedCompleted;
    private bool collarCompleted;


    private int completedMemoryCount;


    private bool isSwitching;


    private bool rotateRunMemory;


    private bool allMemoriesFinished;


    private GameObject currentMemoryRoot;
    private Vector3 currentOriginalScale;


    private Vector3 runOriginalScale;
    private Vector3 sleepOriginalScale;
    private Vector3 doorOriginalScale;


    private Quaternion runOriginalRotation;

    private void Awake()
    {
        if (runMemoryRoot != null)
        {
            runOriginalScale =
                runMemoryRoot.transform.localScale;

            runOriginalRotation =
                runMemoryRoot.transform.localRotation;

            runMemoryRoot.SetActive(false);
        }

        if (sleepMemoryRoot != null)
        {
            sleepOriginalScale =
                sleepMemoryRoot.transform.localScale;

            sleepMemoryRoot.SetActive(false);
        }

        if (doorMemoryRoot != null)
        {
            doorOriginalScale =
                doorMemoryRoot.transform.localScale;

            doorMemoryRoot.SetActive(false);
        }


        if (ballGlow != null)
            ballGlow.SetActive(true);

        if (bedGlow != null)
            bedGlow.SetActive(true);

        if (collarGlow != null)
            collarGlow.SetActive(true);
    }

    private void Start()
    {
        SetSubtitle(
            "Tap the glowing objects to remember Lumi."
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


    public bool TryPlayMemory(MemoryType memoryType)
    {

        if (isSwitching)
            return false;


        if (allMemoriesFinished)
            return false;


        if (memoryType == MemoryType.Ball &&
            ballCompleted)
        {
            return false;
        }

        if (memoryType == MemoryType.Bed &&
            bedCompleted)
        {
            return false;
        }

        if (memoryType == MemoryType.Collar &&
            collarCompleted)
        {
            return false;
        }


        if (!HasRequiredReferences(memoryType))
            return false;

        MarkCompleted(memoryType);

        StartCoroutine(
            SwitchMemory(memoryType)
        );

        return true;
    }

    private bool HasRequiredReferences(
        MemoryType memoryType)
    {
        switch (memoryType)
        {
            case MemoryType.Ball:
                if (runMemoryRoot == null ||
                    runDogAnimator == null)
                {
                    Debug.LogError(
                        "Ball memory is missing " +
                        "RunMemoryRoot or RunDogAnimator."
                    );

                    return false;
                }

                break;

            case MemoryType.Bed:
                if (sleepMemoryRoot == null ||
                    sleepDogAnimator == null)
                {
                    Debug.LogError(
                        "Bed memory is missing " +
                        "SleepMemoryRoot or SleepDogAnimator."
                    );

                    return false;
                }

                break;

            case MemoryType.Collar:
                if (doorMemoryRoot == null ||
                    doorDogAnimator == null)
                {
                    Debug.LogError(
                        "Collar memory is missing " +
                        "DoorMemoryRoot or DoorDogAnimator."
                    );

                    return false;
                }

                break;
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
        }

        completedMemoryCount++;

        Debug.Log(
            $"Completed memory count: " +
            $"{completedMemoryCount}/3"
        );
    }


    private IEnumerator SwitchMemory(
        MemoryType memoryType)
    {
        isSwitching = true;


        rotateRunMemory = false;


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
        GameObject selectedGlow = null;

        Vector3 selectedOriginalScale =
            Vector3.one;

        string stateName = "";
        string subtitle = "";

        switch (memoryType)
        {
            case MemoryType.Ball:
                selectedRoot = runMemoryRoot;
                selectedAnimator = runDogAnimator;
                selectedGlow = ballGlow;

                selectedOriginalScale =
                    runOriginalScale;

                stateName =
                    "Base Layer.Run";

                subtitle =
                    "You remember how Lumi chased " +
                    "the ball around the room.";


                runMemoryRoot.transform.localRotation =
                    runOriginalRotation;

                break;

            case MemoryType.Bed:
                selectedRoot = sleepMemoryRoot;
                selectedAnimator = sleepDogAnimator;
                selectedGlow = bedGlow;

                selectedOriginalScale =
                    sleepOriginalScale;

                stateName =
                    "Base Layer.Rest";

                subtitle =
                    "You remember how Lumi rested " +
                    "on the soft bed.";

                break;

            case MemoryType.Collar:
                selectedRoot = doorMemoryRoot;
                selectedAnimator = doorDogAnimator;
                selectedGlow = collarGlow;

                selectedOriginalScale =
                    doorOriginalScale;

                stateName =
                    "Base Layer.Jump";

                subtitle =
                    "You remember how Lumi waited " +
                    "excitedly by the door.";

                break;
        }


        if (selectedGlow != null)
        {
            selectedGlow.SetActive(false);
        }


        selectedRoot.transform.localScale =
            Vector3.zero;

        selectedRoot.SetActive(true);


        PrepareAndPlayAnimator(
            selectedAnimator,
            stateName
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


        if (completedMemoryCount < 3)
        {
            isSwitching = false;
            yield break;
        }


        yield return new WaitForSeconds(
            Mathf.Max(0f, finalMemoryDuration)
        );


        rotateRunMemory = false;


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
            return;

        animator.enabled = true;
        animator.speed = 1f;
        animator.applyRootMotion = false;

        animator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        animator.Rebind();
        animator.Update(0f);

        int stateHash =
            Animator.StringToHash(fullStateName);

        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError(
                $"{animator.gameObject.name}: " +
                $"Animator state '{fullStateName}' " +
                "was not found. Check the Animator " +
                "Controller and state name."
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
            yield break;

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
        SetSubtitle(
            "The memories faded, but something " +
            "near Lumi's collar began to glow."
        );

        Debug.Log(
            "All three Lumi memories " +
            "have been completed."
        );

        
    }

    public void HideCurrentMemory()
    {
        rotateRunMemory = false;

        if (currentMemoryRoot != null)
        {
            currentMemoryRoot.SetActive(false);

            currentMemoryRoot.transform.localScale =
                currentOriginalScale;
        }

        currentMemoryRoot = null;
    }

    private void SetSubtitle(string message)
    {
        if (subtitleText != null)
        {
            subtitleText.text =
                message;
        }
    }
}