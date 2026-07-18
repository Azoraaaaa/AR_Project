using System.Collections;
using UnityEngine;

public class MemoryGardenGateManager : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip(
        "Controls the Page 2 GameHint, StoryBG, " +
        "typewriter effect and dialogue."
    )]
    [SerializeField]
    private Page2DialogueController dialogueController;

    [Header("Gate Interaction")]
    [Tooltip(
        "The root object that shakes when the locked gate is tapped."
    )]
    [SerializeField]
    private Transform gateShakeRoot;

    [Tooltip(
        "The collider used to click the garden gate."
    )]
    [SerializeField]
    private Collider gateClickCollider;

    [Header("Locked Gate Shake")]
    [SerializeField]
    private float lockedShakeDuration = 0.4f;

    [SerializeField]
    private float lockedShakeAngle = 3f;

    [SerializeField]
    private int lockedShakeCycles = 3;

    [Tooltip(
        "Usually use (0, 1, 0) to shake around the Y axis."
    )]
    [SerializeField]
    private Vector3 lockedShakeAxis =
        new Vector3(0f, 1f, 0f);

    [Header("Paw Trail")]
    [Tooltip(
        "Assign Paw1 to Paw6 in the correct order."
    )]
    [SerializeField]
    private GameObject[] pawMarkers;

    [Header("Key")]
    [Tooltip(
        "The parent object containing the key model " +
        "and its click area."
    )]
    [SerializeField]
    private GameObject keyRoot;

    [Tooltip(
        "The collider used to click the key."
    )]
    [SerializeField]
    private Collider keyClickCollider;

    [SerializeField]
    private GameObject keySparkleEffect;

    [SerializeField]
    private GameObject keyTapHint;

    [Header("Key Reveal")]
    [Tooltip(
        "How long to wait after the final paw print " +
        "before the key appears."
    )]
    [SerializeField]
    private float keyRevealDelay = 3f;

    [Tooltip(
        "How long the key takes to grow from zero scale."
    )]
    [SerializeField]
    private float keyRevealDuration = 0.5f;

    [Header("Key Floating")]
    [Tooltip(
        "How far the key moves up and down."
    )]
    [SerializeField]
    private float keyFloatHeight = 0.06f;

    [Tooltip(
        "How quickly the key moves up and down."
    )]
    [SerializeField]
    private float keyFloatSpeed = 2.5f;

    [Header("Key Flight")]
    [Tooltip(
        "The middle control point used to create " +
        "the curved key flight path."
    )]
    [SerializeField]
    private Transform keyControlPoint;

    [Tooltip(
        "The final point at the centre of the gate lock."
    )]
    [SerializeField]
    private Transform lockPoint;

    [SerializeField]
    private float keyFlightDuration = 1.2f;

    [Header("Door Opening")]
    [Tooltip(
        "The pivot placed at the left door hinge."
    )]
    [SerializeField]
    private Transform leftDoorPivot;

    [Tooltip(
        "Optional. Leave empty if only one door should open."
    )]
    [SerializeField]
    private Transform rightDoorPivot;

    [Tooltip(
        "Rotation added to the left door's closed rotation."
    )]
    [SerializeField]
    private Vector3 leftDoorOpenEuler =
        new Vector3(0f, -85f, 0f);

    [Tooltip(
        "Rotation added to the right door's closed rotation."
    )]
    [SerializeField]
    private Vector3 rightDoorOpenEuler =
        new Vector3(0f, 85f, 0f);

    [SerializeField]
    private float doorOpenDuration = 1.4f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [Tooltip(
        "Played whenever the correct paw print is tapped."
    )]
    [SerializeField]
    private AudioClip pawSelectClip;

    [Tooltip(
        "Played when the hidden key appears."
    )]
    [SerializeField]
    private AudioClip keyRevealClip;

    [Tooltip(
        "Played when the player taps the key."
    )]
    [SerializeField]
    private AudioClip keySelectClip;

    [Tooltip(
        "Played whenever the player taps the locked gate."
    )]
    [SerializeField]
    private AudioClip lockedClip;

    [Tooltip(
        "Played when the key reaches the gate lock."
    )]
    [SerializeField]
    private AudioClip unlockClip;

    [Tooltip(
        "Played when the gate starts opening."
    )]
    [SerializeField]
    private AudioClip gateOpenClip;

    private int currentPawIndex;

    private bool introCompleted;
    private bool gateChecked;
    private bool gateOpened;
    private bool pawTrailReady;

    private bool keyRevealed;
    private bool keySelected;
    private bool keyFloating;

    private bool sequenceBusy;
    private bool gateShaking;

    private Vector3 keyOriginalScale;
    private Vector3 keyFloatBaseLocalPosition;

    private Quaternion gateShakeOriginalRotation;
    private Quaternion leftDoorClosedRotation;
    private Quaternion rightDoorClosedRotation;

    private void Awake()
    {
        InitialisePawTrail();
        InitialiseKey();
        InitialiseGate();
        InitialiseAudio();
    }

    private IEnumerator Start()
    {
        introCompleted = false;
        pawTrailReady = false;

        /*
         * Do not allow the player to click the gate
         * while the opening butterfly dialogue is playing.
         */
        if (gateClickCollider != null)
        {
            gateClickCollider.enabled = false;
        }

        if (dialogueController != null)
        {
            yield return
                dialogueController
                    .PlayIntroButterflySequence();
        }
        else
        {
            Debug.LogWarning(
                "Page2DialogueController has not been assigned. " +
                "The Page 2 opening dialogue will be skipped."
            );
        }

        /*
         * The final opening line has now finished:
         * "First, we must get inside. Try opening the gate."
         */
        introCompleted = true;

        if (gateClickCollider != null)
        {
            gateClickCollider.enabled = true;
        }

        Debug.Log(
            "Page 2 opening dialogue has finished. " +
            "The garden gate can now be selected."
        );
    }

    private void Update()
    {
        if (keyFloating &&
            keyRoot != null &&
            keyRoot.activeSelf)
        {
            float verticalOffset =
                Mathf.Sin(
                    Time.time *
                    keyFloatSpeed
                ) *
                keyFloatHeight;

            keyRoot.transform.localPosition =
                keyFloatBaseLocalPosition +
                Vector3.up *
                verticalOffset;
        }
    }

    private void OnDisable()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void InitialisePawTrail()
    {
        currentPawIndex = 0;
        pawTrailReady = false;

        if (pawMarkers == null)
        {
            return;
        }

        /*
         * All paw prints start hidden.
         * Paw1 only appears after the player checks
         * the gate and finishes reading the hint.
         */
        for (int i = 0;
             i < pawMarkers.Length;
             i++)
        {
            SetObjectActive(
                pawMarkers[i],
                false
            );
        }
    }

    private void InitialiseKey()
    {
        if (keyRoot != null)
        {
            keyOriginalScale =
                keyRoot.transform.localScale;

            keyFloatBaseLocalPosition =
                keyRoot.transform.localPosition;

            keyRoot.SetActive(false);
        }

        if (keyClickCollider != null)
        {
            keyClickCollider.enabled = false;
        }

        SetObjectActive(
            keySparkleEffect,
            false
        );

        SetObjectActive(
            keyTapHint,
            false
        );
    }

    private void InitialiseGate()
    {
        if (gateShakeRoot != null)
        {
            gateShakeOriginalRotation =
                gateShakeRoot.localRotation;
        }

        if (leftDoorPivot != null)
        {
            leftDoorClosedRotation =
                leftDoorPivot.localRotation;
        }

        if (rightDoorPivot != null)
        {
            rightDoorClosedRotation =
                rightDoorPivot.localRotation;
        }
    }

    private void InitialiseAudio()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public bool TryCheckGate()
    {
        if (!introCompleted ||
            gateOpened ||
            sequenceBusy)
        {
            return false;
        }

        /*
         * The locked sound and shake can still happen
         * if the player taps the locked gate again.
         */
        PlayAudio(
            lockedClip
        );

        if (!gateShaking &&
            gateShakeRoot != null)
        {
            StartCoroutine(
                ShakeLockedGateRoutine()
            );
        }

        /*
         * The locked-gate dialogue and Paw1 reveal
         * only happen on the first successful gate tap.
         */
        if (gateChecked)
        {
            return true;
        }

        gateChecked = true;

        StartCoroutine(
            LockedGateDialogueRoutine()
        );

        return true;
    }

    private IEnumerator LockedGateDialogueRoutine()
    {
        sequenceBusy = true;

        /*
         * Page2DialogueController automatically hides
         * StoryBG and displays GameHint.
         */
        if (dialogueController != null)
        {
            yield return
                dialogueController
                    .PlayLockedGateHintSequence();
        }
        else
        {
            yield return new WaitForSeconds(
                1f
            );
        }

        /*
         * Paw1 appears only after the locked-gate
         * explanation has completely finished.
         */
        if (pawMarkers != null &&
            pawMarkers.Length > 0 &&
            pawMarkers[0] != null &&
            currentPawIndex == 0)
        {
            SetObjectActive(
                pawMarkers[0],
                true
            );
        }

        pawTrailReady = true;

        /*
         * Show the final interaction instruction.
         * Paw1 is already visible at this point.
         */
        if (dialogueController != null)
        {
            dialogueController
                .ShowPawTrailHint();
        }

        sequenceBusy = false;

        Debug.Log(
            "The paw trail is ready. Paw1 is now visible."
        );
    }

    public bool TrySelectPaw(
        int pawIndex)
    {
        if (gateOpened ||
            sequenceBusy ||
            !pawTrailReady)
        {
            return false;
        }

        if (pawMarkers == null ||
            pawMarkers.Length == 0)
        {
            Debug.LogError(
                "No paw markers have been assigned."
            );

            return false;
        }

        if (pawIndex < 0 ||
            pawIndex >= pawMarkers.Length)
        {
            return false;
        }

        /*
         * Only the currently visible paw print
         * can be accepted.
         */
        if (pawIndex != currentPawIndex)
        {
            return false;
        }

        if (pawMarkers[pawIndex] == null ||
            !pawMarkers[pawIndex].activeSelf)
        {
            return false;
        }

        PlayAudio(
            pawSelectClip
        );

        SetObjectActive(
            pawMarkers[pawIndex],
            false
        );

        /*
         * Show the next paw print.
         */
        if (currentPawIndex <
            pawMarkers.Length - 1)
        {
            currentPawIndex++;

            SetObjectActive(
                pawMarkers[currentPawIndex],
                true
            );
        }
        else
        {
            /*
             * The final paw was selected.
             * Stop paw interaction and begin
             * the key reveal sequence.
             */
            currentPawIndex++;
            pawTrailReady = false;

            StartCoroutine(
                RevealKeyRoutine()
            );
        }

        return true;
    }

    private IEnumerator RevealKeyRoutine()
    {
        sequenceBusy = true;

        /*
         * Create a short pause after the final
         * paw print disappears.
         */
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                keyRevealDelay
            )
        );

        if (keyRoot == null)
        {
            Debug.LogError(
                "Key Root has not been assigned."
            );

            sequenceBusy = false;
            yield break;
        }

        PlayAudio(
            keyRevealClip
        );

        keyRoot.transform.localPosition =
            keyFloatBaseLocalPosition;

        keyRoot.transform.localScale =
            Vector3.zero;

        keyRoot.SetActive(true);

        SetObjectActive(
            keySparkleEffect,
            true
        );

        /*
         * The key is not clickable while appearing.
         */
        if (keyClickCollider != null)
        {
            keyClickCollider.enabled = false;
        }

        SetObjectActive(
            keyTapHint,
            false
        );

        yield return ScaleObject(
            keyRoot.transform,
            Vector3.zero,
            keyOriginalScale,
            keyRevealDuration
        );

        keyRoot.transform.localScale =
            keyOriginalScale;

        keyFloatBaseLocalPosition =
            keyRoot.transform.localPosition;

        keyFloating = true;

        /*
         * Explain the key before allowing the player
         * to interact with it.
         */
        if (dialogueController != null)
        {
            yield return
                dialogueController
                    .PlayKeyRevealHintSequence();
        }

        keyRevealed = true;

        if (keyClickCollider != null)
        {
            keyClickCollider.enabled = true;
        }

        SetObjectActive(
            keyTapHint,
            true
        );

        sequenceBusy = false;

        Debug.Log(
            "The Memory Key is now clickable."
        );
    }

    public bool TrySelectKey()
    {
        if (!keyRevealed ||
            keySelected ||
            gateOpened ||
            sequenceBusy ||
            keyRoot == null)
        {
            return false;
        }

        keySelected = true;
        keyFloating = false;
        sequenceBusy = true;

        PlayAudio(
            keySelectClip
        );

        if (keyClickCollider != null)
        {
            keyClickCollider.enabled = false;
        }

        SetObjectActive(
            keyTapHint,
            false
        );

        SetObjectActive(
            keySparkleEffect,
            false
        );

        StartCoroutine(
            FlyKeyAndOpenGateRoutine()
        );

        return true;
    }

    private IEnumerator FlyKeyAndOpenGateRoutine()
    {
        if (keyControlPoint == null ||
            lockPoint == null)
        {
            Debug.LogError(
                "Key Control Point or Lock Point " +
                "has not been assigned."
            );

            sequenceBusy = false;
            yield break;
        }

        /*
         * Detach the key while preserving its
         * current world position.
         */
        keyRoot.transform.SetParent(
            null,
            true
        );

        Vector3 startPosition =
            keyRoot.transform.position;

        float elapsed = 0f;

        float safeDuration =
            Mathf.Max(
                0.01f,
                keyFlightDuration
            );

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    safeDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Vector3 currentControlPosition =
                keyControlPoint.position;

            Vector3 currentEndPosition =
                lockPoint.position;

            keyRoot.transform.position =
                CalculateQuadraticBezier(
                    startPosition,
                    currentControlPosition,
                    currentEndPosition,
                    smoothProgress
                );

            yield return null;
        }

        keyRoot.transform.position =
            lockPoint.position;

        /*
         * The key has reached the lock.
         */
        PlayAudio(
            unlockClip
        );

        yield return new WaitForSeconds(
            0.25f
        );

        keyRoot.SetActive(false);

        PlayAudio(
            gateOpenClip
        );

        yield return StartCoroutine(
            OpenGateRoutine()
        );

        gateOpened = true;

        if (gateClickCollider != null)
        {
            gateClickCollider.enabled = false;
        }

        /*
         * GameHint is hidden and StoryBG is displayed
         * automatically for the final butterfly dialogue.
         */
        if (dialogueController != null)
        {
            yield return
                dialogueController
                    .PlayGateOpenedButterflySequence();
        }
        else
        {
            Debug.LogWarning(
                "Page2DialogueController has not been assigned. " +
                "The final Page 2 dialogue will be skipped."
            );
        }

        if (SimpleCloudRecoEventHandler.Instance != null)
        {
            SimpleCloudRecoEventHandler.Instance
                .ShowNextPageCanvas();
        }

        sequenceBusy = false;

        Debug.Log(
            "The Memory Garden gate has opened. " +
            "Page 2 has been completed."
        );
    }

    private IEnumerator ShakeLockedGateRoutine()
    {
        gateShaking = true;

        Quaternion startRotation =
            gateShakeOriginalRotation;

        float elapsed = 0f;

        float safeDuration =
            Mathf.Max(
                0.01f,
                lockedShakeDuration
            );

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    safeDuration
                );

            float wave =
                Mathf.Sin(
                    progress *
                    Mathf.PI *
                    2f *
                    lockedShakeCycles
                );

            float fade =
                1f -
                progress;

            float angle =
                wave *
                lockedShakeAngle *
                fade;

            gateShakeRoot.localRotation =
                startRotation *
                Quaternion.Euler(
                    lockedShakeAxis *
                    angle
                );

            yield return null;
        }

        gateShakeRoot.localRotation =
            startRotation;

        gateShaking = false;
    }

    private IEnumerator OpenGateRoutine()
    {
        if (leftDoorPivot == null &&
            rightDoorPivot == null)
        {
            Debug.LogError(
                "No door pivot has been assigned."
            );

            yield break;
        }

        Quaternion leftTargetRotation =
            leftDoorClosedRotation *
            Quaternion.Euler(
                leftDoorOpenEuler
            );

        Quaternion rightTargetRotation =
            rightDoorClosedRotation *
            Quaternion.Euler(
                rightDoorOpenEuler
            );

        float elapsed = 0f;

        float safeDuration =
            Mathf.Max(
                0.01f,
                doorOpenDuration
            );

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    safeDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (leftDoorPivot != null)
            {
                leftDoorPivot.localRotation =
                    Quaternion.Slerp(
                        leftDoorClosedRotation,
                        leftTargetRotation,
                        smoothProgress
                    );
            }

            if (rightDoorPivot != null)
            {
                rightDoorPivot.localRotation =
                    Quaternion.Slerp(
                        rightDoorClosedRotation,
                        rightTargetRotation,
                        smoothProgress
                    );
            }

            yield return null;
        }

        if (leftDoorPivot != null)
        {
            leftDoorPivot.localRotation =
                leftTargetRotation;
        }

        if (rightDoorPivot != null)
        {
            rightDoorPivot.localRotation =
                rightTargetRotation;
        }
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
            inverseT *
            inverseT *
            start +
            2f *
            inverseT *
            t *
            control +
            t *
            t *
            end;
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
                    elapsed /
                    duration
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

    private void PlayAudio(
        AudioClip clip)
    {
        if (audioSource == null ||
            clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            clip
        );
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