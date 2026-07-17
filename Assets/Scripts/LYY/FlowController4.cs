using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FlowController4 : MonoBehaviour
{
    [System.Serializable]
    public class StoryLine
    {
        [TextArea(2, 5)]
        public string text;

        public AudioClip voiceClip;
    }

    // =========================================================
    // Narration UI
    // =========================================================

    [Header("Narration UI")]
    [Tooltip("可以拖入 Hint Panel 或 Story Panel")]
    [SerializeField] private GameObject narrationPanel;

    [SerializeField] private TMP_Text narrationText;

    [Tooltip("必须放在不会被隐藏的 Managers 对象上")]
    [SerializeField] private AudioSource narrationAudioSource;

    [Header("Typewriter")]
    [Range(0.5f, 1f)]
    [SerializeField] private float revealDurationRatio = 0.9f;

    [SerializeField] private float fallbackCharactersPerSecond = 25f;

    [Tooltip("两句旁白之间的停顿")]
    [SerializeField] private float timeBetweenLines = 0.3f;

    // =========================================================
    // Story lines
    // =========================================================

    [Header("Opening Narration")]
    [SerializeField] private StoryLine[] openingLines;

    [Header("Middle Narration")]
    [SerializeField] private StoryLine[] middleLines;

    [Header("Final Narration")]
    [SerializeField] private StoryLine[] finalLines;

    // =========================================================
    // Plant stages
    // =========================================================

    [Header("Plant Models")]
    [Tooltip("Page 12 结尾留下的发芽种子")]
    [SerializeField] private GameObject sproutModel;

    [Tooltip("第一次时间流逝后出现的加强发芽模型")]
    [SerializeField] private GameObject sproutPlusModel;

    [Tooltip("第二次时间流逝后出现的开花模型")]
    [SerializeField] private GameObject flowerModel;

    [Tooltip("模型切换前短暂停顿")]
    [SerializeField] private float beforeModelSwitchDelay = 0.15f;

    [Tooltip("模型切换后短暂停顿")]
    [SerializeField] private float afterModelSwitchDelay = 0.3f;

    [Header("Plant Change Effect")]
    [Tooltip("种子每次变化时重复使用的同一个特效")]
    [SerializeField] private GameObject plantChangeEffectRoot;

    [Tooltip("特效出现后，多久切换植物模型")]
    [SerializeField] private float plantModelSwitchTime = 0.35f;

    [Tooltip("每次变化特效的总持续时间")]
    [SerializeField] private float plantChangeEffectDuration = 1.2f;

    [Tooltip("播放种子变化音效的 AudioSource")]
    [SerializeField] private AudioSource plantChangeAudioSource;

    [Tooltip("两次模型变化重复使用的同一个音效")]
    [SerializeField] private AudioClip plantChangeClip;

    [Range(0f, 1f)]
    [SerializeField] private float plantChangeVolume = 1f;

    // =========================================================
    // Directional light
    // =========================================================

    [Header("Time Passage - Directional Light")]
    [Tooltip("建议使用 Page13Prefab 内部的 Directional Light")]
    [SerializeField] private Light directionalLight;

    [Tooltip("白天方向参考空物体，可留空使用下面的 Euler")]
    [SerializeField] private Transform dayLightAnchor;

    [Tooltip("夜晚方向参考空物体，可留空使用下面的 Euler")]
    [SerializeField] private Transform nightLightAnchor;

    [SerializeField]
    private Vector3 fallbackDayRotation =
        new Vector3(45f, -30f, 0f);

    [SerializeField]
    private Vector3 fallbackNightRotation =
        new Vector3(-20f, 150f, 0f);

    [Header("Day Light")]
    [SerializeField] private Color dayLightColor = Color.white;

    [SerializeField] private float dayLightIntensity = 1f;

    [Header("Night Light")]
    [SerializeField]
    private Color nightLightColor =
        new Color(0.28f, 0.36f, 0.65f, 1f);

    [SerializeField] private float nightLightIntensity = 0.2f;

    [Header("Time Passage Timing")]
    [Tooltip("白天变夜晚所需时间")]
    [SerializeField] private float dayToNightDuration = 1.8f;

    [Tooltip("夜晚保持时间")]
    [SerializeField] private float nightHoldDuration = 0.6f;

    [Tooltip("夜晚变回白天所需时间")]
    [SerializeField] private float nightToDayDuration = 1.8f;

    [Tooltip("回到白天后的停顿")]
    [SerializeField] private float dayHoldDuration = 0.4f;

    [SerializeField]
    private AnimationCurve lightTransitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // =========================================================
    // Optional ambient light
    // =========================================================

    [Header("Optional Ambient Light")]
    [Tooltip("同时改变环境光，使昼夜变化更明显")]
    [SerializeField] private bool controlAmbientLight = true;

    [SerializeField]
    private Color dayAmbientColor =
        new Color(0.65f, 0.65f, 0.65f, 1f);

    [SerializeField]
    private Color nightAmbientColor =
        new Color(0.08f, 0.1f, 0.2f, 1f);

    // =========================================================
    // Time passage SFX
    // =========================================================

    [Header("Time Passage SFX")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Tooltip("每次白天—夜晚—白天时播放，可留空")]
    [SerializeField] private AudioClip timePassageClip;

    [Range(0f, 1f)]
    [SerializeField] private float timePassageVolume = 1f;

    // =========================================================
    // AR start
    // =========================================================

    [Header("AR Start")]
    [Tooltip("扫描出现后等待场景稳定")]
    [SerializeField] private float pageAppearDelay = 0.5f;

    // =========================================================
    // The End
    // =========================================================

    [Header("Ending Black Screen")]
    [Tooltip("全屏黑色 UI Image")]
    [SerializeField] private Image blackOverlayImage;

    [Tooltip("直接在这个 TMP Text 组件中写好 THE END")]
    [SerializeField] private TMP_Text theEndText;

    [Tooltip("画面逐渐变黑所需时间")]
    [SerializeField] private float blackFadeDuration = 1.5f;

    [Tooltip("完全黑屏后，等待多久出现 THE END")]
    [SerializeField] private float beforeTheEndDelay = 0.4f;

    [Tooltip("THE END 文字淡入所需时间")]
    [SerializeField] private float theEndFadeDuration = 1f;

    [Tooltip("THE END 出现后保持多久再触发完成事件")]
    [SerializeField] private float theEndHoldDuration = 1f;

    [Header("Page Complete")]
    [SerializeField] private UnityEvent onPageCompleted;

    private bool pageStarted;
    private ParticleSystem[] plantChangeParticles;

    // =========================================================
    // Unity lifecycle
    // =========================================================

    private void Awake()
    {
        CachePlantChangeParticles();
        PrepareInitialState();
    }
    private void CachePlantChangeParticles()
    {
        if (plantChangeEffectRoot == null)
            return;

        plantChangeParticles =
            plantChangeEffectRoot
                .GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Start()
    {
        if (pageStarted)
            return;

        pageStarted = true;

        StartCoroutine(Page13Routine());
    }

    // =========================================================
    // Initial state
    // =========================================================

    private void PrepareInitialState()
    {
        HideNarration();

        SetPlantStage(0);

        SetImmediateDayLight();

        if (plantChangeEffectRoot != null)
        {
            StopPlantChangeEffect();
            plantChangeEffectRoot.SetActive(false);
        }

        if (blackOverlayImage != null)
        {
            blackOverlayImage.gameObject.SetActive(true);

            Color blackColor =
                blackOverlayImage.color;

            blackColor.a = 0f;

            blackOverlayImage.color =
                blackColor;

            blackOverlayImage.raycastTarget = false;
        }

        if (theEndText != null)
        {
            theEndText.gameObject.SetActive(true);

            Color textColor =
                theEndText.color;

            textColor.a = 0f;

            theEndText.color =
                textColor;

            theEndText.maxVisibleCharacters =
                int.MaxValue;
        }
    }

    // =========================================================
    // Complete page flow
    // =========================================================

    private IEnumerator Page13Routine()
    {
        if (pageAppearDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                pageAppearDelay
            );
        }

        // 第一段旁白
        yield return StartCoroutine(
            PlayStoryLines(openingLines)
        );

        HideNarration();

        // 第一次时间流逝
        yield return StartCoroutine(
            DayNightDayRoutine()
        );

        // 发芽种子变成 Sprout Plus
        yield return StartCoroutine(
            ChangePlantStageRoutine(1)
        );

        // 中段旁白
        yield return StartCoroutine(
            PlayStoryLines(middleLines)
        );

        HideNarration();

        // 第二次时间流逝
        yield return StartCoroutine(
            DayNightDayRoutine()
        );

        // Sprout Plus 变成花
        yield return StartCoroutine(
            ChangePlantStageRoutine(2)
        );

        // 最终旁白
        yield return StartCoroutine(
            PlayStoryLines(finalLines)
        );

        HideNarration();

        // 黑屏和 The End
        yield return StartCoroutine(
            ShowTheEndRoutine()
        );

        onPageCompleted?.Invoke();
    }

    // =========================================================
    // Narration
    // =========================================================

    private IEnumerator PlayStoryLines(
        StoryLine[] lines)
    {
        if (narrationPanel == null ||
            narrationText == null)
        {
            Debug.LogError(
                "FlowController4: Narration Panel or Text is missing."
            );

            yield break;
        }

        narrationPanel.SetActive(true);
        narrationText.gameObject.SetActive(true);

        if (lines == null)
            yield break;

        foreach (StoryLine line in lines)
        {
            if (line == null)
                continue;

            narrationText.text = line.text;
            narrationText.ForceMeshUpdate();

            int characterCount =
                narrationText.textInfo.characterCount;

            narrationText.maxVisibleCharacters = 0;

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
                        "FlowController4: Narration AudioSource is missing."
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

                narrationText.maxVisibleCharacters =
                    Mathf.FloorToInt(
                        characterCount * progress
                    );

                yield return null;
            }

            narrationText.maxVisibleCharacters =
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
                    yield return new WaitForSecondsRealtime(
                        remaining
                    );
                }
            }

            if (timeBetweenLines > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    timeBetweenLines
                );
            }
        }
    }

    private void HideNarration()
    {
        if (narrationPanel != null)
        {
            narrationPanel.SetActive(false);
        }
    }

    // =========================================================
    // Plant stages
    // =========================================================

    private IEnumerator ChangePlantStageRoutine(
    int newStage)
    {
        if (beforeModelSwitchDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                beforeModelSwitchDelay
            );
        }

        /*
         * 每次模型变化时，
         * 重新播放同一个特效和同一个音效。
         */
        PlayPlantChangeEffect();
        PlayPlantChangeSFX();

        float totalEffectDuration =
            Mathf.Max(
                0f,
                plantChangeEffectDuration
            );

        float switchTime =
            Mathf.Clamp(
                plantModelSwitchTime,
                0f,
                totalEffectDuration
            );

        /*
         * 先让特效出现一小段时间，
         * 再切换模型。
         */
        if (switchTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                switchTime
            );
        }

        SetPlantStage(newStage);

        /*
         * 模型切换后继续播放剩余特效。
         */
        float remainingEffectTime =
            totalEffectDuration -
            switchTime;

        if (remainingEffectTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remainingEffectTime
            );
        }

        /*
         * 特效结束后清空并隐藏，
         * 等待下一次模型变化时重新使用。
         */
        StopPlantChangeEffect();

        if (plantChangeEffectRoot != null)
        {
            plantChangeEffectRoot.SetActive(false);
        }

        if (afterModelSwitchDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                afterModelSwitchDelay
            );
        }
    }
    private void PlayPlantChangeEffect()
    {
        if (plantChangeEffectRoot == null)
            return;

        plantChangeEffectRoot.SetActive(true);

        if (plantChangeParticles == null ||
            plantChangeParticles.Length == 0)
        {
            plantChangeParticles =
                plantChangeEffectRoot
                    .GetComponentsInChildren<ParticleSystem>(true);
        }

        foreach (ParticleSystem particle
                 in plantChangeParticles)
        {
            if (particle == null)
                continue;

            /*
             * 每次都先清空上一次残留粒子，
             * 再重新播放。
             */
            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particle.Clear(true);
            particle.Play(true);
        }
    }
    private void StopPlantChangeEffect()
    {
        if (plantChangeParticles == null)
            return;

        foreach (ParticleSystem particle
                 in plantChangeParticles)
        {
            if (particle == null)
                continue;

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }
    private void PlayPlantChangeSFX()
    {
        if (plantChangeAudioSource == null ||
            plantChangeClip == null)
        {
            return;
        }

        plantChangeAudioSource.PlayOneShot(
            plantChangeClip,
            plantChangeVolume
        );
    }

    private void SetPlantStage(int stage)
    {
        Debug.Log(
            $"FlowController4: Changing plant stage to {stage}"
        );

        if (sproutModel != null)
        {
            sproutModel.SetActive(stage == 0);
        }
        else
        {
            Debug.LogError(
                "FlowController4: Sprout Model is missing."
            );
        }

        if (sproutPlusModel != null)
        {
            sproutPlusModel.SetActive(stage == 1);
        }
        else
        {
            Debug.LogError(
                "FlowController4: Sprout Plus Model is missing."
            );
        }

        if (flowerModel != null)
        {
            flowerModel.SetActive(stage == 2);
        }
        else
        {
            Debug.LogError(
                "FlowController4: Flower Model is missing."
            );
        }
    }

    // =========================================================
    // Day → Night → Day
    // =========================================================

    private IEnumerator DayNightDayRoutine()
    {
        PlayTimePassageSFX();

        Quaternion nightRotation =
            GetNightRotation();

        Quaternion dayRotation =
            GetDayRotation();

        yield return StartCoroutine(
            TransitionLightRoutine(
                nightRotation,
                nightLightColor,
                nightLightIntensity,
                nightAmbientColor,
                dayToNightDuration
            )
        );

        if (nightHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                nightHoldDuration
            );
        }

        yield return StartCoroutine(
            TransitionLightRoutine(
                dayRotation,
                dayLightColor,
                dayLightIntensity,
                dayAmbientColor,
                nightToDayDuration
            )
        );

        if (dayHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                dayHoldDuration
            );
        }
    }

    private IEnumerator TransitionLightRoutine(
        Quaternion targetRotation,
        Color targetColor,
        float targetIntensity,
        Color targetAmbientColor,
        float transitionDuration)
    {
        if (directionalLight == null)
        {
            Debug.LogError(
                "FlowController4: Directional Light is missing."
            );

            yield break;
        }

        Quaternion startRotation =
            directionalLight.transform.rotation;

        Color startColor =
            directionalLight.color;

        float startIntensity =
            directionalLight.intensity;

        Color startAmbientColor =
            RenderSettings.ambientLight;

        float duration =
            Mathf.Max(
                0.05f,
                transitionDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float progress =
                lightTransitionCurve != null
                    ? lightTransitionCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;

            directionalLight.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    progress
                );

            directionalLight.color =
                Color.Lerp(
                    startColor,
                    targetColor,
                    progress
                );

            directionalLight.intensity =
                Mathf.Lerp(
                    startIntensity,
                    targetIntensity,
                    progress
                );

            if (controlAmbientLight)
            {
                RenderSettings.ambientLight =
                    Color.Lerp(
                        startAmbientColor,
                        targetAmbientColor,
                        progress
                    );
            }

            yield return null;
        }

        directionalLight.transform.rotation =
            targetRotation;

        directionalLight.color =
            targetColor;

        directionalLight.intensity =
            targetIntensity;

        if (controlAmbientLight)
        {
            RenderSettings.ambientLight =
                targetAmbientColor;
        }
    }

    private Quaternion GetDayRotation()
    {
        if (dayLightAnchor != null)
        {
            return dayLightAnchor.rotation;
        }

        return Quaternion.Euler(
            fallbackDayRotation
        );
    }

    private Quaternion GetNightRotation()
    {
        if (nightLightAnchor != null)
        {
            return nightLightAnchor.rotation;
        }

        return Quaternion.Euler(
            fallbackNightRotation
        );
    }

    private void SetImmediateDayLight()
    {
        if (directionalLight == null)
            return;

        directionalLight.transform.rotation =
            GetDayRotation();

        directionalLight.color =
            dayLightColor;

        directionalLight.intensity =
            dayLightIntensity;

        if (controlAmbientLight)
        {
            RenderSettings.ambientLight =
                dayAmbientColor;
        }
    }

    private void PlayTimePassageSFX()
    {
        if (sfxAudioSource != null &&
            timePassageClip != null)
        {
            sfxAudioSource.PlayOneShot(
                timePassageClip,
                timePassageVolume
            );
        }
    }

    // =========================================================
    // The End
    // =========================================================

    private IEnumerator ShowTheEndRoutine()
    {
        if (blackOverlayImage == null)
        {
            Debug.LogError(
                "FlowController4: Black Overlay Image is missing."
            );

            yield break;
        }

        /*
         * 先让全屏黑色 Image 从透明变成不透明。
         */
        blackOverlayImage.gameObject.SetActive(true);

        yield return StartCoroutine(
            FadeImageAlphaRoutine(
                blackOverlayImage,
                blackOverlayImage.color.a,
                1f,
                blackFadeDuration
            )
        );

        /*
         * 黑屏完成后开始拦截点击，
         * 避免用户继续操作下面的内容。
         */
        blackOverlayImage.raycastTarget = true;

        if (beforeTheEndDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                beforeTheEndDelay
            );
        }

        /*
         * THE END 淡入。
         */
        if (theEndText != null)
        {
            theEndText.gameObject.SetActive(true);

            yield return StartCoroutine(
                FadeTextAlphaRoutine(
                    theEndText,
                    theEndText.color.a,
                    1f,
                    theEndFadeDuration
                )
            );
        }

        if (theEndHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                theEndHoldDuration
            );
        }

        /*
         * 黑屏和 THE END 保持显示，
         * 不再自动隐藏。
         */
    }

    private IEnumerator FadeImageAlphaRoutine(
    Image targetImage,
    float startAlpha,
    float targetAlpha,
    float fadeDuration)
    {
        if (targetImage == null)
            yield break;

        float duration =
            Mathf.Max(
                0.05f,
                fadeDuration
            );

        Color startColor =
            targetImage.color;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            Color currentColor =
                startColor;

            currentColor.a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );

            targetImage.color =
                currentColor;

            yield return null;
        }

        Color finalColor =
            targetImage.color;

        finalColor.a =
            targetAlpha;

        targetImage.color =
            finalColor;
    }
    private IEnumerator FadeTextAlphaRoutine(
    TMP_Text targetText,
    float startAlpha,
    float targetAlpha,
    float fadeDuration)
    {
        if (targetText == null)
            yield break;

        float duration =
            Mathf.Max(
                0.05f,
                fadeDuration
            );

        Color startColor =
            targetText.color;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            Color currentColor =
                startColor;

            currentColor.a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );

            targetText.color =
                currentColor;

            yield return null;
        }

        Color finalColor =
            targetText.color;

        finalColor.a =
            targetAlpha;

        targetText.color =
            finalColor;
    }
}