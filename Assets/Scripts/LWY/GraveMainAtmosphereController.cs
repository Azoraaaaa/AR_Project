using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class GraveMainAtmosphereController : MonoBehaviour
{
    [Header("Flower Task")]
    [SerializeField] private FlowerTaskInteractionController flowerTaskController;
    [SerializeField] private int totalTaskCount = 4;

    [Header("Rain Audio")]
    [SerializeField] private AudioSource rainAudioSource;
    [SerializeField] private AudioClip rainClip;
    [Range(0f, 1f)]
    [SerializeField] private float rainStartVolume = 1f;
    [SerializeField] private bool playRainOnStart = true;
    [SerializeField] private bool resumeRainAfterEachTask = true;

    [Header("Rain Particles")]
    [SerializeField] private Transform rainParticleRoot;
    [SerializeField] private ParticleSystem[] rainParticleSystems = new ParticleSystem[0];
    [FormerlySerializedAs("rainFinalScaleMultiplier")]
    [SerializeField] private float rainFinalMaxParticlesMultiplier = 0.15f;

    [Header("Directional Light")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float startLightIntensity = 0.4f;
    [SerializeField] private float finalLightIntensity = 1.2f;

    [Header("Transition")]
    [SerializeField] private float transitionSeconds = 0.8f;

    private int completedTaskCount;
    private int[] rainStartMaxParticles = new int[0];
    private Coroutine transitionRoutine;
    private bool subscribed;

    private void Awake()
    {
        ResolveReferences();
        CacheRainParticleSettings();

        ApplyAtmosphereProgress(0f);
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();

        if (playRainOnStart)
            PlayRain();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (flowerTaskController == null)
            flowerTaskController = GetComponent<FlowerTaskInteractionController>();

        if (flowerTaskController == null)
            flowerTaskController = GetComponentInParent<FlowerTaskInteractionController>();

        if (directionalLight == null)
            directionalLight = RenderSettings.sun;

        if ((rainParticleSystems == null || rainParticleSystems.Length == 0) && rainParticleRoot != null)
            rainParticleSystems = rainParticleRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Subscribe()
    {
        if (subscribed || flowerTaskController == null)
            return;

        flowerTaskController.OrbSelected.AddListener(OnOrbSelected);
        flowerTaskController.TaskCompleted.AddListener(OnTaskCompleted);
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || flowerTaskController == null)
            return;

        flowerTaskController.OrbSelected.RemoveListener(OnOrbSelected);
        flowerTaskController.TaskCompleted.RemoveListener(OnTaskCompleted);
        subscribed = false;
    }

    private void OnOrbSelected(int taskIndex)
    {
        StopRain();
    }

    private void OnTaskCompleted(int taskIndex)
    {
        completedTaskCount = Mathf.Clamp(completedTaskCount + 1, 0, Mathf.Max(1, totalTaskCount));
        float progress = completedTaskCount / (float)Mathf.Max(1, totalTaskCount);

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(AtmosphereTransitionRoutine(progress));

        if (resumeRainAfterEachTask && completedTaskCount < totalTaskCount)
            PlayRain();
    }

    private IEnumerator AtmosphereTransitionRoutine(float targetProgress)
    {
        float startProgress = GetCurrentProgressEstimate();
        float elapsed = 0f;

        while (elapsed < transitionSeconds)
        {
            elapsed += Time.deltaTime;
            float t = transitionSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / transitionSeconds);
            float progress = Mathf.Lerp(startProgress, targetProgress, t * t * (3f - 2f * t));
            ApplyAtmosphereProgress(progress);
            yield return null;
        }

        ApplyAtmosphereProgress(targetProgress);
        transitionRoutine = null;
    }

    private float GetCurrentProgressEstimate()
    {
        if (rainAudioSource != null && rainStartVolume > 0f)
        {
            float volumeProgress = 1f - rainAudioSource.volume / rainStartVolume;
            return Mathf.Clamp01(volumeProgress);
        }

        return completedTaskCount / (float)Mathf.Max(1, totalTaskCount);
    }

    private void ApplyAtmosphereProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (rainAudioSource != null)
            rainAudioSource.volume = Mathf.Lerp(rainStartVolume, 0f, progress);

        ApplyRainMaxParticles(progress);

        if (directionalLight != null)
            directionalLight.intensity = Mathf.Lerp(startLightIntensity, finalLightIntensity, progress);
    }

    private void CacheRainParticleSettings()
    {
        if (rainParticleSystems == null)
        {
            rainStartMaxParticles = new int[0];
            return;
        }

        rainStartMaxParticles = new int[rainParticleSystems.Length];
        for (int i = 0; i < rainParticleSystems.Length; i++)
        {
            if (rainParticleSystems[i] == null)
                continue;

            ParticleSystem.MainModule main = rainParticleSystems[i].main;
            rainStartMaxParticles[i] = main.maxParticles;
        }
    }

    private void ApplyRainMaxParticles(float progress)
    {
        if (rainParticleSystems == null || rainStartMaxParticles == null)
            return;

        int count = Mathf.Min(rainParticleSystems.Length, rainStartMaxParticles.Length);
        for (int i = 0; i < count; i++)
        {
            ParticleSystem particleSystem = rainParticleSystems[i];
            if (particleSystem == null)
                continue;

            int startMaxParticles = Mathf.Max(0, rainStartMaxParticles[i]);
            int finalMaxParticles = Mathf.RoundToInt(startMaxParticles * Mathf.Max(0f, rainFinalMaxParticlesMultiplier));
            int currentMaxParticles = Mathf.RoundToInt(Mathf.Lerp(startMaxParticles, finalMaxParticles, progress));

            ParticleSystem.MainModule main = particleSystem.main;
            main.maxParticles = Mathf.Max(0, currentMaxParticles);
        }
    }

    private void PlayRain()
    {
        if (rainAudioSource == null)
            return;

        if (rainClip != null)
            rainAudioSource.clip = rainClip;

        rainAudioSource.loop = true;

        float progress = completedTaskCount / (float)Mathf.Max(1, totalTaskCount);
        rainAudioSource.volume = Mathf.Lerp(rainStartVolume, 0f, progress);

        if (!rainAudioSource.isPlaying && rainAudioSource.volume > 0.001f)
            rainAudioSource.Play();
    }

    private void StopRain()
    {
        if (rainAudioSource != null)
            rainAudioSource.Stop();
    }
}
