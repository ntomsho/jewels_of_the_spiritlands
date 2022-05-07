using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JukeboxController : MonoBehaviour
{
    [Header("Songs")]
    [SerializeField] List<AudioClip> songs;
    [SerializeField] List<string> songTitles;
    [SerializeField] TextMeshProUGUI songTitle;

    [Header("UI")]
    [SerializeField] Image trackProgress;
    [SerializeField] Button playButton;
    [SerializeField] Button pauseButton;
    [SerializeField] Button previousButton;
    [SerializeField] Button nextButton;
    
    AudioSource source;
    int currentSongInt;
    bool isPlaying;
    void Awake()
    {
        source = GetComponent<AudioSource>();
        // currentSongInt = Random.Range(0, songs.Count);
        currentSongInt = 0;
        source.clip = songs[currentSongInt];
        SetSongTitle();
        TogglePlaying();
    }

    void Update()
    {
        if (source.isPlaying)
        {
            trackProgress.fillAmount = source.time / source.clip.length;
        }
        if (isPlaying && !source.isPlaying)
        {
            ChangeSong(true);
        }
    }

    public void TogglePlaying()
    {
        if (!isPlaying)
        {
            source.Play();
            isPlaying = true;
            playButton.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(true);
        }
        else
        {
            source.Pause();
            isPlaying = false;
            playButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
        }
    }

    public void ChangeSong(bool inc)
    {
        source.Stop();
        if (inc && currentSongInt == songs.Count - 1)
        {
            currentSongInt = 0;
        }
        else if (!inc && currentSongInt == 0)
        {
            currentSongInt = songs.Count - 1;
        }
        else
        {
            currentSongInt += inc ? 1 : -1;
        }
        source.clip = songs[currentSongInt];
        SetSongTitle();
        playButton.gameObject.SetActive(false);
        pauseButton.gameObject.SetActive(true);
        if (isPlaying)
        {
            source.Play();
        }
    }

    void SetSongTitle()
    {
        songTitle.text = (currentSongInt + 1) + ". " + songTitles[currentSongInt];
    }

    public void ChangeVolume(float val)
    {
        source.volume = val;
    }

    public void ToggleMute()
    {
        source.mute = !source.mute;
        if (source.isPlaying)
        {
            isPlaying = false;
            source.Pause();
        }
    }
}
