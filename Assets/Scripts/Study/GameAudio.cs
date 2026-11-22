using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    public AudioSource music;
    public AudioSource effects;
    public AudioClip ambience;
    public AudioClip cluePickup;
    public AudioClip clueAnalyse;
    public AudioClip suspectTalk;

    void Awake()
    {
        Instance = this;
        if (music == null)
            music = gameObject.AddComponent<AudioSource>();
        if (effects == null)
        {
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
        }

        music.playOnAwake = false;
        music.loop = true;
        music.volume = 0.28f;
        effects.volume = 0.7f;
    }

    public void StartMusic()
    {
        if (ambience == null)
            return;
        music.clip = ambience;
        if (!music.isPlaying)
            music.Play();
    }

    public void StopMusic()
    {
        if (music != null)
            music.Stop();
    }

    public void PauseMusic(bool pause)
    {
        if (music == null || ambience == null)
            return;
        if (pause)
            music.Pause();
        else if (!music.isPlaying)
            music.UnPause();
    }

    public void PlayCluePickup()
    {
        Play(cluePickup);
    }

    public void PlayClueAnalyse()
    {
        Play(clueAnalyse);
    }

    public void PlaySuspectTalk()
    {
        Play(suspectTalk);
    }

    void Play(AudioClip clip)
    {
        if (effects == null || clip == null)
            return;
        effects.PlayOneShot(clip);
    }
}
