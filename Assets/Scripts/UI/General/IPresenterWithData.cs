
namespace UI.General
{
    public interface IPresenterWithData<T>: IPresenter
    {
        void SetData(T data);
    }
}