using General;

namespace Sound
{
    public interface ISoundService : IService
    {
        void PlaySound(ClipName clipName, float delay = 0);
        void PlayRandomSound(ClipName firstClipName, ClipName secondClipName, float delay = 0);
        void PlayMusic();
        void StopMusic();
    }
}