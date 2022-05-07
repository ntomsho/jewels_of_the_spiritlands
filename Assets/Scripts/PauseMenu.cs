using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] Transform buttonTransform;
    [SerializeField] Image menuPanel;
    
    [Header("Audio Controls")]
    [SerializeField] JukeboxController jukeboxController;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider soundVolumeSlider;

    bool expanded;
    bool canClick = true;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void TweenInButtons()
    {
        buttonTransform.DOLocalMoveY(-165f, 0.5f).SetEase(Ease.OutBack).SetDelay(1f);
    }

    public void UpdateSoundVolume()
    {
        GameManager.Instance.ChangeVolume(soundVolumeSlider.value);
    }

    public void UpdateMusicVolume()
    {
        jukeboxController.ChangeVolume(musicVolumeSlider.value);
    }

    public void ToggleSoundMute()
    {
        GameManager.Instance.ToggleMute();
    }

    public void ToggleMusicMute()
    {
        jukeboxController.ToggleMute();
    }

    public void ToggleOptions()
    {
        if (!canClick) return;
        canClick = false;
        expanded = !expanded;
        // Time.timeScale = expanded ? 0f : 1f;
        var sequence = DOTween.Sequence();
        sequence.SetEase(expanded ? Ease.Linear : Ease.InBack);
        sequence.Append(menuPanel.transform.DOScale(expanded ? Vector3.one : Vector3.zero, 0.5f));
        sequence.Join(menuPanel.transform.DOLocalMoveY(expanded ? 25f : -165f, 0.5f));
        sequence.OnComplete(() => canClick = true);
    }
}
