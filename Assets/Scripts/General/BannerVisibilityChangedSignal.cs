using General.EventDispatcher;

namespace General
{
    public class BannerVisibilityChangedSignal: ISignal
    {
        public bool Visible;

        public BannerVisibilityChangedSignal(bool visible)
        {
            Visible = visible;
        }
    }
}