using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class SeedTapController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera arCamera;

    [Header("Click Detection")]
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    [Header("Visual")]
    [Tooltip("必须是 PlantedSeed 的子物体，不要拖入 PlantedSeed 自身")]
    [SerializeField] private GameObject glowRing;

    [Header("Activation Audio")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip activationClip;

    [Range(0f, 1f)]
    [SerializeField] private float activationVolume = 1f;

    [Header("Timing")]
    [SerializeField] private float glowDuration = 2f;
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

        HideGlowRing();
    }

    private void OnEnable()
    {
        /*
         * PlantedSeed 最开始是隐藏的。
         * 当种子放入花盆、PlantedSeed 被激活时，
         * OnEnable 会再次确保光圈处于隐藏状态。
         */
        HideGlowRing();
    }

    private void Update()
    {
        if (!interactable || hasBeenTapped || isProcessing)
            return;

        if (PointerDown(out Vector2 screenPosition))
        {
            TryTapSeed(screenPosition);
        }
    }

    private void FindCamera()
    {
        if (arCamera == null)
            arCamera = Camera.main;

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

        Debug.Log(
            $"SeedTapController: Interactable = {value}"
        );

        /*
         * 开放点击不等于显示光圈。
         * 光圈只在玩家真正点击种子后出现。
         */
        if (!value)
        {
            HideGlowRing();
        }
    }

    private void TryTapSeed(Vector2 screenPosition)
    {
        if (arCamera == null)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

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

        // 玩家点击后，光圈才出现
        if (glowRing != null)
        {
            glowRing.SetActive(true);
        }

        if (sfxAudioSource != null &&
            activationClip != null)
        {
            sfxAudioSource.PlayOneShot(
                activationClip,
                activationVolume
            );
        }

        // 制造“种子似乎即将开花”的期待
        yield return new WaitForSecondsRealtime(
            glowDuration
        );

        // 最后什么都没有发生
        HideGlowRing();

        if (pauseBeforeFinalDialogue > 0f)
        {
            yield return new WaitForSecondsRealtime(
                pauseBeforeFinalDialogue
            );
        }

        // 显示最终故事文本、播放配音，并保持不消失
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

    private void HideGlowRing()
    {
        if (glowRing != null &&
            glowRing != gameObject)
        {
            glowRing.SetActive(false);
        }
    }

    private bool PointerDown(out Vector2 position)
    {
        position = Vector2.zero;

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                position = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position =
                Mouse.current.position.ReadValue();

            return true;
        }

        return false;
    }
}