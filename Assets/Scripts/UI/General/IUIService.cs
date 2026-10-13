using General;

namespace UI.General
{
    public interface IUIService : IService
    {
        T ShowPopup<T>(bool shouldPlaySound, bool shouldAnimate) where T : class, IPresenter, new();
        T ShowPopup<T>(bool shouldPlaySound = true, PopupAnimationType? animationOverride = null, bool shouldAnimate = true) where T : class, IPresenter, new();
        T ShowPopup<T, TData>(TData data, bool shouldAnimate) where T : class, IPresenterWithData<TData>, new();
        T ShowPopup<T, TData>(TData data, PopupAnimationType? animationOverride = null, bool shouldAnimate = true) where T : class, IPresenterWithData<TData>, new();
        void HidePopup<T>(bool shouldAnimate) where T : class, IPresenter;
        void HidePopup<T>(PopupAnimationType? animationOverride = null, bool shouldAnimate = true) where T : class, IPresenter;
        void HideAllPopups();
    }
}