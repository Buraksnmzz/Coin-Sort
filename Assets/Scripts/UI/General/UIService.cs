using System;
using System.Collections.Generic;
using General;
using Sound;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UI.General
{
    public class UIService : IUIService
    {
        private readonly Dictionary<Type, IPresenter> _activePresenters = new Dictionary<Type, IPresenter>();
        private readonly Dictionary<Type, IView> _activeViews = new Dictionary<Type, IView>();
        private readonly Dictionary<Type, string> _presenterViewPathCache = new Dictionary<Type, string>();

        private readonly Transform _uiRoot;
        private ISoundService _soundService;
        private IHapticService _hapticService;

        public UIService(Transform uiRoot)
        {
            _uiRoot = uiRoot;
            _soundService = ServiceLocator.GetService<ISoundService>();
            _hapticService = ServiceLocator.GetService<IHapticService>();
        }

        public T ShowPopup<T>(bool shouldPlaySound, bool shouldAnimate) where T : class, IPresenter, new()
        {
            return ShowPopup<T>(shouldPlaySound, null, shouldAnimate);
        }

        public T ShowPopup<T>(bool shouldPlaySound = true, PopupAnimationType? animationOverride = null, bool shouldAnimate = true) where T : class, IPresenter, new()
        {
            var presenterType = typeof(T);

            if (_activePresenters.TryGetValue(presenterType, out var existingPresenter))
            {
                if (_activeViews.TryGetValue(presenterType, out var existingView))
                {
                    ApplyShowSettings(existingView, animationOverride, shouldAnimate);
                    existingView.Show();
                    ((T)existingPresenter).ViewShown();
                }

                return (T)existingPresenter;
            }

            var view = CreateViewForPresenter(presenterType);
            if (view == null)
                return null;

            var presenter = new T();
            presenter.Initialize(view);
            _activePresenters[presenterType] = presenter;
            _activeViews[presenterType] = view;

            ApplyShowSettings(view, animationOverride, shouldAnimate);
            view.Show();
            presenter.ViewShown();
            return presenter;
        }

        public T ShowPopup<T, TData>(TData data, bool shouldAnimate) where T : class, IPresenterWithData<TData>, new()
        {
            return ShowPopup<T, TData>(data, null, shouldAnimate);
        }

        public T ShowPopup<T, TData>(TData data, PopupAnimationType? animationOverride = null, bool shouldAnimate = true) where T : class, IPresenterWithData<TData>, new()
        {
            var presenterType = typeof(T);

            if (_activePresenters.TryGetValue(presenterType, out var existingPresenter))
            {
                var typedPresenter = (T)existingPresenter;
                typedPresenter.SetData(data);

                if (_activeViews.TryGetValue(presenterType, out var existingView))
                {
                    ApplyShowSettings(existingView, animationOverride, shouldAnimate);
                    existingView.Show();
                    typedPresenter.ViewShown();
                }

                return typedPresenter;
            }

            var view = CreateViewForPresenter(presenterType);
            if (view == null)
            {
                return null;
            }

            var presenter = new T();
            presenter.Initialize(view);
            presenter.SetData(data);
            _activePresenters[presenterType] = presenter;
            _activeViews[presenterType] = view;

            ApplyShowSettings(view, animationOverride, shouldAnimate);
            view.Show();
            presenter.ViewShown();
            return presenter;
        }

        public void HidePopup<T>(bool shouldAnimate) where T : class, IPresenter
        {
            HidePopup<T>(null, shouldAnimate);
        }

        public void HidePopup<T>(PopupAnimationType? animationOverride = null, bool shouldAnimate = true) where T : class, IPresenter
        {
            var presenterType = typeof(T);

            if (_activePresenters.TryGetValue(presenterType, out var presenter) &&
                _activeViews.TryGetValue(presenterType, out var view))
            {
                ApplyHideSettings(view, animationOverride, shouldAnimate);
                view.Hide();
                presenter.ViewHidden();
            }
        }

        public void HideAllPopups()
        {
            foreach (var presenterPair in new Dictionary<Type, IPresenter>(_activePresenters))
            {
                if (_activeViews.TryGetValue(presenterPair.Key, out var view))
                {
                    view.Hide();
                    presenterPair.Value.ViewHidden();
                }
            }
        }

        private IView CreateViewForPresenter(Type presenterType)
        {
            var viewType = GetViewTypeForPresenter(presenterType);
            if (viewType == null)
            {
                return null;
            }

            var existingSceneView = FindExistingSceneView(viewType);
            if (existingSceneView != null)
            {
                return existingSceneView;
            }

            var all = Resources.LoadAll<GameObject>("UI");
            foreach (var prefab in all)
            {
                if (prefab != null && prefab.GetComponent(viewType) != null)
                {
                    var resourcePath = "UI/" + prefab.name;
                    var v = InstantiateViewPrefab(prefab, viewType);
                    if (v != null)
                    {
                        _presenterViewPathCache[presenterType] = resourcePath;
                        return v;
                    }
                }
            }
            return null;
        }

        private IView FindExistingSceneView(Type viewType)
        {
            var components = _uiRoot.GetComponentsInChildren(viewType, true);
            foreach (var component in components)
            {
                if (component is IView view)
                {
                    return view;
                }
            }

            return null;
        }

        private Type GetViewTypeForPresenter(Type presenterType)
        {
            var currentType = presenterType;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(BasePresenter<>))
                {
                    var genericArguments = currentType.GetGenericArguments();
                    if (genericArguments.Length == 1)
                    {
                        return genericArguments[0];
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private IView InstantiateViewPrefab(GameObject prefab, Type viewType)
        {
            var viewInstance = Object.Instantiate(prefab, _uiRoot);
            var view = viewInstance.GetComponent(viewType) as IView;
            if (view == null)
            {
                Object.Destroy(viewInstance);
                return null;
            }
            return view;
        }

        private static void ApplyShowSettings(IView view, PopupAnimationType? animationOverride, bool shouldAnimate)
        {
            if (view is not BaseView baseView)
            {
                return;
            }

            baseView.SetNextShowShouldAnimate(shouldAnimate);

            if (animationOverride != null)
            {
                baseView.SetNextShowAnimationOverride(animationOverride.Value);
            }
        }

        private static void ApplyHideSettings(IView view, PopupAnimationType? animationOverride, bool shouldAnimate)
        {
            if (view is not BaseView baseView)
            {
                return;
            }

            baseView.SetNextHideShouldAnimate(shouldAnimate);

            if (animationOverride != null)
            {
                baseView.SetNextHideAnimationOverride(animationOverride.Value);
            }
        }

        public void Dispose()
        {
            foreach (var presenter in _activePresenters.Values)
            {
                presenter.Cleanup();
            }

            foreach (var view in _activeViews.Values)
            {
                if (view is MonoBehaviour viewComponent)
                {
                    Object.Destroy(viewComponent.gameObject);
                }
            }

            _activePresenters.Clear();
            _activeViews.Clear();
            _presenterViewPathCache.Clear();
        }
    }
}
