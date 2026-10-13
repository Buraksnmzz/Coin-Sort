namespace UI.General
{
    
    public abstract class BasePresenterWithData<TView, TData> : BasePresenter<TView>, IPresenterWithData<TData>
        where TView : IView
    {
        protected TData Data { get; private set; }

        public virtual void SetData(TData data)
        {
            Data = data;
            OnDataSet();
        }
        
        protected virtual void OnDataSet() { }

        public override void Cleanup()
        {
            base.Cleanup();
            Data = default;
        }
    }
}