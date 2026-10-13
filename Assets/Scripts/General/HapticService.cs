using SavedData;
using UI.Settings;
using UnityEngine;

namespace General
{
    public class HapticService : IHapticService
    {
        private readonly ISavedDataService _savedDataService;
        
        public HapticService()
        {
            Debug.Log("HapticService initialized");
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
        }

        public void HapticMin()
        {
            if (_savedDataService.GetModel<SettingsModel>().IsHapticOn)
            {
#if UNITY_IOS
                Vibration.VibrateIOS(ImpactFeedbackStyle.Light);
                return;
#endif
                Vibration.VibratePop();
            }
        }
       
        public void HapticLow()
        {
            if(_savedDataService.GetModel<SettingsModel>().IsHapticOn)
            {
                Vibration.VibratePop();
            }
        }
        
        public void HapticMedium()
        {
            if(_savedDataService.GetModel<SettingsModel>().IsHapticOn)
            {
                Vibration.VibratePeek();
            }
        }
        
        public void HapticHigh()
        {
            if(_savedDataService.GetModel<SettingsModel>().IsHapticOn)
            {
                Vibration.Vibrate();
            }
        }

        public void Dispose()
        {
            
        }
    }
}