using System.Collections.Generic;
using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using OutOfMoves;
using SavedData;
using UI.General;
using UI.Shop;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private const int EasyDealLastLevel = 21;

        private IBoardGenerationService _boardGenerationService;
        private ILevelProgressService _levelProgressService;
        private IEventDispatcherService _eventDispatcherService;
        private ISavedDataService _savedDataService;
        private int _currentBoardIndex;
        private BoardState _levelStartSnapshot;

        protected override void OnInitialize()
        {
            _boardGenerationService = ServiceLocator.GetService<IBoardGenerationService>();
            _levelProgressService = ServiceLocator.GetService<ILevelProgressService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _currentBoardIndex = _levelProgressService.GetBoardIndex();

            View.NextBoardButtonClicked += OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked += OnPreviousBoardButtonClicked;
            View.AutoPlayButtonClicked += OnAutoPlayButtonClicked;
            View.StopAutoPlayButtonClicked += OnStopAutoPlayButtonClicked;
            View.CoinsCreated += OnCoinsCreated;
            View.BoardStateChanged += OnBoardStateChanged;
            _eventDispatcherService.AddListener<RestartLevelSignal>(OnRestartLevel);
            _eventDispatcherService.AddListener<ShuffleBoardSignal>(OnShuffleBoard);

            ShowBoard();
            CaptureLevelStartSnapshot();
        }

        public override void Cleanup()
        {
            View.NextBoardButtonClicked -= OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked -= OnPreviousBoardButtonClicked;
            View.AutoPlayButtonClicked -= OnAutoPlayButtonClicked;
            View.StopAutoPlayButtonClicked -= OnStopAutoPlayButtonClicked;
            View.CoinsCreated -= OnCoinsCreated;
            View.BoardStateChanged -= OnBoardStateChanged;
            _eventDispatcherService.RemoveListener<RestartLevelSignal>(OnRestartLevel);
            _eventDispatcherService.RemoveListener<ShuffleBoardSignal>(OnShuffleBoard);
            base.Cleanup();
        }

        private void OnNextBoardButtonClicked()
        {
            _currentBoardIndex++;
            _levelProgressService.SetDebugBoard(_currentBoardIndex);
            ShowBoard();
            CaptureLevelStartSnapshot();
        }

        private void OnPreviousBoardButtonClicked()
        {
            if (_currentBoardIndex == 1)
            {
                return;
            }

            _currentBoardIndex--;
            _levelProgressService.SetDebugBoard(_currentBoardIndex);
            ShowBoard();
            CaptureLevelStartSnapshot();
        }

        private void OnAutoPlayButtonClicked()
        {
            View.StartAutoPlay();
        }

        private void OnStopAutoPlayButtonClicked()
        {
            View.StopAutoPlay();
        }

        private void ShowBoard()
        {
            var generatedBoardState = _boardGenerationService.CreateBoard(_currentBoardIndex, _levelProgressService.CurrentBoardLevel);
            var boardState = _levelProgressService.RestoreBoardState(generatedBoardState);
            View.SetBoard(boardState);
            var isNewBoard = _levelProgressService.InitializeBoard(_currentBoardIndex, View.GetHighestBoardTier());

            if (isNewBoard)
            {
                _levelProgressService.SaveBoardState(View.GetCurrentBoardState());
            }

            UpdateDealType();
            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);
        }

        private void OnBoardStateChanged(BoardState boardState)
        {
            _levelProgressService.SaveBoardState(boardState);
        }

        private void OnCoinsCreated(IReadOnlyList<int> tiers)
        {
            var levelCompleted = false;

            foreach (var tier in tiers)
            {
                levelCompleted |= _levelProgressService.RegisterCoinCreated(tier);
            }

            UpdateLevel(levelCompleted);

            if (!View.CanContinuePlaying())
            {
                ServiceLocator.GetService<IUIService>().ShowPopup<OutOfMovesPresenter>();
            }
        }

        private void UpdateLevel(bool levelCompleted)
        {
            // if (levelCompleted)
            // {
            //     View.StopAutoPlay();
            // }

            var nextBoardIndex = _levelProgressService.GetBoardIndex();

            if (nextBoardIndex != _currentBoardIndex)
            {
                _currentBoardIndex = nextBoardIndex;
                ShowBoard();

                if (levelCompleted)
                {
                    CaptureLevelStartSnapshot();
                    _levelProgressService.Flush();
                }

                return;
            }

            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);

            if (levelCompleted)
            {
                UpdateDealType();
                View.UnlockSlotsForLevel(_levelProgressService.CurrentBoardLevel);
                CaptureLevelStartSnapshot();
                _levelProgressService.Flush();
            }
        }

        private void OnRestartLevel(RestartLevelSignal signal)
        {
            _levelProgressService.ResetLevelProgress();
            View.SetBoard(CloneBoardState(_levelStartSnapshot));
            _levelProgressService.SaveBoardState(View.GetCurrentBoardState());
            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);
            _levelProgressService.Flush();
        }

        private void OnShuffleBoard(ShuffleBoardSignal signal)
        {
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            var shuffleCost = _savedDataService.GetModel<RemoteConfigModel>().shuffleCost;

            if (collectibleModel.TotalCoins <= shuffleCost)
            {
                return;
            }

            collectibleModel.TotalCoins -= shuffleCost;
            _savedDataService.SaveData(collectibleModel);
            _eventDispatcherService.Dispatch(new CoinChangedSignal());
            View.ShuffleBoard();
            _levelProgressService.SaveBoardState(View.GetCurrentBoardState());
            _levelProgressService.Flush();
            ServiceLocator.GetService<IUIService>().HidePopup<OutOfMovesPresenter>();
            ServiceLocator.GetService<IUIService>().HidePopup<LosePresenter>();
        }

        private void CaptureLevelStartSnapshot()
        {
            _levelStartSnapshot = CloneBoardState(View.GetCurrentBoardState());
        }

        private void UpdateDealType()
        {
            View.SetUsesEasyDeal(_levelProgressService.CurrentLevel <= EasyDealLastLevel);
        }

        private static BoardState CloneBoardState(BoardState boardState)
        {
            var slots = new List<SlotState>(boardState.Slots.Count);

            foreach (var slotState in boardState.Slots)
            {
                var coins = new List<CoinState>(slotState.Coins.Count);

                foreach (var coinState in slotState.Coins)
                {
                    coins.Add(new CoinState(coinState.Tier, coinState.Type, coinState.IsHiddenGroupCenter));
                }

                slots.Add(new SlotState(
                    slotState.UnlockCoinCost,
                    slotState.AutomaticUnlockLevel,
                    slotState.Type,
                    coins));
            }

            return new BoardState(boardState.BoardIndex, slots);
        }
    }
}