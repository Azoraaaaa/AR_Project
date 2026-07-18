using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Events;

public class FlowController2 : MonoBehaviour
{
    [System.Serializable]
    public class StoryLine
    {
        [TextArea(2, 5)]
        public string text;

        public AudioClip voiceClip;
    }

    private class FadeMaterialData
    {
        public Material material;

        public int colorPropertyID;
        public Color originalColor;

        public bool hasEmission;
        public int emissionPropertyID;
        public Color originalEmissionColor;
    }

    [Header("Story UI")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private AudioSource narrationAudioSource;

    [Header("Opening Story")]
    [SerializeField] private StoryLine[] openingLines;

    [Header("Ending Story")]
    [SerializeField] private StoryLine[] endingLines;

    [Header("Story Typewriter")]
    [Range(0.5f, 1f)]
    [SerializeField] private float revealDurationRatio = 0.9f;

    [SerializeField] private float fallbackCharactersPerSecond = 25f;
    [SerializeField] private float timeBetweenLines = 0.25f;

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [TextArea(2, 4)]
    [SerializeField]
    private string dragCloudHint =
        "Drag the clouds to find the hidden feelings.";

    [Header("Feeling Text UI")]
    [Tooltip("显示 I love you 等文字的独立 2D UI")]
    [SerializeField] private GameObject feelingTextPanel;

    [SerializeField] private TMP_Text feelingText;

    [Header("Clouds")]
    [SerializeField] private DraggableCloud[] clouds;

    [Header("Hidden Feeling Items")]
    [SerializeField] private HiddenFeelingItem[] feelingItems;

    [Header("Feeling Flight")]
    [Tooltip("物品最终飞到的目标点，放在 Lumi 身前")]
    [SerializeField] private Transform dogReceiveTarget;

    [SerializeField] private float itemClickToFlyDelay = 0.15f;
    [SerializeField] private float itemFlyDuration = 1.2f;
    [SerializeField] private float itemFlyArcHeight = 0.15f;
    [SerializeField] private float feelingTextExtraDuration = 0.5f;

    [Header("Lumi")]
    [SerializeField] private GameObject dogRoot;
    [SerializeField] private Animator dogAnimator;

    [Tooltip("Animator 中反应动作的 Trigger 参数")]
    [SerializeField] private string reactionTrigger = "React";

    [Tooltip("反应动画的大约时长")]
    [SerializeField] private float dogReactionDuration = 1.2f;

    [SerializeField] private AudioSource dogAudioSource;
    [SerializeField] private AudioClip dogBarkClip;

    [Range(0f, 1f)]
    [SerializeField] private float dogBarkVolume = 1f;

    [Header("Heart Effect")]
    [SerializeField] private GameObject heartEffectRoot;
    [SerializeField] private float heartEffectDuration = 1.2f;

    [Header("SFX")]
    [Tooltip("必须挂在不会消失的 Manager 对象上")]
    [SerializeField] private AudioSource sfxAudioSource;

    [SerializeField] private AudioClip cloudGrabClip;
    [SerializeField] private AudioClip itemClickClip;
    [SerializeField] private AudioClip itemFlyClip;
    [SerializeField] private AudioClip goodbyeClickClip;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("After All Feelings")]
    [SerializeField] private float delayBeforeEndingStory = 0.6f;

    [Header("Goodbye Button")]
    [SerializeField] private GameObject goodbyeButtonRoot;
    [SerializeField] private Button goodbyeButton;

    [Header("Final Dog Reaction")]
    [Tooltip("点击 Goodbye 后，等待反应动画多久再开始渐隐")]
    [SerializeField] private float goodbyeReactionDuration = 1.3f;

    [System.Serializable]
    public class DogFadeRendererSetup
    {
        [Tooltip("小狗身上的 Renderer")]
        public Renderer dogRenderer;

        [Tooltip("透明材质，数量必须与 Renderer 的 Material Slots 一致")]
        public Material[] transparentMaterials;
    }

    [Header("Dog Fade")]
    [SerializeField]
    private DogFadeRendererSetup[] dogFadeSetups;

