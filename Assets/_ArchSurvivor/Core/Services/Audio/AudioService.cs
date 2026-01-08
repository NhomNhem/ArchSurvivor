using _ArchSurvivor.Core.Interfaces;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

namespace _ArchSurvivor.Core.Services.Audio {
    public class AudioService : IAudioService {
        private readonly AudioMixer _mixer;
        private AudioSource _bgmSource;
        private readonly GameObject _audioContainer;

        public AudioService(AudioMixer mixer = null) {
            _mixer = mixer;
            _audioContainer = new GameObject("AudioContainer");
            Object.DontDestroyOnLoad(_audioContainer);
            
            _bgmSource = _audioContainer.AddComponent<AudioSource>();
            
            if (_mixer != null) {
                var groups = _mixer.FindMatchingGroups("BGM");
                if (groups.Length > 0) _bgmSource.outputAudioMixerGroup = groups[0];
            }
        }

        public async void PlaySFX(string assetKey) {
            var clip = await Addressables.LoadAssetAsync<AudioClip>(assetKey);
            if (clip != null) {
                // Simple implementation, should use Pooling in production
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            }
        }

        public async void PlayBGM(string assetKey, bool loop = true) {
            var clip = await Addressables.LoadAssetAsync<AudioClip>(assetKey);
            if (clip != null) {
                _bgmSource.clip = clip;
                _bgmSource.loop = loop;
                _bgmSource.Play();
            }
        }

        public void StopBGM() {
            _bgmSource.Stop();
        }

        public void SetMasterVolume(float volume) => _mixer?.SetFloat("MasterVol", Mathf.Log10(volume) * 20);
        public void SetSFXVolume(float volume) => _mixer?.SetFloat("SFXVol", Mathf.Log10(volume) * 20);
        public void SetBGMVolume(float volume) => _mixer?.SetFloat("BGMVol", Mathf.Log10(volume) * 20);
    }
}
