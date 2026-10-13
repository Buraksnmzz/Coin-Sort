namespace UI.General
{
    public interface IPresenter
    {
        void Initialize(IView view);
        void ViewShown();
        void ViewHidden();
        void Cleanup();
    }
}