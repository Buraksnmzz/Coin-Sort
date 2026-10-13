using System.Collections;
using General;
using SavedData;
using Sound;
using UI.Settings;
using UnityEngine;
using UnityEngine.Pool;

namespace Core.Scripts.Services
{
    public class SoundService : ISoundService
    {
        private readonly SoundSo _soundSo;
        private bool _isMusicPlaying;
        private readonly ObjectPool<AudioSource> _audioSourcePool;
        private readonly int _poolSize = 10;
        private readonly ISavedDataService _savedDataService;
        private AudioSource _music;
        private int _partialSoundCounter = 1;

        public SoundService()
        {
            Debug.Log("SoundService initialized");
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _soundSo = Resources.Load<SoundSo>("SoundSo");
            _audioSourcePool = new ObjectPool<AudioSource>(
                createFunc: CreateAudioSource,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: DestroyAudioSource,
                collectionCheck: false,
                defaultCapacity: _poolSize
            );
            InitializeMusic();
        }

        private void InitializeMusic()
        {
            _music = new GameObject().AddComponent<AudioSource>();
            _music.clip = _soundSo.GetAudioClip(ClipName.Music);
            _music.loop = true;
            _music.volume = _soundSo.GetVolume(ClipName.Music);
        }

        private AudioSource CreateAudioSource()
        {
            AudioSource audioSource = new GameObject("PooledAudioSource").AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.gameObject.SetActive(false);
            return audioSource;
        }

        private void OnGet(AudioSource audioSource)
        {
            audioSource.gameObject.SetActive(true);
        }

        private void OnRelease(AudioSource audioSource)
        {
            audioSource.Stop();
            audioSource.gameObject.SetActive(false);
        }

        private void DestroyAudioSource(AudioSource audioSource)
        {
            MonoHelper.Instance.DestroyObject(audioSource.gameObject);
        }

        public void PlaySound(ClipName clipName, float delay = 0)
        {
            if (!_savedDataService.GetModel<SettingsModel>().IsSoundOn)
                return;

            var audioSource = _audioSourcePool.Get();
            var clip = _soundSo.GetAudioClip(clipName);

            if (clip == null)
            {
                _audioSourcePool.Release(audioSource);
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = _soundSo.GetVolume(clipName);
            MonoHelper.Instance.StartCoroutine(PlayRoutine(audioSource, delay));
        }

        public void PlayRandomSound(ClipName firstClipName, ClipName secondClipName, float delay = 0)
        {
            var clipName = Random.Range(0, 2) == 0 ? firstClipName : secondClipName;
            PlaySound(clipName, delay);
        }

        private IEnumerator PlayRoutine(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (audioSource.clip == null)
            {
                _audioSourcePool.Release(audioSource);
                yield break;
            }

            audioSource.Play();
            yield return new WaitForSeconds(audioSource.clip.length);

            audioSource.Stop();
            _audioSourcePool.Release(audioSource);
        }

        public void PlaySoundPartial(ClipName clipName, int interval, float delay)
        {
            if (_partialSoundCounter % interval == 1)
            {
                PlaySound(clipName, delay);
            }
            _partialSoundCounter++;
        }

        private IEnumerator ReturnToPoolAfterPlaying(AudioSource audioSource)
        {
            if (audioSource.clip == null)
            {
                _audioSourcePool.Release(audioSource);
                yield break;
            }

            yield return new WaitForSeconds(audioSource.clip.length);

            audioSource.Stop();
            _audioSourcePool.Release(audioSource);
        }

        public void PlayMusic()
        {
            if (!_music.isPlaying && _savedDataService.GetModel<SettingsModel>().IsMusicOn)
                _music.Play();
        }

        public void StopMusic()
        {
            _music.Stop();
        }

        public void Dispose()
        {

        }
    }
}
