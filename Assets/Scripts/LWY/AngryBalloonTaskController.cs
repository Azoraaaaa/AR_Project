using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AngryBalloonTaskController : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string Text;

        public AudioClip VoiceClip;
        public float DisplaySeconds = 3f;
    }

    [System.Serializable]
    public class BalloonStep
    {
        public GameObject BalloonObject;
        public Transform FingerTarget;
        public ParticleSystem PopParticles;
        public AudioClip PopClip;

        [HideInInspector] public bool Popped;
        [HideInInspector] public Vector3 StartLocalPosition;
        [HideInInspector] public float FloatPhase;
    }

    [Header("Camera")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private bool autoFindCamera = true;
    [SerializeField] private LayerMask balloonLayers = ~0;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Parent Task")]
    [SerializeField] private FlowerTaskInteractionController flowerTaskController;
    [SerializeField] private bool startAfterFlowerTaskStartedEvent = true;

    [Header("Balloons")]
    [SerializeField] private BalloonStep[] balloons = new BalloonStep[3];
    [SerializeField] private bool autoCreateMissingColliders = true;
    [SerializeField] private float floatAmplitude = 0.015f;
    [SerializeField] private float floatSpeed = 1.6f;
    [SerializeField] private float phaseOffsetPerBalloon = 1.2f;

    [Header("Angry Visuals")]
    [SerializeField] private GameObject angryOrbObject;
    [SerializeField] private Transform angryParticleRoot;
    [SerializeField] private float angryExpandScale = 1.25f;
    [SerializeField] private float angryShrinkScaleMultiplier = 0.85f;
    [SerializeField] private float angryScaleAnimSeconds = 0.35f;

    [Header("Butterfly Dialogue")]
    [SerializeField] private GameObject butterflyDialoguePanel;
    [SerializeField] private TMP_Text butterflyDialogueText;
    [SerializeField] private bool useVoiceClipLength = true;
    [SerializeField] private DialogueLine[] introDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] completedDialogue = new DialogueLine[0];

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [TextArea(2, 4)]
    [SerializeField] private string popBalloonHintText = "Pop the balloons to release pressure and anger";

    [SerializeField] private GameObject fingerHint;
    [SerializeField] private bool moveFingerToTarget = true;
    [SerializeField] private bool faceCameraWhenWorldSpace = true;
    [SerializeField] private Vector3 fingerWorldOffset = new Vector3(0f, 0.04f, 0f);
    [SerializeField] private Vector2 fingerScreenOffset = new Vector2(0f, 60f);

    [Header("Next Button")]
    [SerializeField] private Button nextButton;
    [SerializeField] private bool autoFindNextButton = true;
    [SerializeField] private string nextButtonName = "TaskCompleteButton";
    [SerializeField] private string nextButtonLabel = "Next";
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private bool hideNextButtonUntilComplete = true;

    [Header("Auto Find Scene UI")]
    [SerializeField] private bool autoFindSceneUi = true;
    [SerializeField] private string butterflyDialoguePanelName = "ButterflyDialoguePanel";
    [SerializeField] private string butterflyDialogueTextName = "ButterflyDialogueText";
    [SerializeField] private string hintPanelName = "HintPanel";
    [SerializeField] private string hintTextName = "HintText";
    [SerializeField] private string fingerHintName = "FingerHint";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip defaultPopClip;
    [SerializeField] private AudioClip showHintClip;
    [SerializeField] private AudioClip taskCompletedClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Header("Events")]
    public UnityEvent AllBalloonsPopped = new UnityEvent();

    private bool inputEnabled;
    private bool taskCompleted;
    private Vector3 angryOriginalScale = Vector3.one;
    private Vector3 angryCurrentTargetScale = Vector3.one;
    private Coroutine angryScaleRoutine;
    private Coroutine taskRoutine;
    private bool subscribedToFlowerTask;

    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
        PrepareBalloons();
        SetDialogueVisible(false);
        SetObjectActive(fingerHint, false);

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToFlowerTask();
        ResetTask();

        if (!startAfterFlowerTaskStartedEvent || flowerTaskController == null)
            StartTaskIntro();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlowerTask();
    }

    private void Update()
    {
        FloatBalloons();

        if (!inputEnabled || taskCompleted)
            return;

        if (!PointerDown(out Vector2 pointerPosition))
            return;

        TryPopBalloon(pointerPosition);
    }

    private IEnumerator TaskIntroRoutine()
    {
        inputEnabled = false;

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);

        yield return PlayDialogue(introDialogue);

        SetHintText(popBalloonHintText, true);
        ShowFingerForNextBalloon();
        PlayOneShot(showHintClip);

        inputEnabled = true;
    }

    private void StartTaskIntro()
    {
        if (taskRoutine != null)
            StopCoroutine(taskRoutine);

        taskRoutine = StartCoroutine(TaskIntroRoutine());
    }

    private void OnParentTaskStarted(int taskIndex)
    {
        StartTaskIntro();
    }

    private void ResetTask()
    {
        taskCompleted = false;
        inputEnabled = false;

        for (int i = 0; i < BalloonCount; i++)
        {
            if (balloons[i] == null)
                continue;

            balloons[i].Popped = false;

            if (balloons[i].BalloonObject != null)
            {
                balloons[i].BalloonObject.SetActive(true);
                balloons[i].BalloonObject.transform.localPosition = balloons[i].StartLocalPosition;
            }
        }

        if (angryParticleRoot != null)
            angryParticleRoot.localScale = angryOriginalScale;

        angryCurrentTargetScale = angryOriginalScale;

        if (angryOrbObject != null)
            angryOrbObject.SetActive(true);

        SetObjectActive(fingerHint, false);

        if (hideNextButtonUntilComplete)
            SetNextButtonVisible(false);
    }

    private void TryPopBalloon(Vector2 screenPosition)
    {
        ResolveCamera();

        if (arCamera == null)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, balloonLayers, QueryTriggerInteraction.Collide))
            return;

        for (int i = 0; i < BalloonCount; i++)
        {
            BalloonStep balloon = balloons[i];
            if (balloon == null || balloon.Popped || balloon.BalloonObject == null)
                continue;

            if (!IsTransformUnderRoot(hit.transform, balloon.BalloonObject.transform))
                continue;

            PopBalloon(i);
            return;
        }
    }

    private void PopBalloon(int index)
    {
        BalloonStep balloon = balloons[index];
        if (balloon == null || balloon.Popped)
            return;

        balloon.Popped = true;

        Vector3 popPosition = balloon.BalloonObject != null
            ? balloon.BalloonObject.transform.position
            : transform.position;

        AudioClip popClip = balloon.PopClip != null ? balloon.PopClip : defaultPopClip;
        PlayOneShot(popClip);

        if (balloon.BalloonObject != null)
            balloon.BalloonObject.SetActive(false);

        if (balloon.PopParticles != null)
        {
            balloon.PopParticles.transform.position = popPosition;
            balloon.PopParticles.gameObject.SetActive(true);
            balloon.PopParticles.Play(true);
        }

        AnimateAngryParticles();

        if (AllBalloonsArePopped())
        {
            StartCoroutine(CompleteTaskRoutine());
            return;
        }

        ShowFingerForNextBalloon();
    }

    private IEnumerator CompleteTaskRoutine()
    {
        taskCompleted = true;
        inputEnabled = false;

        SetObjectActive(fingerHint, false);
        SetObjectActive(hintPanel, false);

        if (angryOrbObject != null)
            angryOrbObject.SetActive(false);

        PlayOneShot(taskCompletedClip);
        AllBalloonsPopped.Invoke();

        yield return PlayDialogue(completedDialogue);

        SetNextButtonLabel();
        SetNextButtonVisible(true);
    }

    private void FloatBalloons()
    {
        for (int i = 0; i < BalloonCount; i++)
        {
            BalloonStep balloon = balloons[i];
            if (balloon == null || balloon.Popped || balloon.BalloonObject == null || !balloon.BalloonObject.activeSelf)
                continue;

            Vector3 localPosition = balloon.StartLocalPosition;
            localPosition.y += Mathf.Sin(Time.time * floatSpeed + balloon.FloatPhase) * floatAmplitude;
            balloon.BalloonObject.transform.localPosition = localPosition;
        }
    }

    private void AnimateAngryParticles()
    {
        if (angryParticleRoot == null)
            return;

        if (angryScaleRoutine != null)
            StopCoroutine(angryScaleRoutine);

        angryScaleRoutine = StartCoroutine(AngryScaleRoutine());
    }

    private IEnumerator AngryScaleRoutine()
    {
        Vector3 startScale = angryParticleRoot.localScale;
        Vector3 expandedScale = startScale * angryExpandScale;
        Vector3 finalScale = angryCurrentTargetScale * angryShrinkScaleMultiplier;
        angryCurrentTargetScale = finalScale;

        yield return ScaleOverTime(startScale, expandedScale, angryScaleAnimSeconds * 0.45f);
        yield return ScaleOverTime(expandedScale, finalScale, angryScaleAnimSeconds * 0.55f);

        angryScaleRoutine = null;
    }

    private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float seconds)
    {
        if (seconds <= 0f)
        {
            angryParticleRoot.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            angryParticleRoot.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        angryParticleRoot.localScale = to;
    }

    private IEnumerator PlayDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            yield break;

        SetDialogueVisible(true);

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine line = lines[i];
            if (line == null)
                continue;

            if (butterflyDialogueText != null)
                butterflyDialogueText.text = line.Text;

            PlayOneShot(line.VoiceClip);

            float seconds = Mathf.Max(0f, line.DisplaySeconds);
            if (useVoiceClipLength && line.VoiceClip != null)
                seconds = Mathf.Max(seconds, line.VoiceClip.length);

            yield return new WaitForSeconds(seconds);
        }

        SetDialogueVisible(false);
    }

    private void ShowFingerForNextBalloon()
    {
        BalloonStep nextBalloon = GetNextUnpoppedBalloon();
        if (nextBalloon == null)
        {
            SetObjectActive(fingerHint, false);
            return;
        }

        Transform target = nextBalloon.FingerTarget != null
            ? nextBalloon.FingerTarget
            : nextBalloon.BalloonObject.transform;

        ShowFingerHint(target);
    }

    private void ShowFingerHint(Transform target)
    {
        if (fingerHint == null)
            return;

        if (target == null)
        {
            fingerHint.SetActive(false);
            return;
        }

        fingerHint.SetActive(true);

        if (!moveFingerToTarget)
            return;

        MoveFingerToTarget(target);
    }

    private void MoveFingerToTarget(Transform target)
    {
        Canvas canvas = fingerHint.GetComponentInParent<Canvas>(true);
        RectTransform fingerRect = fingerHint.GetComponent<RectTransform>();

        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace && fingerRect != null)
        {
            ResolveCamera();

            if (arCamera == null)
                return;

            Vector3 screenPosition = arCamera.WorldToScreenPoint(target.position + fingerWorldOffset);
            screenPosition.x += fingerScreenOffset.x;
            screenPosition.y += fingerScreenOffset.y;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                fingerRect.position = screenPosition;
            }
            else
            {
                Camera canvasCamera = canvas.worldCamera != null ? canvas.worldCamera : arCamera;
                RectTransform canvasRect = canvas.transform as RectTransform;
                if (canvasRect != null &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvasCamera, out Vector2 localPosition))
                {
                    fingerRect.anchoredPosition = localPosition;
                }
            }

            return;
        }

        fingerHint.transform.position = target.position + target.TransformVector(fingerWorldOffset);

        if (faceCameraWhenWorldSpace)
            FaceCamera(fingerHint.transform);
    }

    private void FaceCamera(Transform target)
    {
        ResolveCamera();

        if (target == null || arCamera == null)
            return;

        Vector3 direction = target.position - arCamera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        target.rotation = Quaternion.LookRotation(direction, arCamera.transform.up);
    }

    private BalloonStep GetNextUnpoppedBalloon()
    {
        for (int i = 0; i < BalloonCount; i++)
        {
            BalloonStep balloon = balloons[i];
            if (balloon != null && !balloon.Popped && balloon.BalloonObject != null && balloon.BalloonObject.activeInHierarchy)
                return balloon;
        }

        return null;
    }

    private bool AllBalloonsArePopped()
    {
        for (int i = 0; i < BalloonCount; i++)
        {
            BalloonStep balloon = balloons[i];
            if (balloon != null && !balloon.Popped)
                return false;
        }

        return true;
    }

    private bool PointerDown(out Vector2 position)
    {
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }

    private void ResolveReferences()
    {
        ResolveFlowerTaskController();
        ResolveCamera();
        ResolveSceneUi();
        ResolveNextButton();
    }

    private void ResolveFlowerTaskController()
    {
        if (flowerTaskController != null)
            return;

        flowerTaskController = GetComponentInParent<FlowerTaskInteractionController>();
    }

    private void SubscribeToFlowerTask()
    {
        if (subscribedToFlowerTask || flowerTaskController == null)
            return;

        flowerTaskController.TaskStarted.AddListener(OnParentTaskStarted);
        subscribedToFlowerTask = true;
    }

    private void UnsubscribeFromFlowerTask()
    {
        if (!subscribedToFlowerTask || flowerTaskController == null)
            return;

        flowerTaskController.TaskStarted.RemoveListener(OnParentTaskStarted);
        subscribedToFlowerTask = false;
    }

    private void ResolveCamera()
    {
        if (!autoFindCamera || arCamera != null)
            return;

        arCamera = Camera.main;

        if (arCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            arCamera = FindFirstObjectByType<Camera>();
#else
            arCamera = FindObjectOfType<Camera>();
#endif
        }
    }

    private void ResolveSceneUi()
    {
        if (!autoFindSceneUi)
            return;

        if (butterflyDialoguePanel == null)
            butterflyDialoguePanel = FindSceneGameObject(butterflyDialoguePanelName);

        if (butterflyDialogueText == null)
            butterflyDialogueText = FindSceneText(butterflyDialogueTextName, butterflyDialoguePanel);

        if (hintPanel == null)
            hintPanel = FindSceneGameObject(hintPanelName);

        if (hintText == null)
            hintText = FindSceneText(hintTextName, hintPanel);

        if (fingerHint == null)
            fingerHint = FindSceneGameObject(fingerHintName);
    }

    private void ResolveNextButton()
    {
        if (!autoFindNextButton || nextButton != null || string.IsNullOrEmpty(nextButtonName))
            return;

        GameObject buttonObject = FindSceneGameObject(nextButtonName);
        if (buttonObject != null)
            nextButton = buttonObject.GetComponent<Button>();

        if (nextButton != null && nextButtonText == null)
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);
    }

    private TMP_Text FindSceneText(string objectName, GameObject fallbackRoot)
    {
        GameObject textObject = FindSceneGameObject(objectName);
        if (textObject != null)
        {
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            if (text != null)
                return text;
        }

        return fallbackRoot != null ? fallbackRoot.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private GameObject FindSceneGameObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.name != objectName)
                continue;

            if (!target.gameObject.scene.IsValid())
                continue;

            return target.gameObject;
        }

        return null;
    }

    private void CacheInitialState()
    {
        for (int i = 0; i < BalloonCount; i++)
        {
            if (balloons[i] == null || balloons[i].BalloonObject == null)
                continue;

            balloons[i].StartLocalPosition = balloons[i].BalloonObject.transform.localPosition;
            balloons[i].FloatPhase = i * phaseOffsetPerBalloon;
        }

        if (angryParticleRoot != null)
        {
            angryOriginalScale = angryParticleRoot.localScale;
            angryCurrentTargetScale = angryOriginalScale;
        }
    }

    private void PrepareBalloons()
    {
        if (!autoCreateMissingColliders)
            return;

        for (int i = 0; i < BalloonCount; i++)
        {
            if (balloons[i] != null)
                EnsureCollider(balloons[i].BalloonObject);
        }
    }

    private void EnsureCollider(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<Collider>(true) != null)
            return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider collider = target.AddComponent<BoxCollider>();
        collider.center = target.transform.InverseTransformPoint(bounds.center);

        Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private void SetDialogueVisible(bool visible)
    {
        SetObjectActive(butterflyDialoguePanel, visible);
    }

    private void SetHintText(string message, bool visible)
    {
        SetObjectActive(hintPanel, visible);

        if (hintText != null)
            hintText.text = message;
    }

    private void SetNextButtonLabel()
    {
        if (nextButtonText != null)
            nextButtonText.text = nextButtonLabel;
    }

    private void SetNextButtonVisible(bool visible)
    {
        ResolveNextButton();

        if (nextButton != null)
            nextButton.gameObject.SetActive(visible);
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, audioVolume);
    }

    private bool IsTransformUnderRoot(Transform child, Transform root)
    {
        if (child == null || root == null)
            return false;

        return child == root || child.IsChildOf(root);
    }

    private int BalloonCount
    {
        get { return balloons == null ? 0 : balloons.Length; }
    }
}
