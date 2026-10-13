namespace SavedData
{
    public interface ILocalDataProtector
    {
        string Protect(string plainText);
        bool TryUnprotect(string protectedText, out string plainText);
    }
}