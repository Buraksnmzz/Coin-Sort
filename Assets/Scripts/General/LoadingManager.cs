using System.Collections;
using System.IO;
using General.EventDispatcher;
using Localization;
using RemoteConfig;
using SavedData;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace General
{
    public class LoadingManager : MonoBehaviour
    {
        [SerializeField] private GameObject loadingVisualRoot;
        [SerializeField] private Image loadingBackgroundImage;
        [SerializeField] private Image loadingBarFillImage;
        [SerializeField] private float fakeProgressDuration = 4f;
        [SerializeField] private float maxFakeProgress = 0.9f;
        [SerializeField] private float completionDuration = 0.2f;

        private readonly string _gameSceneName = "MainScene";
        private float _displayedProgress;
        private bool _isReadyToComplete;

        private void Awake()
        {
            ServiceLocator.Register<IEventDispatcherService>(new EventDispatcherService());
            ServiceLocator.Register<ISavedDataService>(new SavedDataService());
            ServiceLocator.Register<IRemoteConfigService>(new RemoteConfigService());
            ServiceLocator.Register<ILocalizationService>(new LocalizationService());
        }

        private void Start()
        {
            StartCoroutine(InitializeGame());
        }

        private IEnumerator InitializeGame()
        {
            var timeoutTimer = 0f;
            const float maxWaitTime = 5f;

            YoogoLabManager.TryStartGame(() =>
            {
                StartCoroutine(WaitForRemoteConfig());
            });

            yield return StartCoroutine(UpdateLoadingProgress());

            SceneManager.LoadScene(_gameSceneName);

            IEnumerator WaitForRemoteConfig()
            {
                while (!YoogoLabManager.IsRemoteConfigReady() && timeoutTimer < maxWaitTime)
                {
                    timeoutTimer += Time.deltaTime;
                    yield return null;
                }

                var rawJson = YoogoLabManager.IsRemoteConfigReady()
                    ? YoogoLabManager.GetRemoteConfig()
                    : null;

                if (!string.IsNullOrWhiteSpace(rawJson) && rawJson != "{}")
                {
                    Debug.Log("[RemoteConfig] Raw RC response: " + rawJson);
                    var remoteConfigService = ServiceLocator.GetService<IRemoteConfigService>();
                    remoteConfigService.ApplyFromRemoteConfigJson(rawJson);
                }
                else
                {
                    Debug.LogWarning("[RemoteConfig] RC response empty, using defaults.");
                }

                _isReadyToComplete = true;
            }
        }

        private IEnumerator UpdateLoadingProgress()
        {
            while (!_isReadyToComplete)
            {
                var targetProgress = Mathf.Clamp01((Time.timeSinceLevelLoad / fakeProgressDuration) * maxFakeProgress);
                SetLoadingProgress(Mathf.MoveTowards(_displayedProgress, targetProgress, Time.deltaTime / fakeProgressDuration));
                yield return null;
            }

            while (_displayedProgress < 1f)
            {
                SetLoadingProgress(Mathf.MoveTowards(_displayedProgress, 1f, Time.deltaTime / completionDuration));
                yield return null;
            }
        }

        private void SetLoadingProgress(float progress)
        {
            _displayedProgress = Mathf.Clamp01(progress);

            if (loadingBarFillImage != null)
            {
                loadingBarFillImage.fillAmount = _displayedProgress;
            }
        }

        private void SetLoadingVisualState(bool isVisible)
        {
            if (loadingVisualRoot != null)
            {
                loadingVisualRoot.SetActive(isVisible);
            }

            if (loadingBackgroundImage != null)
            {
                loadingBackgroundImage.enabled = isVisible;
            }

            if (loadingBarFillImage != null)
            {
                loadingBarFillImage.enabled = isVisible;
            }
        }

        private string ReadDefaultRemoteConfigJson()
        {
            var path = Path.Combine(Application.dataPath, "Scripts", "remote_config_defaults.json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
