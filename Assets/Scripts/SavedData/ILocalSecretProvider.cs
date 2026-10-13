namespace SavedData
{
    public interface ILocalSecretProvider
    {
        string GetOrCreateSecret();
    }
}