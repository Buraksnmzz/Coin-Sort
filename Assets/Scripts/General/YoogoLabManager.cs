using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class YoogoLabManager : MonoBehaviour
{
    public delegate void OnRewardedCompleted(bool success);
    public static OnRewardedCompleted onRewardedCompleted;

    public delegate void OnRewardedReady();
    public static OnRewardedReady onRewardedReady;

    public static void TryStartGame(Action callback)
    {
        callback?.Invoke();
    }

    public static void ShowInterstitial() { }

    public static void PlayRewarded(OnRewardedCompleted callback)
    {
        onRewardedCompleted = callback;
        Debug.Log("Simulating PlayRewarded (dev wrapper)");
    }

public static bool RewardedAvailable(Action onAvailable, Action onUnavailable)
{
    // DEV NOTE: Change this flag manually to simulate ad flow during integration
    bool simulateRewardedAvailable = true;

    if (simulateRewardedAvailable)
    {
        onAvailable?.Invoke();
        return true;
    }
    else
    {
        onUnavailable?.Invoke();
        return false;
    }
}

    public static void SetFirebaseUserProperty(string property, string value) { }

    public static void LogFirebaseEvent(string eventName, params object[] parameters) { }

    public static void IAP(Product product, bool isRestore = false)

    {
        Debug.Log($"Simulated IAP tracked: {product.definition.id}, is restore? {isRestore}");
    }


    public static void Vibrate(int ms)
    {
        Debug.Log($"Simulated Vibration: {ms}ms");
    }

    public static void ShowNativeReview()
    {
        Debug.Log("Simulated Native Review");
    }

    public static void ShowBanner()
    {
#if UNITY_EDITOR
        SimulateEditorBanner();
#endif

    }

    public static void HideBanner()
    {
        #if UNITY_EDITOR
        if (_simulatedBannerCanvas != null)
        {
            _simulatedBannerCanvas.SetActive(false);
        }
#endif
    }

#if UNITY_EDITOR
    private static GameObject _simulatedBannerCanvas;

    /// <summary>
    /// Simulates a banner ad in the Unity Editor for layout and UI testing.
    /// Creates the banner once and toggles its visibility on Show/Hide calls.
    /// Intended only for use in the Editor as a development aid.
    /// </summary>
    private static void SimulateEditorBanner()
    {
        if (_simulatedBannerCanvas != null)
        {
            _simulatedBannerCanvas.SetActive(true);
            return;
        }

        GameObject parentGO = GameObject.FindObjectOfType<YoogoLabManager>()?.gameObject;

        if (parentGO == null)
        {
            Debug.LogWarning("[SimulatedBanner] YoogoLabManager instance not found. Banner will be unparented.");
        }

        _simulatedBannerCanvas = new GameObject("SimulatedBannerCanvas");

        if (parentGO != null)
        {
            _simulatedBannerCanvas.transform.SetParent(parentGO.transform, false);
        }

        var canvas = _simulatedBannerCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        _simulatedBannerCanvas.AddComponent<CanvasScaler>();
        _simulatedBannerCanvas.AddComponent<GraphicRaycaster>();

        var bannerGO = new GameObject("SimulatedBanner");
        bannerGO.transform.SetParent(_simulatedBannerCanvas.transform, false);

        var image = bannerGO.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 0f, 1f, 0.5f); // Magenta

        var rectTransform = image.GetComponent<RectTransform>();

        float bannerHeight = Screen.height * GetBannerHeightPercent();
        float bottomInset = Mathf.Max(0, Screen.safeArea.y);

        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, bottomInset);
        rectTransform.sizeDelta = new Vector2(0f, bannerHeight);
    }
#endif


    public static float GetBannerHeightPercent()
    {
        return 0.08f;
    }

    public static bool IsRemoteConfigReady()
    {
        if (Time.realtimeSinceStartup < 1f)
            return false;
        return true;
    }

    public static string GetRemoteConfig()
    {
        return "{}"; // Simulated empty JSON
    }

    public static void LevelEnd(int id)
    {
        Debug.Log($"Simulated LevelEnd: Level {id}");
    }
}
