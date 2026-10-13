using System;

namespace General
{
    public interface IAdsService: IService
    {
        bool IsRewardedAvailable();
        void GetReward(Action<bool> callback);
    }
}