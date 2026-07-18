using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class FlowController3 : MonoBehaviour
{
    [System.Serializable]
    public class StoryLine
    {
        [TextArea(2, 5)]
        public string text;

        public AudioClip voiceClip;
    }

    [Header("Story UI")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private AudioSource narrationAudioSource;

    [Header("Story Typewriter")]
    [Range(0.5f, 1f)]
    [SerializeField] private float revealDurationRatio = 0.9f;

    [SerializeField] private float fallbackCharactersPerSecond = 25f;
    [SerializeField] private float timeBetweenLines = 0.25f;

    [Header("Page 12 Story")]
    [SerializeField]
    private StoryLine firstLine =
        new StoryLine
        {
            text = "Your goodbye has reached the Memory Seed."
        };

    [SerializeField]
    private StoryLine secondLine =
        new StoryLine
        {
            text = "It is ready to grow with you."
        };

    [SerializeField]
    private StoryLine thirdLine =
        new StoryLine
        {
            text = "Take it home. Let it grow beside you."
        };

    [SerializeField]
    private StoryLine fourthLine =
    new StoryLine
    {
        text = "From here, you can keep growing in your own way."
    };

    [Header("Light Point")]
    [SerializeField] private GameObject lightPointRoot;

    [Tooltip("光团开始位置，放在花盆上方")]
    [SerializeField] private Transform lightPointStartAnchor;

    [Tooltip("光团最终飞入的位置，放在花盆中的种子位置")]
    [SerializeField] private Transform lightPointTargetAnchor;

    [Tooltip("AR 场景出现后，等待多久才开始播放光团动画")]
    [SerializeField] private float pageAppearDelay = 0.5f;

    [Tooltip("光团在上方先停留一下，让玩家看见")]
    [SerializeField] private float lightPointStartHoldDuration = 0.25f;

    [SerializeField] private float lightPointFlyDuration = 1.2f;

    [Tooltip("飞到花盆后停留多久再消失")]
    [SerializeField] private float lightPointEndHoldDuration = 0.1f;

    [SerializeField]
    private AnimationCurve lightPointFlyCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Seed Models")]
    [Tooltip("原来的种子模型")]
    [SerializeField] private GameObject originalSeedModel;

    [Tooltip("发芽后的种子模型，初始隐藏")]
    [SerializeField] private GameObject sproutedSeedModel;

    [Header("Seed Growth Effect")]
    [SerializeField] private GameObject growthEffectRoot;

    [Header("Seed Success Effect")]
    [Tooltip("种子完成发芽后出现的成功特效，初始隐藏")]
    [SerializeField] private GameObject successEffectRoot;

    [Tooltip("成功特效持续时间")]
    [SerializeField] private float successEffectDuration = 1.2f;

    [Tooltip("发芽完成后的成功音效")]
    [SerializeField] private AudioClip successClip;

    [Range(0f, 1f)]
    [SerializeField] private float successVolume = 1f;

    [Tooltip("生长特效总时长")]
    [SerializeField] private float growthEffectDuration = 1.5f;

    [Tooltip("生长过程中，多久后切换成发芽模型")]
    [SerializeField] private float sproutSwitchTime = 0.6f;

    [SerializeField] private float afterGrowthDelay = 0.15f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxAudioSource;

    [SerializeField] private AudioClip lightPointFlyClip;
    [SerializeField] private AudioClip seedGrowthClip;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Butterfly Departure")]
    [Tooltip("需要移动和最终隐藏的整个蝴蝶对象")]
    [SerializeField] private GameObject butterflyRoot;

    [Tooltip("绕圈中心，放在花盆中心附近")]
    [SerializeField] private Transform potOrbitCenter;

    [Tooltip("蝴蝶最后飞走的目标点，放在画面外")]
    [SerializeField] private Transform butterflyFlyAwayTarget;

    [Tooltip("蝴蝶先飞到花盆附近的时间")]
    [SerializeField] private float butterflyApproachDuration = 1f;

    [Tooltip("绕花盆时与中心的距离")]
    [SerializeField] private float butterflyOrbitRadius = 0.25f;

    [Tooltip("绕圈轨道高于花盆中心的高度")]
    [SerializeField] private float butterflyOrbitHeight = 0.15f;

    [Tooltip("绕花盆一周所需时间")]
    [SerializeField] private float butterflyOrbitDuration = 2f;

    [Tooltip("顺时针绕花盆")]
    [SerializeField] private bool butterflyOrbitClockwise = true;

    [Tooltip("绕完后飞出画面的时间")]
    [SerializeField] private float butterflyFlyAwayDuration = 1.2f;

    [SerializeField]
    private AnimationCurve butterflyMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("是否根据飞行方向旋转蝴蝶")]
    [SerializeField] private bool rotateButterflyAlongPath = false;

    [SerializeField] private float butterflyRotationSpeed = 8f;

    [Tooltip("可选：蝴蝶开始离开时播放的飞行音效")]
    [SerializeField] private AudioClip butterflyFlyClip;

    [Header("Completion")]
    [Tooltip("整页播放结束后等待多久再触发完成事件")]
    [SerializeField] private float endHoldDuration = 0.5f;

    [Tooltip("可用于衔接下一页")]
    [SerializeField] private UnityEvent onPageCompleted;

    private ParticleSystem[] growthParticles;
    private ParticleSystem[] lightPointParticles;
    private ParticleSystem[] successParticles;

    private bool pageStarted;

    private void Awake()
    {
        PrepareInitialState();
        CacheParticles();
    }

    private void Start()
    {
        if (!pageStarted)
        {
            pageStarted = true;
            StartCoroutine(Page12Routine());
        }
    }

    private void PrepareInitialState()
    {
        if (storyPanel != null)
            storyPanel.SetActive(false);

        if (storyText != null)
        {
            storyText.text = "";
            storyText.maxVisibleCharacters = 0;
        }

        PrepareLightPointInitialState();

        if (originalSeedModel != null)
            originalSeedModel.SetActive(true);

        if (sproutedSeedModel != null)
            sproutedSeedModel.SetActive(false);

        if (growthEffectRoot != null)
            growthEffectRoot.SetActive(false);

        if (successEffectRoot != null)
            successEffectRoot.SetActive(false);

        if (butterflyRoot != null)
        {
            butterflyRoot.SetActive(true);
        }
    }
    private void PrepareLightPointInitialState()
    {
        if (lightPointRoot == null)
            return;

        /*
         * 防止错误地把挂 FlowController3 的对象
         * 拖入 Light Point Root。
         */
        if (lightPointRoot == gameObject)
        {
            Debug.LogError(
                "FlowController3: Light Point Root cannot be " +
                "the same GameObject as FlowController3."
            );

            return;
        }

        StopParticles(lightPointParticles);

        if (lightPointStartAnchor != null)
        {
            lightPointRoot.transform.position =
                lightPointStartAnchor.position;

            lightPointRoot.transform.rotation =
                lightPointStartAnchor.rotation;
        }

        lightPointRoot.SetActive(false);
    }

    private void CacheParticles()
    {
        if (growthEffectRoot != null)
        {
            growthParticles =
                growthEffectRoot.GetComponentsInChildren
                <ParticleSystem>(true);
        }

        if (successEffectRoot != null)
        {
            successParticles =
                successEffectRoot.GetComponentsInChildren
                <ParticleSystem>(true);
        }

        if (lightPointRoot != null)
        {
            lightPointParticles =
                lightPointRoot.GetComponentsInChildren
                <ParticleSystem>(true);
        }
    }

    private IEnumerator Page12Routine()
    {
        if (pageAppearDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                pageAppearDelay
            );
        }

        // 光团从上方飞进花盆
        yield return StartCoroutine(
            LightPointFlyIntoPotRoutine()
        );

        // 第一段故事
        yield return StartCoroutine(
            PlayStoryLine(firstLine)
        );

        // 种子成长、切换发芽模型、成功特效
        yield return StartCoroutine(
            SeedGrowthRoutine()
        );

        // 最后三段故事
        yield return StartCoroutine(
            PlayStoryLine(secondLine)
        );

        yield return StartCoroutine(
            PlayStoryLine(thirdLine)
        );

        yield return StartCoroutine(
            PlayStoryLine(fourthLine)
        );

        // 最后一句念完后关闭 Story UI
        HideStory();

        // 蝴蝶绕花盆一周后飞走
        yield return StartCoroutine(
            ButterflyDepartureRoutine()
        );

        if (endHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                endHoldDuration
            );
        }

        if (SimpleCloudRecoEventHandler.Instance != null)
        {
            SimpleCloudRecoEventHandler.Instance.ShowNextPageCanvas();
        }
    }
    private void HideStory()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }
    }
    private IEnumerator ButterflyDepartureRoutine()
    {
        if (butterflyRoot == null)
        {
            Debug.LogError(
                "FlowController3: Butterfly Root is missing."
            );

            yield break;
        }

        if (potOrbitCenter == null)
        {
            Debug.LogError(
                "FlowController3: Pot Orbit Center is missing."
            );

            yield break;
        }

        butterflyRoot.SetActive(true);

        if (butterflyFlyClip != null)
        {
            PlaySFX(butterflyFlyClip);
        }

        Transform butterflyTransform =
            butterflyRoot.transform;

        /*
         * 使用花盆中心的本地轴构成绕圈平面。
         * 对 AR Prefab 旋转更加稳定，不依赖世界坐标。
         */
        Vector3 orbitUp =
            potOrbitCenter.up.normalized;

        Vector3 orbitRight =
            potOrbitCenter.right.normalized;

        Vector3 orbitForward =
            potOrbitCenter.forward.normalized;

        Vector3 orbitCenterPosition =
            potOrbitCenter.position;

        Vector3 orbitStartPosition =
            orbitCenterPosition +
            orbitRight * butterflyOrbitRadius +
            orbitUp * butterflyOrbitHeight;

        /*
         * 第一阶段：
         * 从当前 Story UI 附近飞到花盆旁边。
         */
        yield return StartCoroutine(
            MoveButterflyRoutine(
                butterflyTransform,
                butterflyTransform.position,
                orbitStartPosition,
                butterflyApproachDuration,
                orbitUp
            )
        );

        /*
         * 第二阶段：
         * 绕花盆一整周。
         */
        float orbitDuration =
            Mathf.Max(
                0.05f,
                butterflyOrbitDuration
            );

        float orbitElapsed = 0f;

        Vector3 previousPosition =
            butterflyTransform.position;

        float direction =
            butterflyOrbitClockwise
                ? -1f
                : 1f;

        while (orbitElapsed < orbitDuration)
        {
            orbitElapsed +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    orbitElapsed /
                    orbitDuration
                );

            float angle =
                normalizedTime *
                Mathf.PI *
                2f *
                direction;

            Vector3 nextPosition =
                orbitCenterPosition +
                orbitRight *
                Mathf.Cos(angle) *
                butterflyOrbitRadius +
                orbitForward *
                Mathf.Sin(angle) *
                butterflyOrbitRadius +
                orbitUp *
                butterflyOrbitHeight;

            butterflyTransform.position =
                nextPosition;

            RotateButterflyTowardMovement(
                butterflyTransform,
                nextPosition - previousPosition,
                orbitUp
            );

            previousPosition =
                nextPosition;

            yield return null;
        }

        // 确保完整绕回起点
        butterflyTransform.position =
            orbitStartPosition;

        /*
         * 第三阶段：
         * 飞向画面外的离场点。
         */
        Vector3 flyAwayPosition;

        if (butterflyFlyAwayTarget != null)
        {
            flyAwayPosition =
                butterflyFlyAwayTarget.position;
        }
        else
        {
            /*
             * 没有配置目标点时的备用方向：
             * 向上并向前飞。
             */
            flyAwayPosition =
                orbitCenterPosition +
                orbitUp * 0.6f +
                orbitForward * 0.6f;
        }

        yield return StartCoroutine(
            MoveButterflyRoutine(
                butterflyTransform,
                butterflyTransform.position,
                flyAwayPosition,
                butterflyFlyAwayDuration,
                orbitUp
            )
        );

        /*
         * 飞出画面后关闭整个蝴蝶对象。
         */
        butterflyRoot.SetActive(false);
    }
    private IEnumerator MoveButterflyRoutine(
    Transform butterflyTransform,
    Vector3 startPosition,
    Vector3 endPosition,
    float moveDuration,
    Vector3 upDirection)
    {
        if (butterflyTransform == null)
            yield break;

        float duration =
            Mathf.Max(
                0.05f,
                moveDuration
            );

        float elapsed = 0f;

        Vector3 previousPosition =
            startPosition;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            float movementProgress =
                butterflyMoveCurve != null
                    ? butterflyMoveCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;

            Vector3 nextPosition =
                Vector3.LerpUnclamped(
                    startPosition,
                    endPosition,
                    movementProgress
                );

            butterflyTransform.position =
                nextPosition;

            RotateButterflyTowardMovement(
                butterflyTransform,
                nextPosition - previousPosition,
                upDirection
            );

            previousPosition =
                nextPosition;

            yield return null;
        }

        butterflyTransform.position =
            endPosition;
    }
    private void RotateButterflyTowardMovement(
    Transform butterflyTransform,
    Vector3 movementDirection,
    Vector3 upDirection)
    {
        if (!rotateButterflyAlongPath ||
            butterflyTransform == null ||
            movementDirection.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                movementDirection.normalized,
                upDirection.normalized
            );

        butterflyTransform.rotation =
            Quaternion.Slerp(
                butterflyTransform.rotation,
                targetRotation,
                Time.unscaledDeltaTime *
                butterflyRotationSpeed
            );
    }

    // =========================================================
    // Light point
    // =========================================================

    private IEnumerator LightPointFlyIntoPotRoutine()
    {
        if (lightPointRoot == null)
        {
            Debug.LogError(
                "FlowController3: Light Point Root is missing."
            );

            yield break;
        }

        if (lightPointStartAnchor == null)
        {
            Debug.LogError(
                "FlowController3: Light Point Start Anchor is missing."
            );

            yield break;
        }

        if (lightPointTargetAnchor == null)
        {
            Debug.LogError(
                "FlowController3: Light Point Target Anchor is missing."
            );

            yield break;
        }

        if (lightPointRoot == gameObject)
        {
            Debug.LogError(
                "FlowController3: Light Point Root cannot be " +
                "the FlowController3 GameObject."
            );

            yield break;
        }

        Vector3 startPosition =
            lightPointStartAnchor.position;

        Vector3 targetPosition =
            lightPointTargetAnchor.position;

        /*
         * 起点和终点太接近时，不会看到飞行动画。
         */
        if (Vector3.Distance(
                startPosition,
                targetPosition) < 0.01f)
        {
            Debug.LogError(
                "FlowController3: Light Point Start and Target " +
                "are almost at the same position."
            );

            yield break;
        }

        /*
         * 每次播放前先彻底重置。
         */
        lightPointRoot.SetActive(false);

        lightPointRoot.transform.position =
            startPosition;

        lightPointRoot.transform.rotation =
            lightPointStartAnchor.rotation;

        if (lightPointParticles == null ||
            lightPointParticles.Length == 0)
        {
            lightPointParticles =
                lightPointRoot
                    .GetComponentsInChildren
                    <ParticleSystem>(true);
        }

        /*
         * 先激活，再等待一帧，
         * 确保摄像机真正渲染到光团。
         */
        lightPointRoot.SetActive(true);

        yield return null;

        PlayParticles(lightPointParticles);
        PlaySFX(lightPointFlyClip);

        /*
         * 在上方停留一下。
         */
        if (lightPointStartHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                lightPointStartHoldDuration
            );
        }

        float duration =
            Mathf.Max(
                0.1f,
                lightPointFlyDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float movementProgress =
                lightPointFlyCurve != null
                    ? lightPointFlyCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;

            lightPointRoot.transform.position =
                Vector3.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    movementProgress
                );

            yield return null;
        }

        lightPointRoot.transform.position =
            targetPosition;

        /*
         * 飞入花盆后短暂停留。
         */
        if (lightPointEndHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                lightPointEndHoldDuration
            );
        }

        /*
         * 清空粒子后，关闭整个光团对象。
         */
        StopParticles(lightPointParticles);

        lightPointRoot.SetActive(false);

        Debug.Log(
            "FlowController3: Light point reached the pot and was hidden."
        );
    }

    // =========================================================
    // Seed growth
    // =========================================================

    private IEnumerator SeedGrowthRoutine()
    {
        /*
         * 第一阶段：
         * 生长特效和生长音效开始。
         */
        if (growthEffectRoot != null)
        {
            growthEffectRoot.SetActive(true);
            PlayParticles(growthParticles);
        }

        PlaySFX(seedGrowthClip);

        float totalGrowthDuration =
            Mathf.Max(0f, growthEffectDuration);

        float switchTime =
            Mathf.Clamp(
                sproutSwitchTime,
                0f,
                totalGrowthDuration
            );

        /*
         * 等待到模型切换时间。
         */
        if (switchTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                switchTime
            );
        }

        /*
         * 原种子消失，
         * 发芽种子出现。
         */
        if (originalSeedModel != null)
        {
            originalSeedModel.SetActive(false);
        }

        if (sproutedSeedModel != null)
        {
            sproutedSeedModel.SetActive(true);
        }

        /*
         * 继续等待剩余的生长特效时间。
         */
        float remainingGrowthTime =
            totalGrowthDuration - switchTime;

        if (remainingGrowthTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remainingGrowthTime
            );
        }

        /*
         * 生长特效结束并隐藏。
         */
        if (growthEffectRoot != null)
        {
            StopParticles(growthParticles);
            growthEffectRoot.SetActive(false);
        }

        /*
         * 第二阶段：
         * 发芽完成后显示成功特效，
         * 同时播放成功音效。
         */
        yield return StartCoroutine(
            SeedSuccessEffectRoutine()
        );

        /*
         * 成功特效结束后稍微停顿，
         * 然后进入最后两句对白。
         */
        if (afterGrowthDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                afterGrowthDelay
            );
        }
    }
    private IEnumerator SeedSuccessEffectRoutine()
    {
        if (successEffectRoot != null)
        {
            successEffectRoot.SetActive(true);

            if (successParticles == null ||
                successParticles.Length == 0)
            {
                successParticles =
                    successEffectRoot.GetComponentsInChildren
                    <ParticleSystem>(true);
            }

            PlayParticles(successParticles);
        }

        /*
         * 成功特效出现的同一时刻播放 Success SFX。
         */
        if (sfxAudioSource != null &&
            successClip != null)
        {
            sfxAudioSource.PlayOneShot(
                successClip,
                successVolume
            );
        }

        if (successEffectDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                successEffectDuration
            );
        }

        /*
         * 成功特效播放完毕后停止并隐藏。
         */
        if (successEffectRoot != null)
        {
            StopParticles(successParticles);
            successEffectRoot.SetActive(false);
        }
    }

    // =========================================================
    // Story
    // =========================================================

    private IEnumerator PlayStoryLine(StoryLine line)
    {
        if (line == null)
            yield break;

        if (storyPanel == null || storyText == null)
        {
            Debug.LogError(
                "FlowController3: StoryPanel or StoryText is missing."
            );

            yield break;
        }

        storyPanel.SetActive(true);
        storyText.gameObject.SetActive(true);

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
                narrationAudioSource.clip = line.voiceClip;
                narrationAudioSource.Play();
            }
        }
        else
        {
            totalDuration =
                characterCount /
                Mathf.Max(1f, fallbackCharactersPerSecond);

            totalDuration = Mathf.Max(0.8f, totalDuration);
        }

        float revealDuration =
            Mathf.Max(0.05f, totalDuration * revealDurationRatio);

        float elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsed / revealDuration);

            storyText.maxVisibleCharacters =
                Mathf.FloorToInt(characterCount * progress);

            yield return null;
        }

        storyText.maxVisibleCharacters = int.MaxValue;

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
                yield return new WaitForSecondsRealtime(remaining);
            }
        }

        if (timeBetweenLines > 0f)
        {
            yield return new WaitForSecondsRealtime(timeBetweenLines);
        }
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, sfxVolume);
        }
    }

    private void PlayParticles(ParticleSystem[] particles)
    {
        if (particles == null)
            return;

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null)
                continue;

            particle.Clear(true);
            particle.Play(true);
        }
    }

    private void StopParticles(ParticleSystem[] particles)
    {
        if (particles == null)
            return;

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null)
                continue;

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }
}