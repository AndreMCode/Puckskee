using UnityEngine;

public class AudioDirector : MonoBehaviour
{
    [Header("SFX Clips")]
    [SerializeField] private AudioClip launchClip;
    [SerializeField] private AudioClip bounceClip;
    [SerializeField] private AudioClip bumperClip;
    [SerializeField] private AudioClip goalClip;

    private AudioSource _sfxSource;

    private void Awake()
    {
        _sfxSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GameEvents.OnPuckLaunched += PlayLaunchSFX;
        GameEvents.OnBumperHit += PlayBumperSFX;
        GameEvents.OnGoalScored += PlayGoalSFX;
        GameEvents.OnPlayCustomSFX += PlayClip;
    }

    private void OnDisable()
    {
        GameEvents.OnPuckLaunched -= PlayLaunchSFX;
        GameEvents.OnBumperHit -= PlayBumperSFX;
        GameEvents.OnGoalScored -= PlayGoalSFX;
        GameEvents.OnPlayCustomSFX -= PlayClip;
    }

    private void PlayLaunchSFX() => PlaySFX(launchClip);
    private void PlayBumperSFX() => PlaySFX(bumperClip);
    private void PlayGoalSFX() => PlaySFX(goalClip);

    private void PlayClip(AudioClip clip) => PlaySFX(clip);

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null) _sfxSource.PlayOneShot(clip);
    }
}