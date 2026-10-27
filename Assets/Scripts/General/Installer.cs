using Collectible;
using Core.Scripts.Services;
using General;
using Gameplay;
using IAP;
using UnityEngine;
using RemoteConfig;
using SavedData;
using Snapshot;
using Sound;
using UI.General;
using UI.Settings;

public class Installer : MonoBehaviour
{
    [Header("UI References")]
    public Transform uiRoot;

    void Awake()
    {
        Vibration.Init();
    }

    private void Start()
    {
        InstallServices();
        SetOptimalFrameRate();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ServiceLocator.GetService<ILevelProgressService>().Flush();
        }
    }

    private void OnApplicationQuit()
    {
        ServiceLocator.GetService<ILevelProgressService>().Flush();
    }

    private void SetOptimalFrameRate()
    {
        var memoryMb = SystemInfo.systemMemorySize;
        var processorCount = SystemInfo.processorCount;

        var isLowEndDevice = memoryMb <= 2100 || processorCount <= 4;

        if (isLowEndDevice)
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }
    }

    private void InstallServices()
    {
        ServiceLocator.Register<ISoundService>(new SoundService());
        ServiceLocator.Register<IHapticService>(new HapticService());
        ServiceLocator.Register<IUIService>(new UIService(uiRoot));
        ServiceLocator.Register<ISnapshotService>(new SnapshotService());
        ServiceLocator.Register<ICollectibleService>(new CollectibleService());
        ServiceLocator.Register<IIAPService>(new IAPService());
        ServiceLocator.Register<IAdsService>(new AdsService());
        ServiceLocator.Register<IBoardGenerationService>(new BoardGenerationService());
        ServiceLocator.Register<ILevelProgressService>(new LevelProgressService());
        var savedDataService = ServiceLocator.GetService<ISavedDataService>();
        if (!savedDataService.GetModel<SettingsModel>().IsNoAds)
            YoogoLabManager.ShowBanner();

        var uiService = ServiceLocator.GetService<IUIService>();
        var soundService = ServiceLocator.GetService<ISoundService>();
        soundService.PlayMusic();
        
        uiService.ShowPopup<GameplayPresenter>(false, false);
    }

}
