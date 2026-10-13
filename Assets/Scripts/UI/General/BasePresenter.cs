namespace UI.General
{
    public abstract class BasePresenter<TView> : IPresenter where TView : IView
    {
        protected TView View { get; private set; }

        public virtual void Initialize(IView view)
        {
            if (view is TView typedView)
            {
                View = typedView;
                OnInitialize();
            }
            else
            {
                throw new System.InvalidOperationException($"View type mismatch. Expected {typeof(TView)}, got {view.GetType()}");
            }
        }
        
        protected virtual void OnInitialize() { }

        public virtual void ViewShown() { }

        public virtual void ViewHidden() { }

        public virtual void Cleanup()
        {
            View = default;
        }
    }
}