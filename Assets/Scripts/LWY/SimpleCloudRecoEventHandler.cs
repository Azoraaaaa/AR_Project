using UnityEngine;
using Vuforia;

public class SimpleCloudRecoEventHandler : MonoBehaviour
{
    CloudRecoBehaviour mCloudRecoBehaviour;
    bool mIsScanning = false;
    string mTargetMetadata = "";
    GameObject mCurrentPageContent;

    [Header("Vuforia")]
    public ImageTargetBehaviour ImageTargetTemplate;

    [Header("Story Pages")]
    public PageContent[] Pages;
    public AudioSource NarrationAudioSource;

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
    }
    //Unregister cloud reco callbacks when the handler is destroyed
    void OnDestroy()
    {
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
            ClearCurrentPage();
        }
    }

    // Here we handle a cloud target recognition event
    public void OnNewSearchResult(CloudRecoBehaviour.CloudRecoSearchResult cloudRecoSearchResult)
    {
        // Store the target metadata
        mTargetMetadata = cloudRecoSearchResult.MetaData;

        // Stop the scanning by disabling the behaviour
        mCloudRecoBehaviour.enabled = false;

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