    [SerializeField] private float dogFadeDuration = 2f;

    [Header("Dog Disappear SFX")]
    [Tooltip("小狗开始渐隐时播放")]
    [SerializeField] private AudioClip dogDisappearClip;

    [Range(0f, 1f)]
    [SerializeField] private float dogDisappearVolume = 1f;

    [Header("Final Light Point")]
    [Tooltip("必须放在 DogB 外面，否则关闭 DogB 后光点也会消失")]
    [SerializeField] private GameObject lightPointRoot;

    [Tooltip("决定光点往哪个方向飞。建议拖入 Page11Prefab 或场景的竖直方向参考物体")]
    [SerializeField] private Transform lightPointFlyDirectionReference;

    [Tooltip("光点出现后，停留多久才开始飞走")]
    [SerializeField] private float lightPointHoldDuration = 0.5f;

    [Tooltip("光点向上飞行的时间")]
    [SerializeField] private float lightPointFlyDuration = 1.5f;

    [Tooltip("光点向上飞行的距离")]
    [SerializeField] private float lightPointFlyDistance = 0.5f;

    [Tooltip("飞行速度曲线")]
    [SerializeField]
    private AnimationCurve lightPointFlyCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("飞行时是否同时逐渐缩小")]
    [SerializeField] private bool shrinkWhileFlying = true;

    [Range(0f, 1f)]
    [Tooltip("飞行结束时保留的缩放比例，0 代表完全缩小")]
    [SerializeField] private float lightPointFinalScaleMultiplier = 0.15f;

    [Header("Page Complete")]
    [Tooltip("光点飞走并消失后触发，可用于连接下一页")]
    [SerializeField] private UnityEvent onPageCompleted;

    private readonly HashSet<HiddenFeelingItem> collectedItems =
        new HashSet<HiddenFeelingItem>();

    private readonly List<FadeMaterialData> dogFadeMaterials =
        new List<FadeMaterialData>();

    private ParticleSystem[] heartParticles;
    private ParticleSystem[] lightPointParticles;

    private bool pageStarted;
    private bool interactionsUnlocked;
    private bool cloudHintDismissed;
    private bool itemBusy;
    private bool endingStarted;
    private bool goodbyeAvailable;
    private bool pageEnded;
    private Vector3 lightPointInitialLocalPosition;
    private Vector3 lightPointInitialLocalScale;
    private bool lightPointTransformCached;

    private int requiredFeelingCount;

    public bool CanDragCloud
    {
        get
        {
            return interactionsUnlocked &&
                   !endingStarted &&
                   !pageEnded;
        }
    }

    public bool CanSelectFeelingItem
    {
        get
        {
            return interactionsUnlocked &&
                   !itemBusy &&
                   !endingStarted &&
                   !pageEnded;
        }
    }

    public bool GoodbyeAvailable => goodbyeAvailable;

    private void Awake()
    {
        CacheLightPointInitialTransform();
        PrepareInitialState();

        if (goodbyeButton != null)
        {
            goodbyeButton.onClick.AddListener(
                OnGoodbyeButtonClicked
            );
        }
    }
    private void CacheLightPointInitialTransform()
    {
        if (lightPointRoot == null)
            return;

        lightPointInitialLocalPosition =
            lightPointRoot.transform.localPosition;

        lightPointInitialLocalScale =
            lightPointRoot.transform.localScale;

        lightPointTransformCached = true;
    }
    private bool ApplyTransparentDogMaterials()
    {
        dogFadeMaterials.Clear();

        if (dogFadeSetups == null ||
            dogFadeSetups.Length == 0)
        {
            Debug.LogError(
                "FlowController2: Dog Fade Setups are empty."
            );

            return false;
        }

        int validRendererCount = 0;

        foreach (DogFadeRendererSetup setup
                 in dogFadeSetups)
        {
            if (setup == null ||
                setup.dogRenderer == null)
            {
                Debug.LogWarning(
                    "FlowController2: " +
                    "A Dog Fade Setup has no Renderer."
                );

                continue;
            }

            int materialSlotCount =
                setup.dogRenderer.sharedMaterials.Length;

            if (materialSlotCount == 0)
            {
                Debug.LogWarning(
                    $"FlowController2: " +
                    $"{setup.dogRenderer.name} has no material slots."
                );

                continue;
            }

            if (setup.transparentMaterials == null ||
                setup.transparentMaterials.Length !=
                materialSlotCount)
            {
                Debug.LogError(
                    $"FlowController2: " +
                    $"{setup.dogRenderer.name} has " +
                    $"{materialSlotCount} material slots, but " +
                    $"{setup.transparentMaterials?.Length ?? 0} " +
                    "transparent materials were assigned."
                );

                continue;
            }

            /*
             * 先检查这一组有没有空材质。
             * 避免只替换一半材质。
             */
            bool setupIsValid = true;

            for (int i = 0;
                 i < materialSlotCount;
                 i++)
            {
                if (setup.transparentMaterials[i] == null)
                {
                    Debug.LogError(
                        $"FlowController2: " +
                        $"Transparent material {i} on " +
                        $"{setup.dogRenderer.name} is missing."
                    );

                    setupIsValid = false;
                }
            }

            if (!setupIsValid)
                continue;

            Material[] runtimeMaterials =
                new Material[materialSlotCount];

            List<FadeMaterialData> currentRendererFadeData =
                new List<FadeMaterialData>();

            bool runtimeSetupValid = true;

            for (int i = 0;
                 i < materialSlotCount;
                 i++)
            {
                Material sourceMaterial =
                    setup.transparentMaterials[i];

                /*
                 * 创建运行时材质实例，
                 * 避免改变 Project 中的原材质。
                 */
                Material runtimeMaterial =
                    new Material(sourceMaterial);

                runtimeMaterial.name =
                    sourceMaterial.name +
                    " Runtime Fade";

                /*
                 * 强制 Standard / URP Lit
                 * 使用透明模式。
                 */
                PrepareRuntimeTransparentMaterial(
                    runtimeMaterial
                );

                runtimeMaterials[i] =
                    runtimeMaterial;

                int colorPropertyID;

                if (runtimeMaterial.HasProperty(
                        "_BaseColor"))
                {
                    colorPropertyID =
                        Shader.PropertyToID(
                            "_BaseColor"
                        );
                }
                else if (runtimeMaterial.HasProperty(
                             "_Color"))
                {
                    colorPropertyID =
                        Shader.PropertyToID(
                            "_Color"
                        );
                }
                else
                {
                    Debug.LogError(
                        $"FlowController2: " +
                        $"{runtimeMaterial.name} has no " +
                        "_BaseColor or _Color property."
                    );

                    Destroy(runtimeMaterial);
                    runtimeMaterials[i] = null;
                    runtimeSetupValid = false;

                    continue;
                }

                Color startColor =
                    runtimeMaterial.GetColor(
                        colorPropertyID
                    );

                startColor.a = 1f;

                runtimeMaterial.SetColor(
                    colorPropertyID,
                    startColor
                );

                FadeMaterialData fadeData =
                    new FadeMaterialData
                    {
                        material =
                            runtimeMaterial,

                        colorPropertyID =
                            colorPropertyID,

                        originalColor =
                            startColor,

                        hasEmission =
                            false,

                        emissionPropertyID =
                            -1,

                        originalEmissionColor =
                            Color.black
                    };

                /*
                 * 天使光圈的材质有 Emission。
                 * 缓存原始发光颜色，
                 * 后续渐隐时一起降低。
                 */
                if (runtimeMaterial.HasProperty(
                        "_EmissionColor"))
                {
                    fadeData.hasEmission = true;

                    fadeData.emissionPropertyID =
                        Shader.PropertyToID(
                            "_EmissionColor"
                        );

                    fadeData.originalEmissionColor =
                        runtimeMaterial.GetColor(
                            fadeData.emissionPropertyID
                        );

                    runtimeMaterial.EnableKeyword(
                        "_EMISSION"
                    );
                }

                currentRendererFadeData.Add(
                    fadeData
                );
            }

            /*
             * 当前 Renderer 有材质不兼容时，
             * 不替换这一整组。
             */
            if (!runtimeSetupValid)
            {
                foreach (Material material
                         in runtimeMaterials)
                {
                    if (material != null)
                    {
                        Destroy(material);
                    }
                }

                continue;
            }

            /*
             * 正式将透明材质换到：
             * Dog / Feather / Torus.001。
             */
            setup.dogRenderer.materials =
                runtimeMaterials;

            dogFadeMaterials.AddRange(
                currentRendererFadeData
            );

            validRendererCount++;
        }

        Debug.Log(
            $"FlowController2: Prepared " +
            $"{dogFadeMaterials.Count} transparent materials " +
            $"across {validRendererCount} renderers."
        );

        return validRendererCount > 0 &&
               dogFadeMaterials.Count > 0;
    }
    private void PrepareRuntimeTransparentMaterial(
    Material material)
    {
        if (material == null)
            return;

        /*
         * Built-in Standard Shader
         * Rendering Mode = Fade
         */
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat(
                "_Mode",
                2f
            );

            material.SetInt(
                "_SrcBlend",
                (int)BlendMode.SrcAlpha
            );

            material.SetInt(
                "_DstBlend",
                (int)BlendMode.OneMinusSrcAlpha
            );

            material.SetInt(
                "_ZWrite",
                0
            );

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.EnableKeyword(
                "_ALPHABLEND_ON"
            );

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON"
            );

            material.SetOverrideTag(
                "RenderType",
                "Transparent"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;

            return;
        }

        /*
         * URP Lit Shader
         * Surface Type = Transparent
         */
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat(
                    "_Blend",
                    0f
                );
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt(
                    "_SrcBlend",
                    (int)BlendMode.SrcAlpha
                );
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt(
                    "_DstBlend",
                    (int)BlendMode.OneMinusSrcAlpha
                );
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt(
                    "_ZWrite",
                    0
                );
            }

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.SetOverrideTag(
                "RenderType",
                "Transparent"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }
    }

    private void Start()
    {
        if (!pageStarted)
        {
            pageStarted = true;
            StartCoroutine(PageOpeningRoutine());
        }
    }

    private void OnDestroy()
    {
        if (goodbyeButton != null)
        {
            goodbyeButton.onClick.RemoveListener(
                OnGoodbyeButtonClicked
            );
        }
    }

    private void PrepareInitialState()
    {
        interactionsUnlocked = false;
        cloudHintDismissed = false;
        itemBusy = false;
        endingStarted = false;
        goodbyeAvailable = false;
        pageEnded = false;

        collectedItems.Clear();

        HideStory();
        HideHint();
        HideFeelingText();

        if (goodbyeButtonRoot != null)
            goodbyeButtonRoot.SetActive(false);

        if (goodbyeButton != null)
            goodbyeButton.interactable = false;

        if (lightPointRoot != null)
        {
            if (!lightPointTransformCached)
            {
                CacheLightPointInitialTransform();
            }

            lightPointRoot.transform.localPosition =
                lightPointInitialLocalPosition;

            lightPointRoot.transform.localScale =
                lightPointInitialLocalScale;

            lightPointRoot.SetActive(false);
        }

        if (dogRoot != null)
            dogRoot.SetActive(true);

        SetHeartEffect(false);

        requiredFeelingCount = 0;

        if (feelingItems != null)
        {
            foreach (HiddenFeelingItem item in feelingItems)
            {
                if (item == null)
                    continue;

                requiredFeelingCount++;
                item.SetInteractable(false);
            }
        }

        if (clouds != null)
        {
            foreach (DraggableCloud cloud in clouds)
            {
                if (cloud != null)
                    cloud.SetInteractable(false);
            }
        }

        if (heartEffectRoot != null)
        {
            heartParticles =
                heartEffectRoot.GetComponentsInChildren
                <ParticleSystem>(true);
        }

        if (lightPointRoot != null)
        {
            lightPointParticles =
                lightPointRoot.GetComponentsInChildren
                <ParticleSystem>(true);
        }
    }

    private IEnumerator PageOpeningRoutine()
    {
        yield return null;

        yield return StartCoroutine(
            PlayStoryLines(openingLines)
        );

        HideStory();

        interactionsUnlocked = true;

        SetCloudInteraction(true);
        SetFeelingInteraction(true);

        ShowHint(dragCloudHint);
    }

    // =========================================================
    // Story UI
    // =========================================================

    private IEnumerator PlayStoryLines(StoryLine[] lines)
    {
        if (storyPanel == null || storyText == null)
        {
            Debug.LogError(
                "FlowController2: StoryPanel or StoryText is missing."
            );

            yield break;
        }

        storyPanel.SetActive(true);
        storyText.gameObject.SetActive(true);

        if (lines == null)
            yield break;

        foreach (StoryLine line in lines)
        {
            if (line == null)
                continue;

            storyText.text = line.text;
            storyText.ForceMeshUpdate();

            int characterCount =
                storyText.textInfo.characterCount;

            storyText.maxVisibleCharacters = 0;

            float totalDuration;

            if (line.voiceClip != null)
            {
                totalDuration = line.voiceClip.length;

                if (narrationAudioSource != null)
                {
                    narrationAudioSource.Stop();
                    narrationAudioSource.clip =
                        line.voiceClip;
                    narrationAudioSource.Play();
                }
                else
                {
                    Debug.LogWarning(
                        "FlowController2: " +
                        "Narration AudioSource is missing."
                    );
                }
            }
            else
            {
                totalDuration =
                    characterCount /
                    Mathf.Max(
                        1f,
                        fallbackCharactersPerSecond
                    );

                totalDuration =
                    Mathf.Max(0.8f, totalDuration);
            }

            float revealDuration =
                Mathf.Max(
                    0.05f,
                    totalDuration *
                    revealDurationRatio
                );

            float elapsed = 0f;

            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / revealDuration
                    );

                storyText.maxVisibleCharacters =
                    Mathf.FloorToInt(
                        characterCount * progress
                    );

                yield return null;
            }

            storyText.maxVisibleCharacters =
                int.MaxValue;

            if (line.voiceClip != null &&
                narrationAudioSource != null)
            {
                while (narrationAudioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                float remaining =
                    totalDuration - revealDuration;

                if (remaining > 0f)
                {
                    yield return
                        new WaitForSecondsRealtime(
                            remaining
                        );
                }
            }

            if (timeBetweenLines > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        timeBetweenLines
                    );
            }
        }
    }

    private void ShowStory()
    {
        HideHint();

        if (storyPanel != null)
            storyPanel.SetActive(true);
    }

    private void HideStory()
    {
        if (storyPanel != null)
            storyPanel.SetActive(false);
    }

    // =========================================================
    // Hint UI
    // =========================================================

    private void ShowHint(string message)
    {
        HideStory();

        if (hintPanel != null)
            hintPanel.SetActive(true);

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = message;
            hintText.maxVisibleCharacters =
                int.MaxValue;
        }
    }

    private void HideHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    // =========================================================
    // Clouds
    // =========================================================

    public void OnCloudGrabbed()
    {
        if (!CanDragCloud)
            return;

        PlaySFX(cloudGrabClip);

        if (!cloudHintDismissed)
        {
            cloudHintDismissed = true;
            HideHint();
        }
    }

    public void OnCloudReleased()
    {
        if (!pageEnded)
            PlaySFX(cloudGrabClip);
    }

    private void SetCloudInteraction(bool value)
    {
        if (clouds == null)
            return;

        foreach (DraggableCloud cloud in clouds)
        {
            if (cloud != null)
                cloud.SetInteractable(value);
        }
    }

    // =========================================================
    // Hidden feelings
    // =========================================================

    public bool TrySelectFeelingItem(
        HiddenFeelingItem item)
    {
        if (!CanSelectFeelingItem ||
            item == null ||
            collectedItems.Contains(item))
        {
            return false;
        }

        itemBusy = true;
        item.SetInteractable(false);

        StartCoroutine(
            CollectFeelingRoutine(item)
        );

        return true;
    }

    private IEnumerator CollectFeelingRoutine(
        HiddenFeelingItem item)
    {
        PlaySFX(itemClickClip);

        ShowFeelingText(item.DisplayText);

        if (itemClickToFlyDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    itemClickToFlyDelay
                );
        }

        PlaySFX(itemFlyClip);

        yield return StartCoroutine(
            FlyItemToDog(item.transform)
        );

        item.gameObject.SetActive(false);

        TriggerDogReaction(true);

        float firstWait =
            Mathf.Min(
                dogReactionDuration,
                heartEffectDuration
            );

        if (firstWait > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    firstWait
                );
        }

        if (heartEffectDuration <=
            dogReactionDuration)
        {
            SetHeartEffect(false);

            float remaining =
                dogReactionDuration -
                heartEffectDuration;

            if (remaining > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        remaining
                    );
            }
        }
        else
        {
            float remaining =
                heartEffectDuration -
                dogReactionDuration;

            if (remaining > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        remaining
                    );
            }

            SetHeartEffect(false);
        }

        if (feelingTextExtraDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    feelingTextExtraDuration
                );
        }

        HideFeelingText();

        collectedItems.Add(item);
        itemBusy = false;

        if (collectedItems.Count >=
            requiredFeelingCount)
        {
            StartCoroutine(
                AfterAllFeelingsRoutine()
            );
        }
    }

    private IEnumerator FlyItemToDog(
        Transform itemTransform)
    {
        if (itemTransform == null)
            yield break;

        Vector3 startPosition =
            itemTransform.position;

        Vector3 endPosition =
            dogReceiveTarget != null
                ? dogReceiveTarget.position
                : dogRoot != null
                    ? dogRoot.transform.position
                    : startPosition;

        float elapsed = 0f;
        float duration =
            Mathf.Max(0.05f, itemFlyDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            Vector3 position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    progress
                );

            float arc =
                4f *
                progress *
                (1f - progress) *
                itemFlyArcHeight;

            position += transform.up * arc;

            itemTransform.position = position;

            yield return null;
        }

        itemTransform.position = endPosition;
    }

    private void SetFeelingInteraction(bool value)
    {
        if (feelingItems == null)
            return;

        foreach (HiddenFeelingItem item in feelingItems)
        {
            if (item != null &&
                !collectedItems.Contains(item))
            {
                item.SetInteractable(value);
            }
        }
    }

    private void ShowFeelingText(string message)
    {
        if (feelingTextPanel != null)
            feelingTextPanel.SetActive(true);

        if (feelingText != null)
        {
            feelingText.gameObject.SetActive(true);
            feelingText.text = message;
            feelingText.maxVisibleCharacters =
                int.MaxValue;
        }
    }

    private void HideFeelingText()
    {
        if (feelingTextPanel != null)
            feelingTextPanel.SetActive(false);
    }

    // =========================================================
    // Dog
    // =========================================================

    private void TriggerDogReaction(bool showHeart)
    {
        if (dogAnimator != null &&
            !string.IsNullOrEmpty(reactionTrigger))
        {
            dogAnimator.ResetTrigger(
                reactionTrigger
            );

            dogAnimator.SetTrigger(
                reactionTrigger
            );
        }

        if (dogAudioSource != null &&
            dogBarkClip != null)
        {
            dogAudioSource.PlayOneShot(
                dogBarkClip,
                dogBarkVolume
            );
        }

        if (showHeart)
            SetHeartEffect(true);
    }

    private void SetHeartEffect(bool visible)
    {
        if (heartEffectRoot == null)
            return;

        if (visible)
        {
            heartEffectRoot.SetActive(true);

            if (heartParticles == null)
            {
                heartParticles =
                    heartEffectRoot
                    .GetComponentsInChildren
                    <ParticleSystem>(true);
            }

            foreach (ParticleSystem particle
                     in heartParticles)
            {
                particle.Clear(true);
                particle.Play(true);
            }
        }
        else
        {
            if (heartParticles != null)
            {
                foreach (ParticleSystem particle
                         in heartParticles)
                {
                    particle.Stop(
                        true,
                        ParticleSystemStopBehavior
                            .StopEmittingAndClear
                    );
                }
            }

            heartEffectRoot.SetActive(false);
        }
    }

    // =========================================================
    // All feelings found
    // =========================================================

    private IEnumerator AfterAllFeelingsRoutine()
    {
        if (endingStarted)
            yield break;

        endingStarted = true;
        interactionsUnlocked = false;

        SetCloudInteraction(false);
        SetFeelingInteraction(false);

        HideHint();
        HideFeelingText();

        if (delayBeforeEndingStory > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    delayBeforeEndingStory
                );
        }

        ShowStory();

        yield return StartCoroutine(
            PlayStoryLines(endingLines)
        );

        HideStory();

        goodbyeAvailable = true;

        if (goodbyeButtonRoot != null)
            goodbyeButtonRoot.SetActive(true);

        if (goodbyeButton != null)
            goodbyeButton.interactable = true;
    }

    // =========================================================
    // Goodbye
    // =========================================================

    public void OnGoodbyeButtonClicked()
    {
        if (!goodbyeAvailable ||
            pageEnded)
        {
            return;
        }

        goodbyeAvailable = false;

        if (goodbyeButton != null)
            goodbyeButton.interactable = false;

        StartCoroutine(GoodbyeRoutine());
    }

    private IEnumerator GoodbyeRoutine()
    {
        PlaySFX(goodbyeClickClip);

        if (goodbyeButtonRoot != null)
        {
            goodbyeButtonRoot.SetActive(false);
        }

        // 小狗先做反应动作并叫一声
        TriggerDogReaction(false);

        if (goodbyeReactionDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                goodbyeReactionDuration
            );
        }

        /*
         * 小狗开始渐隐时，
         * 同一时刻播放消失音效。
         */
        if (sfxAudioSource != null &&
            dogDisappearClip != null)
        {
            sfxAudioSource.PlayOneShot(
                dogDisappearClip,
                dogDisappearVolume
            );
        }

        yield return StartCoroutine(
            FadeDogRoutine()
        );

        if (dogRoot != null)
        {
            dogRoot.SetActive(false);
        }

        /*
         * 小狗完全消失后：
         * 光点出现 → 停留 → 向上飞 → 消失。
         */
        yield return StartCoroutine(
            LightPointFlyAwayRoutine()
        );

        pageEnded = true;

        /*
         * Page 11 完成。
         * 可在 Inspector 连接下一页流程。
         */
        onPageCompleted?.Invoke();
    }

    // =========================================================
    // Dog fade
    // =========================================================

    private IEnumerator FadeDogRoutine()
    {
        /*
         * 渐隐开始时才把 UnityChan/Skin
         * 换成准备好的透明材质。
         */
        bool materialsReady =
            ApplyTransparentDogMaterials();

        if (!materialsReady)
        {
            Debug.LogWarning(
                "FlowController2: Dog fade materials were not ready. " +
                "The dog will disappear without a smooth fade."
            );

            yield return new WaitForSecondsRealtime(
                dogFadeDuration
            );

            yield break;
        }

        float duration =
            Mathf.Max(
                0.05f,
                dogFadeDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float alpha =
                Mathf.SmoothStep(
                    1f,
                    0f,
                    progress
                );

            foreach (FadeMaterialData data
         in dogFadeMaterials)
            {
                if (data.material == null)
                    continue;

                Color currentColor =
                    data.originalColor;

                currentColor.a =
                    data.originalColor.a * alpha;

                data.material.SetColor(
                    data.colorPropertyID,
                    currentColor
                );

                /*
                 * 光圈有 Emission，
                 * 同时降低发光强度。
                 */
                if (data.hasEmission)
                {
                    Color emissionColor =
                        data.originalEmissionColor * alpha;

                    data.material.SetColor(
                        data.emissionPropertyID,
                        emissionColor
                    );
                }
            }

            yield return null;
        }

        foreach (FadeMaterialData data
         in dogFadeMaterials)
        {
            if (data.material == null)
                continue;

            Color finalColor =
                data.originalColor;

            finalColor.a = 0f;

            data.material.SetColor(
                data.colorPropertyID,
                finalColor
            );

            if (data.hasEmission)
            {
                data.material.SetColor(
                    data.emissionPropertyID,
                    Color.black
                );
            }
        }
    }

    private void ShowLightPoint()
    {
        if (lightPointRoot == null)
        {
            Debug.LogError(
                "FlowController2: Light Point Root is missing."
            );

            return;
        }

        if (!lightPointTransformCached)
        {
            CacheLightPointInitialTransform();
        }

        lightPointRoot.transform.localPosition =
            lightPointInitialLocalPosition;

        lightPointRoot.transform.localScale =
            lightPointInitialLocalScale;

        lightPointRoot.SetActive(true);

        if (lightPointParticles == null ||
            lightPointParticles.Length == 0)
        {
            lightPointParticles =
                lightPointRoot
                    .GetComponentsInChildren
                    <ParticleSystem>(true);
        }

        foreach (ParticleSystem particle
                 in lightPointParticles)
        {
            if (particle == null)
                continue;

            particle.Clear(true);
            particle.Play(true);
        }
    }
    private IEnumerator LightPointFlyAwayRoutine()
    {
        if (lightPointRoot == null)
        {
            Debug.LogError(
                "FlowController2: Light Point Root is missing."
            );

            yield break;
        }

        ShowLightPoint();

        /*
         * 光点先停留一下，
         * 让玩家看到 Lumi 变成了光。
         */
        if (lightPointHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                lightPointHoldDuration
            );
        }

        Transform lightTransform =
            lightPointRoot.transform;

        Vector3 startLocalPosition =
            lightTransform.localPosition;

        Vector3 startLocalScale =
            lightTransform.localScale;

        /*
         * 默认使用光点自身父对象的上方向。
         * 也可以在 Inspector 指定方向参考物体。
         */
        Vector3 worldFlyDirection;

        if (lightPointFlyDirectionReference != null)
        {
            worldFlyDirection =
                lightPointFlyDirectionReference.up;
        }
        else if (lightTransform.parent != null)
        {
            worldFlyDirection =
                lightTransform.parent.up;
        }
        else
        {
            worldFlyDirection =
                Vector3.up;
        }

        worldFlyDirection.Normalize();

        Vector3 localFlyDirection;

        if (lightTransform.parent != null)
        {
            localFlyDirection =
                lightTransform.parent
                    .InverseTransformDirection(
                        worldFlyDirection
                    )
                    .normalized;
        }
        else
        {
            localFlyDirection =
                worldFlyDirection;
        }

        Vector3 endLocalPosition =
            startLocalPosition +
            localFlyDirection *
            lightPointFlyDistance;

        Vector3 endLocalScale =
            startLocalScale *
            lightPointFinalScaleMultiplier;

        float duration =
            Mathf.Max(
                0.05f,
                lightPointFlyDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float curveValue =
                lightPointFlyCurve != null
                    ? lightPointFlyCurve
                        .Evaluate(normalizedTime)
                    : normalizedTime;

            lightTransform.localPosition =
                Vector3.LerpUnclamped(
                    startLocalPosition,
                    endLocalPosition,
                    curveValue
                );

            if (shrinkWhileFlying)
            {
                lightTransform.localScale =
                    Vector3.LerpUnclamped(
                        startLocalScale,
                        endLocalScale,
                        curveValue
                    );
            }

            yield return null;
        }

        lightTransform.localPosition =
            endLocalPosition;

        if (shrinkWhileFlying)
        {
            lightTransform.localScale =
                endLocalScale;
        }

        StopLightPointParticles();

        lightPointRoot.SetActive(false);

        if (SimpleCloudRecoEventHandler.Instance != null)
        {
            SimpleCloudRecoEventHandler.Instance.ShowNextPageCanvas();
        }
    }
    private void StopLightPointParticles()
    {
        if (lightPointParticles == null)
            return;

        foreach (ParticleSystem particle
                 in lightPointParticles)
        {
            if (particle == null)
                continue;

            particle.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );
        }
    }
    // =========================================================
    // Audio
    // =========================================================

    private void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource != null &&
            clip != null)
        {
            sfxAudioSource.PlayOneShot(
                clip,
                sfxVolume
            );
        }
    }
}