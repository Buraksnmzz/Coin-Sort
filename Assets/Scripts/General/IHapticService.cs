namespace General
{
    public interface IHapticService: IService
    {
        void HapticLow();
        void HapticMedium();
        void HapticHigh();
        void HapticMin();
    }
}