// Put this on a UI Slider (0 to 1) to control a volume in the AudioManager's mixer.
// Exposed Parameter is MusicVolume or SFXVolume (the names exposed in MainMixer).
using UnityEngine;
using UnityEngine.UI;

namespace TopDownStarter
{
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private string exposedParameter = "MusicVolume";

        private Slider slider;

        private void Start()
        {
            slider = GetComponent<Slider>();

            // Start at the saved volume, then listen for the player moving the slider.
            slider.value = AudioManager.Instance.GetVolume(exposedParameter);
            slider.onValueChanged.AddListener(OnSliderMoved);
        }

        private void OnSliderMoved(float value)
        {
            AudioManager.Instance.SetVolume(exposedParameter, value);
        }
    }
}
