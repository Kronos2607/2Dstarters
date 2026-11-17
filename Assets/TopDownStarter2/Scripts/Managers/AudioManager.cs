// The AudioManager plays the music and the sound effects, and sets their volumes through an Audio Mixer.
// Like the GameManager there is one of it, it survives scene loads, and any script can reach it with
// AudioManager.Instance, for example: AudioManager.Instance.PlaySound(pickupSound);
using UnityEngine;
using UnityEngine.Audio;

namespace TopDownStarter
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer mixer;

        // Two AudioSources: one loops the music, the other plays short effects. Their Output is set to the
        // Music and SFX groups of the mixer, so each has its own volume.
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource effectsSource;

        [SerializeField] private AudioClip startingMusic;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Saved volumes from last time. The mixer ignores SetFloat in Awake, so this has to be in Start.
            ApplyVolume("MusicVolume", GetVolume("MusicVolume"));
            ApplyVolume("SFXVolume", GetVolume("SFXVolume"));

            if (startingMusic != null)
            {
                PlayMusic(startingMusic);
            }
        }

        public void PlayMusic(AudioClip music)
        {
            // Already playing this track (for example after walking into the next area): let it carry on.
            if (musicSource.clip == music && musicSource.isPlaying)
            {
                return;
            }
            musicSource.clip = music;
            musicSource.Play();
        }

        // PlayOneShot lets sounds overlap: collecting two gems quickly plays two gem sounds.
        public void PlaySound(AudioClip sound)
        {
            if (sound == null)
            {
                return;
            }
            effectsSource.PlayOneShot(sound);
        }

        // Volume from a slider (0 to 1). It is saved in PlayerPrefs, so it's the same next time the game runs.
        public void SetVolume(string exposedParameter, float sliderValue)
        {
            PlayerPrefs.SetFloat(exposedParameter, sliderValue);
            ApplyVolume(exposedParameter, sliderValue);
        }

        // The saved slider value, or 1 (full volume) if nothing has been saved yet.
        public float GetVolume(string exposedParameter)
        {
            return PlayerPrefs.GetFloat(exposedParameter, 1f);
        }

        // The mixer works in decibels (-80 to 0), so convert the 0 to 1 slider value.
        private void ApplyVolume(string exposedParameter, float sliderValue)
        {
            // Log10(0) can't be worked out, so keep the slider value just above zero.
            if (sliderValue < 0.0001f)
            {
                sliderValue = 0.0001f;
            }
            float decibels = Mathf.Log10(sliderValue) * 20f;
            mixer.SetFloat(exposedParameter, decibels);
        }
    }
}
