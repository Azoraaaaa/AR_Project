using System.Collections;
using TMPro;
using UnityEngine;

public class MemoryGardenGateManager : MonoBehaviour
{
    [Header("Gate Interaction")]
    [SerializeField] private Transform gateShakeRoot;
    [SerializeField] private Collider gateClickCollider;

    [Tooltip("The paw trail cannot be used until the gate has been checked.")]
    [SerializeField] private bool requireGateCheckBeforePaws = true;

    [Tooltip("Show the first paw print as soon as the scene begins.")]
    [SerializeField] private bool showFirstPawAtStart = true;

    [Header("Locked Gate Shake")]
    [SerializeField] private float lockedShakeDuration = 0.4f;
    [SerializeField] private float lockedShakeAngle = 3f;
    [SerializeField] private int lockedShakeCycles = 3;

    [Tooltip("Use (0, 1, 0) for a small Y-axis shake.")]
    [SerializeField]
    private Vector3 lockedShakeAxis =
        new Vector3(0f, 1f, 0f);

    [Header("Paw Trail")]
    [SerializeField] private GameObject[] pawMarkers;

    [Header("Key")]
    [SerializeField] private GameObject keyRoot;
    [SerializeField] private Collider keyClickCollider;
    [SerializeField] private GameObject keySparkleEffect;
    [SerializeField] private GameObject keyTapHint;

    [Header("Key Reveal")]
    [Tooltip("How long to wait after the final paw print is selected.")]
    [SerializeField] private float keyRevealDelay = 3f;

    [SerializeField] private float keyRevealDuration = 0.5f;

    [Header("Key Floating")]
    [SerializeField] private float keyFloatHeight = 0.06f;
    [SerializeField] private float keyFloatSpeed = 2.5f;

    [Header("Key Flight")]
    [SerializeField] private Transform keyControlPoint;
    [SerializeField] private Transform lockPoint;
    [SerializeField] private float keyFlightDuration = 1.2f;

    [Header("Door Opening")]
    [Tooltip("Assign the left door hinge pivot.")]
    [SerializeField] private Transform leftDoorPivot;

    [Tooltip("Optional. Leave empty when only opening one door.")]
    [SerializeField] private Transform rightDoorPivot;

    [Tooltip("Adjust this after testing the left door pivot.")]
    [SerializeField]
    private Vector3 leftDoorOpenEuler =
        new Vector3(0f, -85f, 0f);

    [Tooltip("Adjust this after testing the right door pivot.")]
    [SerializeField]
    private Vector3 rightDoorOpenEuler =
        new Vector3(0f, 85f, 0f);

    [SerializeField] private float doorOpenDuration = 1.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Played whenever the player selects the correct paw print.")]
    [SerializeField] private AudioClip pawSelectClip;

    [Tooltip("Played when the hidden key appears.")]
    [SerializeField] private AudioClip keyRevealClip;

    [Tooltip("Played immediately when the player selects the key.")]
    [SerializeField] private AudioClip keySelectClip;

    [Tooltip("Played when the player taps the locked gate.")]
    [SerializeField] private AudioClip lockedClip;

    [Tooltip("Played when the key reaches the gate lock.")]
    [SerializeField] private AudioClip unlockClip;

    [Tooltip("Played when the gate begins to open.")]
    [SerializeField] private AudioClip gateOpenClip;

    [Header("Text")]
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text instructionText;

    private int currentPawIndex;

    private bool gateChecked;
    private bool gateOpened;
    private bool keyRevealed;
    private bool keySelected;

    private bool sequenceBusy;
    private bool keyFloating;
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

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    private void Start()
    {
        SetSubtitle(
            "The entrance to the Memory Garden stood before you."
        );

        SetInstruction(
            "Tap the garden gate."
        );
    }

    private void Update()
    {
        if (keyFloating &&
            keyRoot != null &&
            keyRoot.activeSelf)
        {
            float verticalOffset =
                Mathf.Sin(Time.time * keyFloatSpeed) *
                keyFloatHeight;

            keyRoot.transform.localPosition =
                keyFloatBaseLocalPosition +
                Vector3.up * verticalOffset;
        }
    }

