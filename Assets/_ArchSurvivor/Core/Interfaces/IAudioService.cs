namespace _ArchSurvivor.Core.Interfaces {
    public interface IAudioService {
        void PlaySFX(string assetKey);
        void PlayBGM(string assetKey, bool loop = true);
        void StopBGM();
        void SetMasterVolume(float volume);
        void SetSFXVolume(float volume);
        void SetBGMVolume(float volume);
    }
}
