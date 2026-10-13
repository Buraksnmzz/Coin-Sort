using General;

namespace RemoteConfig
{
    public interface IRemoteConfigService: IService
    {
        bool ApplyFromRemoteConfigJson(string rawJson);
    }
}