using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Select -> Filming
/// 입력 없을 때 지정 시간 뒤 자동으로 전환되는 컨트롤러
/// </summary>
public class SelectAutoTransitionCtrl : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("카운트다운 시작 초(기본값)")]
    [SerializeField] private float _startSeconds = 10f;

    [Header("Runtime")]
    [SerializeField] private float _timer;                   // 현재 남은 시간
    [SerializeField] private TextMeshProUGUI _timerText;     // 타이머 표시용 텍스트 (선택)

    [Header("Events")]
    [Tooltip("타이머가 0이 되었을 때 호출할 동작 (Filming 화면 전환 등)")]
    [SerializeField] private UnityEvent _onTimerFinished;

    private Coroutine _timerRoutine;

    /// <summary>
    /// [외부 호출용] 자동 전환 카운트다운 시작
    /// </summary>
    public void StartAutoTransitionTimer()
    {
        // 이미 돌고 있으면 먼저 정지 후 다시 시작 (리셋 느낌)
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        _timerRoutine = StartCoroutine(TimerRoutine());
    }

    /// <summary>
    /// [외부 호출용] 타이머 중단 및 UI 초기화
    /// (필요하면 선택적으로 사용)
    /// 일단 안쓸듯?
    /// </summary>
    public void StopAutoTransitionTimer()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        if (_timerText != null)
            _timerText.text = string.Empty;
    }

    private IEnumerator TimerRoutine()
    {
        _timer = _startSeconds;

        while (_timer > 0f)
        {
            int display = Mathf.CeilToInt(_timer);

            if (_timerText != null)
                _timerText.text = display.ToString();

            yield return new WaitForSeconds(1f);
            _timer -= 1f;
        }

        // 마지막 0 표시
        if (_timerText != null)
            _timerText.text = "0";

        _timerRoutine = null;

        // 타이머 종료 후 텍스트 지우기(선택사항)
        if (_timerText != null)
            _timerText.text = string.Empty;

        // 실제 전환 로직은 인스펙터에서 _onTimerFinished 에 연결해서 사용
        if (_onTimerFinished != null)
        {
            _onTimerFinished.Invoke();
        }
        else
        {
            Debug.Log("[SelectAutoTransition] Timer finished, but no event hooked.");
        }
    }
}
