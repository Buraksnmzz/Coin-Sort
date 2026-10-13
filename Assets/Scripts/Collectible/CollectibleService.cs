using General;
using General.EventDispatcher;
using SavedData;

namespace Collectible
{
    public class CollectibleService : ICollectibleService
    {
        private readonly ISavedDataService _savedDataService;
        private readonly IEventDispatcherService _eventDispatcher;
        private readonly CollectibleModel _model;

        public CollectibleService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _eventDispatcher = ServiceLocator.GetService<IEventDispatcherService>();
            _model = _savedDataService.LoadData<CollectibleModel>();
        }
    }
}