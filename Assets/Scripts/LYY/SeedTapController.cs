using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class SeedTapController : MonoBehaviour
{
    [System.Serializable]
    private class ConvergingEnergy
    {
        [Header("成功放置后的固定物品模型")]
        public GameObject placedItemModel;

        [Header("从物品位置飞向花盆的光圈特效")]
        public GameObject flyingGlowEffect;
    }

    [Header("Camera")]
    [SerializeField] private Camera arCamera;

    [Header("Click Detection")]
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    [Header("Three Item Energy Effects")]
    [Tooltip("顺序可以是 Leaf、Petal、Star")]
    [SerializeField] private ConvergingEnergy[] convergingEnergies;

    [Tooltip("三个光圈最终飞向的位置，建议放在花盆种子上方")]
    [SerializeField] private Transform convergenceTarget;

    [Tooltip("光圈飞向花盆所需的时间")]
    [SerializeField] private float convergenceDuration = 1.2f;

    [Tooltip("飞行轨迹向上弯曲的高度")]
    [SerializeField] private float convergenceArcHeight = 0.03f;

    [Tooltip("光圈飞到花盆时缩小到原来的比例")]
    [Range(0.1f, 1f)]
    [SerializeField] private float endScaleMultiplier = 0.4f;

    [Tooltip("控制飞行速度变化")]
    [SerializeField]
    private AnimationCurve convergenceCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("三个能量汇聚完成后，等待多久显示种子光圈")]
    [SerializeField] private float delayBeforeSeedGlow = 0.25f;

    [Header("Seed Glow")]
    [Tooltip("种子最终尝试开花时出现的光圈")]
    [SerializeField] private GameObject seedGlowRing;

    [Header("Audio")]
    [Tooltip("必须使用不会被隐藏的 AudioSource")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Tooltip("三个光圈飞向花盆时的音效")]
    [SerializeField] private AudioClip convergenceClip;

    [Tooltip("种子光圈出现时的音效")]
    [SerializeField] private AudioClip activationClip;

    [Range(0f, 1f)]
    [SerializeField] private float convergenceVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float activationVolume = 1f;

    [Header("Final Timing")]
    [Tooltip("种子光圈持续时间")]
    [SerializeField] private float seedGlowDuration = 2f;

    [Tooltip("种子光圈消失后，多久显示最终对白")]
    [SerializeField] private float pauseBeforeFinalDialogue = 0.5f;

    [Header("Page Flow")]
    [SerializeField] private FlowController1 flowController;

    private bool interactable;
    private bool hasBeenTapped;
    private bool isProcessing;

    private void Awake()
    {
        FindCamera();

        interactable = false;
        hasBeenTapped = false;
        isProcessing = false;

        HideSeedGlow();
        HideAllFlyingGlowEffects();
    }

    private void OnEnable()
    {
        /*
         * PlantedSeed 从隐藏变成显示时，
         * 再次确保所有光圈都不会提前出现。
         */
        HideSeedGlow();
        HideAllFlyingGlowEffects();
    }

    private void Update()
    {
        if (!interactable ||
            hasBeenTapped ||
            isProcessing)
        {
            return;
        }

        if (PointerDown(out Vector2 screenPosition))
        {
            TryTapSeed(screenPosition);
        }
    }

    private void FindCamera()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            arCamera = FindFirstObjectByType<Camera>();
#else
            arCamera = FindObjectOfType<Camera>();
