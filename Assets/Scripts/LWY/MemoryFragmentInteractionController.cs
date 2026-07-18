using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MemoryFragmentInteractionController : MonoBehaviour
{
    enum InteractionState
    {
        WaitingToStart,
        OpeningDialogue,
        CatchingLights,
        ShowingMemory,
        FinalReveal,
        WaitingForFinalStarClick,
        Complete
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string Text;

        public AudioClip VoiceClip;
        public float DisplaySeconds = 3f;
    }

    [System.Serializable]
    public class MemoryLight
    {
        [Header("Scene Light")]
        public GameObject LightObject;
        public Transform LightTransform;
        public float CatchRadius = 0.03f;

        [Header("Memory UI")]
        public Sprite MemoryImage;

        [TextArea(2, 4)]
        public string MemoryHintText = "Look at this memory";

        [Header("Collected Star UI")]
        public Image StarImage;

        [Header("Audio Overrides")]
        public AudioClip CaughtClip;
        public AudioClip StarChangedClip;

        [HideInInspector] public bool Collected;
        [HideInInspector] public Vector3 StartPosition;
        [HideInInspector] public Vector3 StartLocalScale;
        [HideInInspector] public Vector3 Velocity;
        [HideInInspector] public Color StarStartColor;
        [HideInInspector] public bool StarStartActive;
    }

    struct PointerInput
    {
        public Vector2 Position;
        public bool Down;
        public bool Held;
        public bool Up;
    }

    [Header("Start")]
    [SerializeField] private bool playOnEnable = true;

    [Header("Camera")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private bool autoFindCamera = true;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Butterfly Dialogue")]
    [SerializeField] private GameObject butterflyDialoguePanel;
    [SerializeField] private TMP_Text butterflyDialogueText;
    [SerializeField] private bool useVoiceClipLength = true;
    [SerializeField] private DialogueLine[] openingDialogue = new DialogueLine[0];
    [SerializeField] private DialogueLine[] finalStarDialogue = new DialogueLine[0];
    [SerializeField] private int showLightAreaOnDialogueLine = 3;
    [SerializeField] private int showNetOnDialogueLine = 4;

    [Header("Text Typing")]
    [SerializeField] private float dialogueTypingSeconds = LwyTypewriterText.DefaultCharacterSeconds;
    [SerializeField] private float hintTypingSeconds = LwyTypewriterText.DefaultCharacterSeconds;

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [TextArea(2, 4)]
    [SerializeField] private string catchLightsHintText = "Use the net to catch the lights";

    [TextArea(2, 4)]
    [SerializeField] private string finalStarHintText = "Tap the star";

    [Header("Light Area")]
    [SerializeField] private GameObject lightAreaRoot;
    [SerializeField] private BoxCollider lightAreaCollider;
    [SerializeField] private bool forceLightAreaIsTrigger = true;
    [SerializeField] private MemoryLight[] memoryLights = new MemoryLight[3];
    [SerializeField] private float lightFloatSpeed = 0.05f;
    [SerializeField] private float lightFadeSeconds = 0.45f;

    [Header("Net")]
    [SerializeField] private GameObject netObject;
    [SerializeField] private Transform netTransform;
    [SerializeField] private SphereCollider netCollider;
    [SerializeField] private Rigidbody netRigidbody;
    [SerializeField] private bool forceNetColliderIsTrigger = true;
    [SerializeField] private bool dragNetWithPointer = true;
    [SerializeField] private Transform dragPlaneReference;
    [SerializeField] private float netLiftHeight = 0.01f;

    [Header("Net Hint")]
    [SerializeField] private GameObject netFingerHint;
    [SerializeField] private bool showNetFingerHintWhenWaiting = true;

    [Header("Mini Game Canvas")]
    [SerializeField] private GameObject miniGameCanvasRoot;
    [SerializeField] private CanvasGroup miniGameCanvasGroup;
    [SerializeField] private Image memoryImage;
    [SerializeField] private float memoryImageFadeSeconds = 0.45f;
    [SerializeField] private float memoryImageDisplaySeconds = 2.5f;

    [Header("Collected Stars")]
    [SerializeField] private Color uncollectedStarColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color collectedStarColor = Color.white;
    [SerializeField] private float collectedStarFadeSeconds = 0.35f;

    [Header("Final Star")]
    [SerializeField] private GameObject finalStarObject;
    [SerializeField] private Transform finalStarTransform;
    [SerializeField] private Collider finalStarCollider;
    [SerializeField] private Vector3 finalStarStartLocalOffset = new Vector3(0f, -0.12f, 0f);
    [SerializeField] private float finalStarStartScaleMultiplier = 0.35f;
    [SerializeField] private float finalStarRevealSeconds = 0.9f;
    [SerializeField] private ParticleSystem finalStarParticles;
    [SerializeField] private bool hideFinalStarAfterClick = true;

    [Header("Props Bar")]
    [SerializeField] private GameObject propsBarBeforeImage;
    [SerializeField] private GameObject propsBarAfterImage;

    [Header("Auto Find Scene UI")]
    [SerializeField] private bool autoFindSceneUi = true;
    [SerializeField] private string butterflyDialoguePanelName = "ButterflyDialoguePanel";
    [SerializeField] private string butterflyDialogueTextName = "ButterflyDialogueText";
    [SerializeField] private string hintPanelName = "HintPanel";
    [SerializeField] private string hintTextName = "HintText";
    [SerializeField] private string propsBarBeforeImageName = "PropsBarBeforeImage";
    [SerializeField] private string propsBarAfterImageName = "PropsBarAfterImage";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip showHintClip;
    [SerializeField] private AudioClip netDragStartClip;
    [SerializeField] private AudioClip defaultLightCaughtClip;
    [SerializeField] private AudioClip defaultStarChangedClip;
    [SerializeField] private AudioClip finalStarRevealClip;
    [SerializeField] private AudioClip finalStarClickClip;
    [SerializeField] private AudioClip propsBarChangedClip;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Header("Events")]
    public UnityEvent AllFragmentsCollected = new UnityEvent();
    public UnityEvent FinalStarCollected = new UnityEvent();

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private InteractionState state = InteractionState.WaitingToStart;
    private Coroutine flowRoutine;
    private Coroutine hintTypingRoutine;
    private Plane netDragPlane;
    private Vector3 netDragOffset;
    private Vector3 netStartLocalPosition;
    private Quaternion netStartLocalRotation = Quaternion.identity;
    private Vector3 netStartLocalScale = Vector3.one;
    private bool draggingNet;
    private Vector3 finalStarEndLocalPosition;
    private Vector3 finalStarEndLocalScale = Vector3.one;

    private int MemoryCount
    {
        get { return memoryLights == null ? 0 : memoryLights.Length; }
    }

    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
        SetInitialVisibility();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResetInteraction();

        if (playOnEnable)
            StartInteraction();
    }

    private void OnDisable()
    {
        StopRunningRoutines();
        draggingNet = false;
    }

    private void Update()
    {
        if (state == InteractionState.CatchingLights || state == InteractionState.ShowingMemory)
            UpdateFloatingLights();

        if (state == InteractionState.CatchingLights)
        {
            UpdateNetDrag();
            CheckLightCaughtByNet();
            return;
        }

        if (state == InteractionState.WaitingForFinalStarClick && PointerDown(out Vector2 pointerPosition))
        {
            if (IsFinalStarClicked(pointerPosition))
                CollectFinalStar();
        }
    }

    public void StartInteraction()
    {
        StopRunningRoutines();
        flowRoutine = StartCoroutine(InteractionRoutine());
    }

    public void ResetInteraction()
    {
        StopRunningRoutines();

        state = InteractionState.WaitingToStart;
        draggingNet = false;

        ReturnNetToStart();
        SetObjectActive(lightAreaRoot, false);
        SetObjectActive(netObject, false);
        SetObjectActive(netFingerHint, false);
        SetMiniGameCanvasVisible(false, true);
        SetDialogueVisible(false);
        SetHintText("", false);
        SetObjectActive(finalStarObject, false);

        Transform finalStar = GetFinalStarTransform();
        if (finalStar != null)
        {
            finalStar.localPosition = finalStarEndLocalPosition;
            finalStar.localScale = finalStarEndLocalScale;
        }

        if (finalStarParticles != null)
            finalStarParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        for (int i = 0; i < MemoryCount; i++)
        {
            MemoryLight memory = memoryLights[i];
            if (memory == null)
                continue;

            memory.Collected = false;
            ResolveMemoryReferences(memory);

            if (memory.LightObject != null)
            {
                memory.LightObject.SetActive(false);
                memory.LightObject.transform.position = memory.StartPosition;
                memory.LightObject.transform.localScale = memory.StartLocalScale;
            }

            if (memory.StarImage != null)
            {
                memory.StarImage.gameObject.SetActive(true);
                memory.StarImage.color = uncollectedStarColor;
            }

            memory.Velocity = CreateRandomLightVelocity();
        }
    }

    private IEnumerator InteractionRoutine()
    {
        state = InteractionState.OpeningDialogue;

        yield return PlayOpeningDialogue();

        ShowLightAreaAndLights();
        ShowNet();

        SetHintText(catchLightsHintText, true);
        PlayOneShot(showHintClip);

        state = InteractionState.CatchingLights;
    }

    private IEnumerator PlayOpeningDialogue()
    {
        SetDialogueVisible(true);

        if (openingDialogue == null)
        {
            SetDialogueVisible(false);
            yield break;
        }

        for (int i = 0; i < openingDialogue.Length; i++)
        {
            int lineNumber = i + 1;

            if (lineNumber == showLightAreaOnDialogueLine)
                ShowLightAreaAndLights();

            if (lineNumber == showNetOnDialogueLine)
                ShowNet();

            yield return PlayDialogueLine(openingDialogue[i]);
        }

        SetDialogueVisible(false);
    }

    private IEnumerator PlayFinalStarDialogue()
    {
        SetDialogueVisible(true);

        if (finalStarDialogue == null)
        {
            SetDialogueVisible(false);
            yield break;
        }

        for (int i = 0; i < finalStarDialogue.Length; i++)
            yield return PlayDialogueLine(finalStarDialogue[i]);

        SetDialogueVisible(false);
    }

    private IEnumerator PlayDialogueLine(DialogueLine line)
    {
        if (line == null)
            yield break;

        PlayOneShot(line.VoiceClip);

        float seconds = Mathf.Max(0f, line.DisplaySeconds);
        if (useVoiceClipLength && line.VoiceClip != null)
            seconds = Mathf.Max(seconds, line.VoiceClip.length);

        float typingSeconds = 0f;
        if (butterflyDialogueText != null)
        {
            yield return LwyTypewriterText.TypeText(butterflyDialogueText, line.Text, dialogueTypingSeconds);
            typingSeconds = LwyTypewriterText.GetTypingDuration(line.Text, dialogueTypingSeconds);
        }

        float remainingSeconds = seconds - typingSeconds;
        if (remainingSeconds > 0f)
            yield return new WaitForSeconds(remainingSeconds);
    }

    private void ShowLightAreaAndLights()
    {
        SetObjectActive(lightAreaRoot, true);

        for (int i = 0; i < MemoryCount; i++)
        {
            MemoryLight memory = memoryLights[i];
            if (memory == null || memory.Collected || memory.LightObject == null)
                continue;

            memory.LightObject.SetActive(true);
        }
    }

    private void ShowNet()
    {
        SetObjectActive(netObject, true);
        SetNetFingerHintVisible(true);
    }

    private void UpdateFloatingLights()
    {
        if (lightAreaCollider == null)
            return;

        Bounds bounds = lightAreaCollider.bounds;

        for (int i = 0; i < MemoryCount; i++)
        {
            MemoryLight memory = memoryLights[i];
            if (memory == null || memory.Collected || memory.LightTransform == null)
                continue;

            Vector3 velocity = memory.Velocity;
            Vector3 position = memory.LightTransform.position + velocity * Time.deltaTime;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            BounceInsideBounds(ref position, ref velocity, min, max);

            memory.Velocity = velocity;
            memory.LightTransform.position = position;
        }
    }

    private void BounceInsideBounds(ref Vector3 position, ref Vector3 velocity, Vector3 min, Vector3 max)
    {
        if (position.x < min.x)
        {
            position.x = min.x;
            velocity.x = Mathf.Abs(velocity.x);
        }
        else if (position.x > max.x)
        {
            position.x = max.x;
            velocity.x = -Mathf.Abs(velocity.x);
        }

        if (position.y < min.y)
        {
            position.y = min.y;
            velocity.y = Mathf.Abs(velocity.y);
        }
        else if (position.y > max.y)
        {
            position.y = max.y;
            velocity.y = -Mathf.Abs(velocity.y);
        }

        if (position.z < min.z)
        {
            position.z = min.z;
            velocity.z = Mathf.Abs(velocity.z);
        }
        else if (position.z > max.z)
        {
            position.z = max.z;
            velocity.z = -Mathf.Abs(velocity.z);
        }
    }

    private void UpdateNetDrag()
    {
        if (!dragNetWithPointer || netTransform == null)
            return;

        if (!TryGetPrimaryPointer(out PointerInput pointer))
            return;

        if (pointer.Down && IsNetClicked(pointer.Position))
            BeginNetDrag(pointer.Position);

        if (draggingNet && pointer.Held)
            DragNet(pointer.Position);

        if (draggingNet && pointer.Up)
            EndNetDrag();
    }

    private bool IsNetClicked(Vector2 screenPosition)
    {
        ResolveCamera();

        if (arCamera == null || netObject == null)
            return false;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayers, QueryTriggerInteraction.Collide))
            return false;

        return hit.transform == netObject.transform || hit.transform.IsChildOf(netObject.transform);
    }

    private void BeginNetDrag(Vector2 screenPosition)
    {
        ResolveCamera();

        if (arCamera == null || netTransform == null)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        Vector3 planeNormal = dragPlaneReference != null ? dragPlaneReference.up : -arCamera.transform.forward;
        netDragPlane = new Plane(planeNormal, netTransform.position);

        if (netDragPlane.Raycast(ray, out float enter))
            netDragOffset = netTransform.position - ray.GetPoint(enter);
        else
            netDragOffset = Vector3.zero;

        draggingNet = true;
        SetNetFingerHintVisible(false);
        PlayOneShot(netDragStartClip);
    }

    private void DragNet(Vector2 screenPosition)
    {
        ResolveCamera();

        if (arCamera == null || netTransform == null)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!netDragPlane.Raycast(ray, out float enter))
            return;

        Vector3 planeNormal = dragPlaneReference != null ? dragPlaneReference.up : -arCamera.transform.forward;
        Vector3 targetPosition = ray.GetPoint(enter) + netDragOffset + planeNormal * netLiftHeight;

        if (netRigidbody != null)
            netRigidbody.MovePosition(targetPosition);
        else
            netTransform.position = targetPosition;
    }

    private void EndNetDrag()
    {
        draggingNet = false;
        ReturnNetToStart();

        if (state == InteractionState.CatchingLights)
            SetNetFingerHintVisible(true);
    }

    private void ReturnNetToStart()
    {
        if (netTransform == null)
            return;

        netTransform.localPosition = netStartLocalPosition;
        netTransform.localRotation = netStartLocalRotation;
        netTransform.localScale = netStartLocalScale;

        if (netRigidbody != null)
        {
            netRigidbody.linearVelocity = Vector3.zero;
            netRigidbody.angularVelocity = Vector3.zero;
            netRigidbody.position = netRigidbody.transform.position;
            netRigidbody.rotation = netRigidbody.transform.rotation;
        }
    }

    private void CheckLightCaughtByNet()
    {
        if (netCollider == null)
            return;

        Vector3 netCenter = netCollider.transform.TransformPoint(netCollider.center);
        float netRadius = GetSphereColliderWorldRadius(netCollider);

        for (int i = 0; i < MemoryCount; i++)
        {
            MemoryLight memory = memoryLights[i];
            if (memory == null || memory.Collected || memory.LightTransform == null)
                continue;

            float distance = Vector3.Distance(netCenter, memory.LightTransform.position);
            if (distance <= netRadius + Mathf.Max(0f, memory.CatchRadius))
            {
                flowRoutine = StartCoroutine(CatchMemoryRoutine(i));
                break;
            }
        }
    }

    private IEnumerator CatchMemoryRoutine(int memoryIndex)
    {
        if (memoryIndex < 0 || memoryIndex >= MemoryCount || memoryLights[memoryIndex] == null)
            yield break;

        state = InteractionState.ShowingMemory;
        draggingNet = false;
        SetNetFingerHintVisible(false);
        ReturnNetToStart();

        MemoryLight memory = memoryLights[memoryIndex];
        memory.Collected = true;

        PlayOneShot(memory.CaughtClip != null ? memory.CaughtClip : defaultLightCaughtClip);
        yield return FadeLightOutRoutine(memory);

        SetHintText(memory.MemoryHintText, true);
        yield return ShowMemoryImageRoutine(memory.MemoryImage);

        ReturnNetToStart();
        SetHintText(catchLightsHintText, true);
        yield return MarkStarCollectedRoutine(memory);

        if (AllMemoriesCollected())
        {
            AllFragmentsCollected.Invoke();
            flowRoutine = StartCoroutine(AllFragmentsCollectedRoutine());
        }
        else
        {
            state = InteractionState.CatchingLights;
            SetNetFingerHintVisible(true);
        }
    }

    private IEnumerator FadeLightOutRoutine(MemoryLight memory)
    {
        if (memory == null || memory.LightTransform == null)
            yield break;

        Transform target = memory.LightTransform;
        Vector3 startScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < lightFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = lightFadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / lightFadeSeconds);
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, Smooth(t));
            yield return null;
        }

        target.localScale = Vector3.zero;

        if (memory.LightObject != null)
            memory.LightObject.SetActive(false);
    }

    private IEnumerator ShowMemoryImageRoutine(Sprite sprite)
    {
        if (memoryImage != null)
        {
            memoryImage.sprite = sprite;
            memoryImage.enabled = sprite != null;
        }

        yield return FadeMiniGameCanvasRoutine(1f);

        if (memoryImageDisplaySeconds > 0f)
            yield return new WaitForSeconds(memoryImageDisplaySeconds);

        yield return FadeMiniGameCanvasRoutine(0f);
    }

    private IEnumerator FadeMiniGameCanvasRoutine(float targetAlpha)
    {
        if (miniGameCanvasRoot == null && miniGameCanvasGroup == null)
            yield break;

        SetObjectActive(miniGameCanvasRoot, true);

        if (miniGameCanvasGroup == null && miniGameCanvasRoot != null)
            miniGameCanvasGroup = miniGameCanvasRoot.GetComponent<CanvasGroup>();

        if (miniGameCanvasGroup == null && miniGameCanvasRoot != null)
            miniGameCanvasGroup = miniGameCanvasRoot.AddComponent<CanvasGroup>();

        if (miniGameCanvasGroup == null)
            yield break;

        float startAlpha = miniGameCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < memoryImageFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = memoryImageFadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / memoryImageFadeSeconds);
            miniGameCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Smooth(t));
            yield return null;
        }

        miniGameCanvasGroup.alpha = targetAlpha;
        miniGameCanvasGroup.interactable = targetAlpha > 0.001f;
        miniGameCanvasGroup.blocksRaycasts = targetAlpha > 0.001f;

        if (targetAlpha <= 0.001f)
            SetObjectActive(miniGameCanvasRoot, false);
    }

    private IEnumerator MarkStarCollectedRoutine(MemoryLight memory)
    {
        if (memory == null || memory.StarImage == null)
            yield break;

        PlayOneShot(memory.StarChangedClip != null ? memory.StarChangedClip : defaultStarChangedClip);

        Color startColor = memory.StarImage.color;
        Color targetColor = collectedStarColor;
        float elapsed = 0f;

        while (elapsed < collectedStarFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = collectedStarFadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / collectedStarFadeSeconds);
            memory.StarImage.color = Color.Lerp(startColor, targetColor, Smooth(t));
            yield return null;
        }

        memory.StarImage.color = targetColor;
    }

    private IEnumerator AllFragmentsCollectedRoutine()
    {
        state = InteractionState.FinalReveal;

        SetObjectActive(netObject, false);
        SetObjectActive(netFingerHint, false);
        SetHintText("", false);

        yield return RevealFinalStarRoutine();
        yield return PlayFinalStarDialogue();

        SetHintText(finalStarHintText, true);
        PlayOneShot(showHintClip);
        state = InteractionState.WaitingForFinalStarClick;
    }

    private IEnumerator RevealFinalStarRoutine()
    {
        PlayOneShot(finalStarRevealClip);

        if (finalStarParticles != null)
        {
            finalStarParticles.Clear(true);
            finalStarParticles.Play(true);
        }

        SetObjectActive(finalStarObject, true);

        Transform star = GetFinalStarTransform();
        Vector3 finalStartLocal = finalStarEndLocalPosition + finalStarStartLocalOffset;
        Vector3 finalStartScale = finalStarEndLocalScale * Mathf.Max(0f, finalStarStartScaleMultiplier);

        if (star != null)
        {
            star.localPosition = finalStartLocal;
            star.localScale = finalStartScale;
        }

        float elapsed = 0f;
        while (elapsed < finalStarRevealSeconds)
        {
            elapsed += Time.deltaTime;
            float t = finalStarRevealSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / finalStarRevealSeconds);
            float smoothT = Smooth(t);

            FadeCollectedStarImages(1f - smoothT);

            if (star != null)
            {
                star.localPosition = Vector3.Lerp(finalStartLocal, finalStarEndLocalPosition, smoothT);
                star.localScale = Vector3.Lerp(finalStartScale, finalStarEndLocalScale, smoothT);
            }

            yield return null;
        }

        FadeCollectedStarImages(0f);

        for (int i = 0; i < MemoryCount; i++)
        {
            if (memoryLights[i] != null && memoryLights[i].StarImage != null)
                memoryLights[i].StarImage.gameObject.SetActive(false);
        }

        if (star != null)
        {
            star.localPosition = finalStarEndLocalPosition;
            star.localScale = finalStarEndLocalScale;
        }
    }

    private void FadeCollectedStarImages(float alpha)
    {
        for (int i = 0; i < MemoryCount; i++)
        {
            MemoryLight memory = memoryLights[i];
            if (memory == null || memory.StarImage == null)
                continue;

            Color color = collectedStarColor;
            color.a *= Mathf.Clamp01(alpha);
            memory.StarImage.color = color;
        }
    }

    private bool IsFinalStarClicked(Vector2 screenPosition)
    {
        ResolveCamera();
        if (arCamera == null || finalStarCollider == null)
            return false;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        return finalStarCollider.Raycast(ray, out RaycastHit _, raycastDistance);
    }

    private void CollectFinalStar()
    {
        state = InteractionState.Complete;
        SetHintText("", false);

        PlayOneShot(finalStarClickClip);

        if (hideFinalStarAfterClick)
            SetObjectActive(finalStarObject, false);

        SetObjectActive(propsBarBeforeImage, false);
        SetObjectActive(propsBarAfterImage, true);
        PlayOneShot(propsBarChangedClip);

        FinalStarCollected.Invoke();
        ShowNextPageCanvas();
    }

    private void ShowNextPageCanvas()
    {
        if (SimpleCloudRecoEventHandler.Instance != null)
            SimpleCloudRecoEventHandler.Instance.ShowNextPageCanvas();
    }

    private bool AllMemoriesCollected()
    {
        bool hasMemory = false;

        for (int i = 0; i < MemoryCount; i++)
        {
            if (memoryLights[i] == null || memoryLights[i].LightObject == null)
                continue;

            hasMemory = true;

            if (!memoryLights[i].Collected)
                return false;
        }

        return hasMemory;
    }

    private Transform GetFinalStarTransform()
    {
        if (finalStarTransform != null)
            return finalStarTransform;

        return finalStarObject != null ? finalStarObject.transform : null;
    }

    private void CacheInitialState()
    {
        for (int i = 0; i < MemoryCount; i++)
        {
            MemoryLight memory = memoryLights[i];
            if (memory == null)
                continue;

            ResolveMemoryReferences(memory);

            if (memory.LightObject != null)
            {
                memory.StartPosition = memory.LightObject.transform.position;
                memory.StartLocalScale = memory.LightObject.transform.localScale;
            }

            if (memory.StarImage != null)
            {
                memory.StarStartColor = memory.StarImage.color;
                memory.StarStartActive = memory.StarImage.gameObject.activeSelf;
            }
        }

        Transform star = GetFinalStarTransform();
        if (star != null)
        {
            finalStarEndLocalPosition = star.localPosition;
            finalStarEndLocalScale = star.localScale;
        }

        if (netTransform != null)
        {
            netStartLocalPosition = netTransform.localPosition;
            netStartLocalRotation = netTransform.localRotation;
            netStartLocalScale = netTransform.localScale;
        }
    }

    private void SetInitialVisibility()
    {
        SetObjectActive(lightAreaRoot, false);
        SetObjectActive(netObject, false);
        SetObjectActive(netFingerHint, false);
        SetMiniGameCanvasVisible(false, true);
        SetObjectActive(finalStarObject, false);
        SetDialogueVisible(false);
        SetHintText("", false);
    }

    private void SetMiniGameCanvasVisible(bool visible, bool instant)
    {
        SetObjectActive(miniGameCanvasRoot, visible);

        if (miniGameCanvasGroup == null && miniGameCanvasRoot != null)
            miniGameCanvasGroup = miniGameCanvasRoot.GetComponent<CanvasGroup>();

        if (miniGameCanvasGroup != null)
        {
            miniGameCanvasGroup.alpha = visible ? 1f : 0f;
            miniGameCanvasGroup.interactable = visible;
            miniGameCanvasGroup.blocksRaycasts = visible;
        }

        if (instant && memoryImage != null)
            memoryImage.enabled = false;
    }

    private void ResolveReferences()
    {
        ResolveCamera();
        ResolveSceneUi();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (lightAreaRoot == null && lightAreaCollider != null)
            lightAreaRoot = lightAreaCollider.gameObject;

        if (lightAreaCollider == null && lightAreaRoot != null)
            lightAreaCollider = lightAreaRoot.GetComponentInChildren<BoxCollider>(true);

        if (lightAreaCollider != null && forceLightAreaIsTrigger)
            lightAreaCollider.isTrigger = true;

        if (netObject == null && netTransform != null)
            netObject = netTransform.gameObject;

        if (netObject == null && netCollider != null)
            netObject = netCollider.gameObject;

        if (netTransform == null && netObject != null)
            netTransform = netObject.transform;

        if (netCollider == null && netObject != null)
            netCollider = netObject.GetComponentInChildren<SphereCollider>(true);

        if (netRigidbody == null && netObject != null)
            netRigidbody = netObject.GetComponentInChildren<Rigidbody>(true);

        if (netCollider != null && forceNetColliderIsTrigger)
            netCollider.isTrigger = true;

        if (miniGameCanvasRoot == null && miniGameCanvasGroup != null)
            miniGameCanvasRoot = miniGameCanvasGroup.gameObject;

        if (miniGameCanvasRoot == null && memoryImage != null)
            miniGameCanvasRoot = memoryImage.gameObject;

        if (finalStarObject == null && finalStarCollider != null)
            finalStarObject = finalStarCollider.gameObject;

        if (finalStarObject == null && finalStarTransform != null)
            finalStarObject = finalStarTransform.gameObject;

        if (finalStarTransform == null && finalStarObject != null)
            finalStarTransform = finalStarObject.transform;

        if (finalStarCollider == null && finalStarObject != null)
            finalStarCollider = finalStarObject.GetComponentInChildren<Collider>(true);

        if (miniGameCanvasGroup == null && miniGameCanvasRoot != null)
            miniGameCanvasGroup = miniGameCanvasRoot.GetComponent<CanvasGroup>();

        for (int i = 0; i < MemoryCount; i++)
            ResolveMemoryReferences(memoryLights[i]);
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

        if (propsBarBeforeImage == null)
            propsBarBeforeImage = FindSceneGameObject(propsBarBeforeImageName);

        if (propsBarAfterImage == null)
            propsBarAfterImage = FindSceneGameObject(propsBarAfterImageName);
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

    private void ResolveMemoryReferences(MemoryLight memory)
    {
        if (memory == null)
            return;

        if (memory.LightTransform == null && memory.LightObject != null)
            memory.LightTransform = memory.LightObject.transform;
    }

    private Vector3 CreateRandomLightVelocity()
    {
        Vector3 direction = Random.insideUnitSphere;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.up;

        return direction.normalized * Mathf.Max(0.001f, lightFloatSpeed);
    }

    private float GetSphereColliderWorldRadius(SphereCollider sphere)
    {
        if (sphere == null)
            return 0f;

        Vector3 scale = sphere.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return sphere.radius * maxScale;
    }

    private bool PointerDown(out Vector2 position)
    {
        if (TryGetPrimaryPointer(out PointerInput pointer) && pointer.Down)
        {
            position = pointer.Position;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    private bool TryGetPrimaryPointer(out PointerInput pointer)
    {
        pointer = new PointerInput();

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            UnityEngine.InputSystem.Controls.TouchControl touch = Touchscreen.current.primaryTouch;
            pointer.Position = touch.position.ReadValue();
            pointer.Down = touch.press.wasPressedThisFrame;
            pointer.Held = touch.press.isPressed;
            pointer.Up = touch.press.wasReleasedThisFrame;

            if (pointer.Down || pointer.Held || pointer.Up)
                return true;
        }

        if (Mouse.current != null)
        {
            pointer.Position = Mouse.current.position.ReadValue();
            pointer.Down = Mouse.current.leftButton.wasPressedThisFrame;
            pointer.Held = Mouse.current.leftButton.isPressed;
            pointer.Up = Mouse.current.leftButton.wasReleasedThisFrame;

            if (pointer.Down || pointer.Held || pointer.Up)
                return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer.Position = touch.position;
            pointer.Down = touch.phase == TouchPhase.Began;
            pointer.Held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            pointer.Up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }

        pointer.Position = Input.mousePosition;
        pointer.Down = Input.GetMouseButtonDown(0);
        pointer.Held = Input.GetMouseButton(0);
        pointer.Up = Input.GetMouseButtonUp(0);

        if (pointer.Down || pointer.Held || pointer.Up)
            return true;
#endif

        return false;
    }

    private void StopRunningRoutines()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

    }

    private void SetDialogueVisible(bool visible)
    {
        SetObjectActive(butterflyDialoguePanel, visible);
    }

    private void SetHintText(string message, bool visible)
    {
        if (hintTypingRoutine != null)
        {
            StopCoroutine(hintTypingRoutine);
            hintTypingRoutine = null;
        }

        SetObjectActive(hintPanel, visible);

        if (hintText != null)
        {
            if (visible && !string.IsNullOrEmpty(message))
                hintTypingRoutine = StartCoroutine(TypeHintTextRoutine(message));
            else
                hintText.text = message;
        }
    }

    private IEnumerator TypeHintTextRoutine(string message)
    {
        yield return LwyTypewriterText.TypeText(hintText, message, hintTypingSeconds);
        hintTypingRoutine = null;
    }

    private void SetNetFingerHintVisible(bool visible)
    {
        if (!showNetFingerHintWhenWaiting)
            visible = false;

        SetObjectActive(netFingerHint, visible);
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

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
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

    private void LogDebug(string message)
    {
        if (logDebug)
            Debug.Log("MemoryFragmentInteractionController: " + message);
    }
}
