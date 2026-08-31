using UnityEngine;
using System.Collections;

public class MatchTimeManager : MonoBehaviour
{
    public static MatchTimeManager Instance { get; private set; }

    public enum MatchTimeState { Normal, BulletTime }
    public MatchTimeState CurrentTimeState { get; private set; } = MatchTimeState.Normal;

    [Header("Bullet Time Settings")]
    [SerializeField] private float _slowTimeScale = 0.1f;
    [SerializeField] private float _slowFixedDelta = 0.002f;

    private float _normalTimeScale = 1f;
    private float _normalFixedDelta = 0.02f;

    private Coroutine _delayedRestoreRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerBulletTime()
    {
        if (CurrentTimeState == MatchTimeState.BulletTime) return;

        CurrentTimeState = MatchTimeState.BulletTime;
        Time.timeScale = _slowTimeScale;
        Time.fixedDeltaTime = _slowFixedDelta;

        if (_delayedRestoreRoutine != null)
        {
            StopCoroutine(_delayedRestoreRoutine);
            _delayedRestoreRoutine = null;
        }
    }

    public void RestoreNormalTimeDelayed(float delayInRealSeconds)
    {
        if (_delayedRestoreRoutine != null) StopCoroutine(_delayedRestoreRoutine);
        _delayedRestoreRoutine = StartCoroutine(DelayedRestoreRoutine(delayInRealSeconds));
    }

    private IEnumerator DelayedRestoreRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        RestoreNormalTime();
    }

    public void RestoreNormalTime()
    {
        CurrentTimeState = MatchTimeState.Normal;
        Time.timeScale = _normalTimeScale;
        Time.fixedDeltaTime = _normalFixedDelta;

        if (_delayedRestoreRoutine != null)
        {
            StopCoroutine(_delayedRestoreRoutine);
            _delayedRestoreRoutine = null;
        }
    }
}