using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class SimpleCloudRecoEventHandler : MonoBehaviour
{
    public static SimpleCloudRecoEventHandler Instance { get; private set; }

    CloudRecoBehaviour mCloudRecoBehaviour;
    bool mIsScanning = false;
    string mTargetMetadata = "";
    GameObject mCurrentPageContent;
    Button mRegisteredNextPageButton;
    Coroutine mNextPageCanvasRoutine;

    [Header("Vuforia")]
    public ImageTargetBehaviour ImageTargetTemplate;

    [Header("Story Pages")]
    public PageContent[] Pages;
    public AudioSource NarrationAudioSource;

    [Header("UI Audio")]
    public AudioSource UiAudioSource;
    public AudioClip NextPageButtonClickClip;
    public AudioClip ScanSuccessClip;
    [Range(0f, 1f)]
    public float UiAudioVolume = 1f;

    [Header("Main Canvas UI")]
    public bool AutoFindPageUi = true;
    public GameObject ScanPage;
    public string ScanPageName = "ScanPage";
    public CanvasGroup NextPageCanvas;
    public string NextPageCanvasName = "NextPageCanvas";
    public Button NextPageButton;
    public string NextPageButtonName = "NextPageButton";
    public float NextPageFadeSeconds = 0.5f;
    public bool MainMenuControlsFirstScanPage = true;
    public bool ShowScanPageOnStart = false;
    public bool HideScanPageWhenTargetFound = true;

    [System.Serializable]
    public class PageContent
    {
        public string Metadata;
        public GameObject ContentPrefab;
        public AudioClip NarrationClip;
    }

    // Register cloud reco callbacks
    void Awake()
    {
        Instance = this;

        mCloudRecoBehaviour = GetComponent<CloudRecoBehaviour>();
        if (mCloudRecoBehaviour == null)
        {
            Debug.LogError("SimpleCloudRecoEventHandler requires a CloudRecoBehaviour on the same GameObject.");
            enabled = false;
            return;
        }

        mCloudRecoBehaviour.RegisterOnInitializedEventHandler(OnInitialized);
        mCloudRecoBehaviour.RegisterOnInitErrorEventHandler(OnInitError);
        mCloudRecoBehaviour.RegisterOnUpdateErrorEventHandler(OnUpdateError);
        mCloudRecoBehaviour.RegisterOnStateChangedEventHandler(OnStateChanged);
        mCloudRecoBehaviour.RegisterOnNewSearchResultEventHandler(OnNewSearchResult);

        ResolveUiAudioSource();
        ResolvePageUiReferences();
        RegisterNextPageButtonListener();
        SetNextPageCanvasVisibleInstant(false);

        if (ShowScanPageOnStart && !MainMenuControlsFirstScanPage)
            SetScanPageVisible(true);
    }
    //Unregister cloud reco callbacks when the handler is destroyed
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnregisterNextPageButtonListener();

        if (mCloudRecoBehaviour == null)
            return;

        mCloudRecoBehaviour.UnregisterOnInitializedEventHandler(OnInitialized);
        mCloudRecoBehaviour.UnregisterOnInitErrorEventHandler(OnInitError);
        mCloudRecoBehaviour.UnregisterOnUpdateErrorEventHandler(OnUpdateError);
        mCloudRecoBehaviour.UnregisterOnStateChangedEventHandler(OnStateChanged);
        mCloudRecoBehaviour.UnregisterOnNewSearchResultEventHandler(OnNewSearchResult);
    }

    public void OnInitialized(CloudRecoBehaviour cloudRecoBehaviour)
    {
        Debug.Log("Cloud Reco initialized");
    }

    public void OnInitError(CloudRecoBehaviour.InitError initError)
    {
        Debug.Log("Cloud Reco init error " + initError.ToString());
    }

    public void OnUpdateError(CloudRecoBehaviour.QueryError updateError)
    {
        Debug.Log("Cloud Reco update error " + updateError.ToString());

    }

    public void OnStateChanged(bool scanning)
    {
        mIsScanning = scanning;

        if (scanning)
        {
            if (!MainMenuControlsFirstScanPage)
                SetScanPageVisible(true);

            SetNextPageCanvasVisibleInstant(false);
            ClearCurrentPage();
        }
    }

    // Here we handle a cloud target recognition event
    public void OnNewSearchResult(CloudRecoBehaviour.CloudRecoSearchResult cloudRecoSearchResult)
    {
        PlayUiOneShot(ScanSuccessClip);

        // Store the target metadata
        mTargetMetadata = cloudRecoSearchResult.MetaData;

        // Stop the scanning by disabling the behaviour
        mCloudRecoBehaviour.enabled = false;

        if (HideScanPageWhenTargetFound)
            SetScanPageVisible(false);

        SetNextPageCanvasVisibleInstant(false);

        // Build augmentation based on target 
        if (ImageTargetTemplate)
        {
            /* Enable the new result with the same ImageTargetBehaviour: */
            mCloudRecoBehaviour.EnableObservers(cloudRecoSearchResult, ImageTargetTemplate.gameObject);
        }

        ShowPageContent(mTargetMetadata);
    }

    public void ScanNextPage()
    {
        ResolvePageUiReferences();
        RegisterNextPageButtonListener();
        SetNextPageCanvasVisibleInstant(false);
        SetScanPageVisible(true);

        ClearCurrentPage();
        mTargetMetadata = "";
        mIsScanning = true;

        if (mCloudRecoBehaviour == null)
            return;

        mCloudRecoBehaviour.enabled = true;
    }

    public void RestartScanning()
    {
        ScanNextPage();
    }

    public void ShowNextPageCanvas()
    {
        ResolvePageUiReferences();
        RegisterNextPageButtonListener();

        if (NextPageCanvas == null)
        {
            Debug.LogWarning("SimpleCloudRecoEventHandler: NextPageCanvas is not assigned.");
            return;
        }

        if (mNextPageCanvasRoutine != null)
            StopCoroutine(mNextPageCanvasRoutine);

        mNextPageCanvasRoutine = StartCoroutine(FadeNextPageCanvasRoutine(1f));
    }

    public void HideNextPageCanvasInstant()
    {
        SetNextPageCanvasVisibleInstant(false);
    }

    void OnNextPageButtonClicked()
    {
        PlayUiOneShot(NextPageButtonClickClip);
        ScanNextPage();
    }

    void ShowPageContent(string targetMetadata)
    {
        ClearCurrentPage();

        PageContent page = FindPageContent(targetMetadata);
        if (page == null)
        {
            Debug.LogWarning("No page content configured for metadata: " + targetMetadata);
            return;
        }

        if (page.ContentPrefab != null && ImageTargetTemplate != null)
        {
            mCurrentPageContent = Instantiate(page.ContentPrefab, ImageTargetTemplate.transform, false);
        }

        PlayNarration(page.NarrationClip);
    }

    PageContent FindPageContent(string targetMetadata)
    {
        if (Pages == null)
            return null;

        for (int i = 0; i < Pages.Length; i++)
        {
            if (Pages[i] != null && Pages[i].Metadata == targetMetadata)
                return Pages[i];
        }

        return null;
    }

    void PlayNarration(AudioClip clip)
    {
        if (NarrationAudioSource == null)
            return;

        NarrationAudioSource.Stop();
        NarrationAudioSource.clip = clip;

        if (clip != null)
            NarrationAudioSource.Play();
    }

    void ClearCurrentPage()
    {
        if (mCurrentPageContent != null)
        {
            Destroy(mCurrentPageContent);
            mCurrentPageContent = null;
        }

        if (NarrationAudioSource != null)
        {
            NarrationAudioSource.Stop();
            NarrationAudioSource.clip = null;
        }
    }

    void ResolveUiAudioSource()
    {
        if (UiAudioSource == null)
        {
            AudioSource[] audioSources = GetComponents<AudioSource>();
            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null && audioSources[i] != NarrationAudioSource)
                {
                    UiAudioSource = audioSources[i];
                    break;
                }
            }
        }

        if (UiAudioSource == null)
            UiAudioSource = NarrationAudioSource != null ? NarrationAudioSource : GetComponent<AudioSource>();
    }

    void PlayUiOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        ResolveUiAudioSource();

        if (UiAudioSource != null)
            UiAudioSource.PlayOneShot(clip, UiAudioVolume);
    }

    IEnumerator FadeNextPageCanvasRoutine(float targetAlpha)
    {
        if (NextPageCanvas == null)
            yield break;

        NextPageCanvas.gameObject.SetActive(true);
        NextPageCanvas.interactable = false;
        NextPageCanvas.blocksRaycasts = false;

        float startAlpha = NextPageCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < NextPageFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = NextPageFadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / NextPageFadeSeconds);
            NextPageCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t * t * (3f - 2f * t));
            yield return null;
        }

        bool visible = targetAlpha > 0.001f;
        NextPageCanvas.alpha = targetAlpha;
        NextPageCanvas.interactable = visible;
        NextPageCanvas.blocksRaycasts = visible;

        if (!visible)
            NextPageCanvas.gameObject.SetActive(false);

        mNextPageCanvasRoutine = null;
    }

    void SetNextPageCanvasVisibleInstant(bool visible)
    {
        if (mNextPageCanvasRoutine != null)
        {
            StopCoroutine(mNextPageCanvasRoutine);
            mNextPageCanvasRoutine = null;
        }

        if (NextPageCanvas == null)
            return;

        NextPageCanvas.alpha = visible ? 1f : 0f;
        NextPageCanvas.interactable = visible;
        NextPageCanvas.blocksRaycasts = visible;
        NextPageCanvas.gameObject.SetActive(visible);
    }

    void SetScanPageVisible(bool visible)
    {
        ResolvePageUiReferences();

        if (ScanPage != null)
            ScanPage.SetActive(visible);
    }

    void ResolvePageUiReferences()
    {
        if (!AutoFindPageUi)
            return;

        if (ScanPage == null)
            ScanPage = FindSceneGameObject(ScanPageName);

        if (NextPageCanvas == null)
        {
            GameObject nextPageObject = FindSceneGameObject(NextPageCanvasName);
            if (nextPageObject != null)
            {
                NextPageCanvas = nextPageObject.GetComponent<CanvasGroup>();
                if (NextPageCanvas == null)
                    NextPageCanvas = nextPageObject.AddComponent<CanvasGroup>();
            }
        }

        if (NextPageButton == null)
        {
            GameObject buttonObject = FindSceneGameObject(NextPageButtonName);
            if (buttonObject != null)
                NextPageButton = buttonObject.GetComponent<Button>();
        }
    }

    void RegisterNextPageButtonListener()
    {
        if (NextPageButton == null || mRegisteredNextPageButton == NextPageButton)
            return;

        UnregisterNextPageButtonListener();
        NextPageButton.onClick.AddListener(OnNextPageButtonClicked);
        mRegisteredNextPageButton = NextPageButton;
    }

    void UnregisterNextPageButtonListener()
    {
        if (mRegisteredNextPageButton == null)
            return;

        mRegisteredNextPageButton.onClick.RemoveListener(OnNextPageButtonClicked);
        mRegisteredNextPageButton = null;
    }

    GameObject FindSceneGameObject(string objectName)
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

    void OnGUI()
    {
        // Display current 'scanning' status
        GUI.Box(new Rect(100, 100, 200, 50), mIsScanning ? "Scanning" : "Not scanning");
        // Display metadata of latest detected cloud-target
        GUI.Box(new Rect(100, 200, 200, 50), "Metadata: " + mTargetMetadata);
        // If not scanning, show button
        // so that user can restart cloud scanning
        if (!mIsScanning)
        {
            if (GUI.Button(new Rect(100, 300, 200, 50), "Restart Scanning"))
            {
                ScanNextPage();
            }
        }
    }
}