#endif
        }

        if (arCamera == null)
        {
            Debug.LogError(
                "SeedTapController: AR Camera could not be found."
            );
        }
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        /*
         * 开启点击权限并不等于显示种子光圈。
         * 种子光圈只在玩家真正点击后显示。
         */
        if (!value)
        {
            HideSeedGlow();
        }

        Debug.Log(
            $"SeedTapController: Interactable = {value}"
        );
    }

    private void TryTapSeed(Vector2 screenPosition)
    {
        if (arCamera == null)
            return;

        Ray ray =
            arCamera.ScreenPointToRay(screenPosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            rayDistance,
            clickableLayers
        );

        if (hits.Length == 0)
            return;

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            bool clickedSeed =
                hit.transform == transform ||
                hit.transform.IsChildOf(transform) ||
                transform.IsChildOf(hit.transform);

            if (!clickedSeed)
                continue;

            hasBeenTapped = true;
            interactable = false;

            StartCoroutine(ActivationRoutine());
            return;
        }
    }

    private IEnumerator ActivationRoutine()
    {
        isProcessing = true;

        // 隐藏 “Tap the Memory Seed”
        if (flowController != null)
        {
            flowController.HideAllUI();
        }

        /*
         * 第一阶段：
         * 三个物品消失，
         * 三个光圈从物品原位置飞向花盆。
         */
        yield return StartCoroutine(
            PlayEnergyConvergenceRoutine()
        );

        if (delayBeforeSeedGlow > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delayBeforeSeedGlow
            );
        }

        /*
         * 第二阶段：
         * 能量已经进入花盆，种子光圈出现。
         */
        if (seedGlowRing != null)
        {
            seedGlowRing.SetActive(true);
            RestartParticleSystems(seedGlowRing);
        }

        if (sfxAudioSource != null &&
            activationClip != null)
        {
            sfxAudioSource.PlayOneShot(
                activationClip,
                activationVolume
            );
        }

        yield return new WaitForSecondsRealtime(
            seedGlowDuration
        );

        /*
         * 种子光圈消失，无事发生。
         */
        HideSeedGlow();

        if (pauseBeforeFinalDialogue > 0f)
        {
            yield return new WaitForSecondsRealtime(
                pauseBeforeFinalDialogue
            );
        }

        /*
         * 最终 Story UI：
         * Something is still missing.
         * 配音和逐字文本由 FlowController 处理，
         * 且 Story UI 不再消失。
         */
        if (flowController != null)
        {
            flowController.ShowFinalSomethingMissing();
        }
        else
        {
            Debug.LogError(
                "SeedTapController: FlowController is missing."
            );
        }

        isProcessing = false;
    }

    private IEnumerator PlayEnergyConvergenceRoutine()
    {
        if (convergenceTarget == null)
        {
            Debug.LogError(
                "SeedTapController: Convergence Target is missing."
            );

            yield break;
        }

        if (convergingEnergies == null ||
            convergingEnergies.Length == 0)
        {
            Debug.LogWarning(
                "SeedTapController: No converging energies assigned."
            );

            yield break;
        }

        int count = convergingEnergies.Length;

        Vector3[] startPositions =
            new Vector3[count];

        Quaternion[] startRotations =
            new Quaternion[count];

        Vector3[] startScales =
            new Vector3[count];

        bool[] validEnergy =
            new bool[count];

        /*
         * 记录三个物品的位置。
         * 然后隐藏原物品，在相同位置显示飞行光圈。
         */
        for (int i = 0; i < count; i++)
        {
            ConvergingEnergy energy =
                convergingEnergies[i];

            if (energy == null ||
                energy.placedItemModel == null ||
                energy.flyingGlowEffect == null)
            {
                validEnergy[i] = false;
                continue;
            }

            Transform itemTransform =
                energy.placedItemModel.transform;

            Transform glowTransform =
                energy.flyingGlowEffect.transform;

            startPositions[i] =
                itemTransform.position;

            startRotations[i] =
                itemTransform.rotation;

            startScales[i] =
                glowTransform.localScale;

            /*
             * 先把光圈放到原物品位置。
             */
            glowTransform.position =
                startPositions[i];

            glowTransform.rotation =
                startRotations[i];

            glowTransform.localScale =
                startScales[i];

            /*
             * 原来的 Leaf / Petal / Star 消失。
             */
            energy.placedItemModel.SetActive(false);

            /*
             * 对应光圈出现。
             */
            energy.flyingGlowEffect.SetActive(true);
            RestartParticleSystems(
                energy.flyingGlowEffect
            );

            validEnergy[i] = true;
        }

        if (sfxAudioSource != null &&
            convergenceClip != null)
        {
            sfxAudioSource.PlayOneShot(
                convergenceClip,
                convergenceVolume
            );
        }

        float elapsed = 0f;

        while (elapsed < convergenceDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                convergenceDuration > 0f
                    ? Mathf.Clamp01(
                        elapsed / convergenceDuration
                    )
                    : 1f;

            float curvedTime =
                convergenceCurve != null
                    ? convergenceCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;

            /*
             * 每一帧重新读取目标位置。
             * 即使 AR Image Target 有轻微移动，
             * 光圈仍会飞向当前花盆位置。
             */
            Vector3 targetPosition =
                convergenceTarget.position;

            Vector3 arcDirection =
                convergenceTarget.up;

            for (int i = 0; i < count; i++)
            {
                if (!validEnergy[i])
                    continue;

                GameObject glow =
                    convergingEnergies[i]
                        .flyingGlowEffect;

                if (glow == null)
                    continue;

                Vector3 position =
                    Vector3.Lerp(
                        startPositions[i],
                        targetPosition,
                        curvedTime
                    );

                /*
                 * 添加一个轻微弧线，
                 * 避免光圈贴着桌面直线移动。
                 */
                float arc =
                    Mathf.Sin(
                        normalizedTime * Mathf.PI
                    ) * convergenceArcHeight;

                position += arcDirection * arc;

                glow.transform.position =
                    position;

                glow.transform.localScale =
                    Vector3.Lerp(
                        startScales[i],
                        startScales[i] *
                            endScaleMultiplier,
                        curvedTime
                    );
            }

            yield return null;
        }

        /*
         * 三个光圈到达花盆中心后消失。
         */
        for (int i = 0; i < count; i++)
        {
            if (!validEnergy[i])
                continue;

            GameObject glow =
                convergingEnergies[i]
                    .flyingGlowEffect;

            if (glow == null)
                continue;

            glow.transform.position =
                convergenceTarget.position;

            // 恢复原始尺寸，方便重新测试
            glow.transform.localScale =
                startScales[i];

            glow.SetActive(false);
        }
    }

    private void HideSeedGlow()
    {
        if (seedGlowRing != null &&
            seedGlowRing != gameObject)
        {
            seedGlowRing.SetActive(false);
        }
    }

    private void HideAllFlyingGlowEffects()
    {
        if (convergingEnergies == null)
            return;

        foreach (ConvergingEnergy energy
                 in convergingEnergies)
        {
            if (energy != null &&
                energy.flyingGlowEffect != null)
            {
                energy.flyingGlowEffect.SetActive(false);
            }
        }
    }

    private void RestartParticleSystems(GameObject effectObject)
    {
        if (effectObject == null)
            return;

        // 先开启整个特效根对象
        effectObject.SetActive(true);

        ParticleSystem[] particleSystems =
            effectObject.GetComponentsInChildren<ParticleSystem>(true);

        /*
         * GetComponentsInChildren(true) 可以找到隐藏子物体中的粒子系统，
         * 但隐藏的 GameObject 仍然需要主动开启。
         */
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
                continue;

            if (!particleSystem.gameObject.activeSelf)
            {
                particleSystem.gameObject.SetActive(true);
            }
        }

        // 先全部清空，避免残留上一次的粒子
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
                continue;

            particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        // 再统一重新播放
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
                continue;

            particleSystem.Play(true);
        }
    }

    private bool PointerDown(
        out Vector2 position)
    {
        position = Vector2.zero;

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                position =
                    touch.position.ReadValue();

                return true;
            }
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame)
        {
            position =
                Mouse.current.position.ReadValue();

            return true;
        }

        return false;
    }
}