    private void InitialisePawTrail()
    {
        currentPawIndex = 0;

        if (pawMarkers == null)
        {
            return;
        }

        for (int i = 0; i < pawMarkers.Length; i++)
        {
            SetObjectActive(
                pawMarkers[i],
                false
            );
        }

        if (showFirstPawAtStart &&
            pawMarkers.Length > 0)
        {
            SetObjectActive(
                pawMarkers[0],
                true
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

    public bool TryCheckGate()
    {
        if (gateOpened ||
            sequenceBusy)
        {
            return false;
        }

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

        SetSubtitle(
            "The gate was locked."
        );

        /*
         * Only the first gate check may activate Paw1.
         * Paw1 will not appear again after it is selected.
         */
        if (!gateChecked)
        {
            gateChecked = true;

            if (currentPawIndex == 0 &&
                pawMarkers != null &&
                pawMarkers.Length > 0 &&
                pawMarkers[0] != null)
            {
                SetObjectActive(
                    pawMarkers[0],
                    true
                );
            }
        }

        if (keyRevealed)
        {
            SetInstruction(
                "Tap the glowing key."
            );
        }
        else if (pawMarkers != null &&
                 currentPawIndex < pawMarkers.Length)
        {
            if (currentPawIndex == 0)
            {
                SetInstruction(
                    "Follow Lumi's paw prints."
                );
            }
            else
            {
                SetInstruction(
                    "Follow the next paw print."
                );
            }
        }
        else
        {
            SetInstruction("");
        }

        return true;
    }

    public bool TrySelectPaw(
        int pawIndex)
    {
        if (gateOpened ||
            sequenceBusy)
        {
            return false;
        }

        if (requireGateCheckBeforePaws &&
            !gateChecked)
        {
            SetInstruction(
                "Check the garden gate first."
            );

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

        if (pawIndex != currentPawIndex)
        {
            return false;
        }

        if (pawIndex < 0 ||
            pawIndex >= pawMarkers.Length)
        {
            return false;
        }

        /*
         * Play the paw sound only when the player
         * selects the correct paw print.
         */
        PlayAudio(
            pawSelectClip
        );

        SetObjectActive(
            pawMarkers[pawIndex],
            false
        );

        if (currentPawIndex <
            pawMarkers.Length - 1)
        {
            currentPawIndex++;

            SetObjectActive(
                pawMarkers[currentPawIndex],
                true
            );

            SetInstruction(
                "Follow the next paw print."
            );
        }
        else
        {
            currentPawIndex++;

            StartCoroutine(
                RevealKeyRoutine()
            );
        }

        return true;
    }

    private IEnumerator RevealKeyRoutine()
    {
        sequenceBusy = true;

        SetInstruction("");

        SetSubtitle(
            "The paw prints ended beside the flowers."
        );

        /*
         * Wait before revealing the key.
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

        /*
         * Play the key reveal sound immediately
         * before the key appears.
         */
        PlayAudio(
            keyRevealClip
        );

        SetSubtitle(
            "A hidden key began to shine."
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

        keyRevealed = true;
        keyFloating = true;

        if (keyClickCollider != null)
        {
            keyClickCollider.enabled = true;
        }

        SetObjectActive(
            keyTapHint,
            true
        );

        SetInstruction(
            "Tap the glowing key."
        );

        sequenceBusy = false;
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

        /*
         * Play the key pickup or whoosh sound.
         */
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
        SetInstruction("");

        SetSubtitle(
            "The key flew towards the locked gate."
        );

        if (keyControlPoint == null ||
            lockPoint == null)
        {
            Debug.LogError(
                "Key Control Point or Lock Point has not been assigned."
            );

            sequenceBusy = false;
            yield break;
        }

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
                    elapsed / safeDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Vector3 controlPosition =
                keyControlPoint.position;

            Vector3 endPosition =
                lockPoint.position;

            keyRoot.transform.position =
                CalculateQuadraticBezier(
                    startPosition,
                    controlPosition,
                    endPosition,
                    smoothProgress
                );

            yield return null;
        }

        keyRoot.transform.position =
            lockPoint.position;

        /*
         * Play the unlock sound when the key
         * reaches the lock.
         */
        PlayAudio(
            unlockClip
        );

        yield return new WaitForSeconds(
            0.25f
        );

        keyRoot.SetActive(false);

        /*
         * Play the gate opening sound before
         * starting the door movement.
         */
        PlayAudio(
            gateOpenClip
        );

        yield return StartCoroutine(
            OpenGateRoutine()
        );

        gateOpened = true;
        sequenceBusy = false;

        if (gateClickCollider != null)
        {
            gateClickCollider.enabled = false;
        }

        SetSubtitle(
            "The gate to the Memory Garden opened."
        );

        SetInstruction(
            "Carry Lumi's Memory Seed into the garden."
        );

        Debug.Log(
            "The Memory Garden gate has opened."
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
                    elapsed / safeDuration
                );

            float wave =
                Mathf.Sin(
                    progress *
                    Mathf.PI *
                    2f *
                    lockedShakeCycles
                );

            float fade =
                1f - progress;

            float angle =
                wave *
                lockedShakeAngle *
                fade;

            gateShakeRoot.localRotation =
                startRotation *
                Quaternion.Euler(
                    lockedShakeAxis * angle
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
                    elapsed / safeDuration
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