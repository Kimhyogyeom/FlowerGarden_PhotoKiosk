using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 공용 카운트다운 타이머 컴포넌트
/// - StartTimer(seconds)로 시작
/// - 매초 1씩 감소
/// - 0이 되면 "제한 시간 초과" 로그 찍고 OnTimeout 이벤트 호출
/// - (옵션) TextMeshProUGUI에 남은 시간 표시 가능
/// </summary>
public class CountdownTimer : MonoBehaviour
{
    [Header("Compoment Setting")]
    [SerializeField] private PrintButtonHandler _printButtonHandler;

    [Header("Default Settings")]
    [Tooltip("기본 시작 시간(초). StartTimer()를 seconds 없이 부를 때 사용")]
    [SerializeField] private int _defaultSeconds = 60;

    [Header("UI (Optional)")]
    [Tooltip("남은 시간을 표시할 TMP 텍스트 (없으면 표시 안 함)")]
    [SerializeField] private TextMeshProUGUI _timeText;

    /// <summary>현재 남은 시간(초)</summary>
    public int RemainingSeconds { get; private set; }

    /// <summary>타이머 동작 중인지 여부</summary>
    public bool IsRunning => _timerRoutine != null;

    /// <summary>시간이 0이 되었을 때 호출되는 콜백</summary>
    // public event Action OnTimeout;

    private Coroutine _timerRoutine;

    /// <summary>
    /// 기본 설정(_defaultSeconds)로 타이머 시작
    /// </summary>
    public void StartTimer()
    {
        StartTimer(_defaultSeconds);
    }

    /// <summary>
    /// 지정한 seconds로 타이머 시작
    /// </summary>
    public void StartTimer(int seconds)
    {
        // 이미 돌고 있으면 먼저 정지
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        RemainingSeconds = Mathf.Max(0, seconds);
        UpdateTimeText();
        _timerRoutine = StartCoroutine(TimerRoutine());
    }

    /// <summary>
    /// 타이머 강제 정지
    /// </summary>
    public void StopTimer()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }
    }

    private IEnumerator TimerRoutine()
    {
        while (RemainingSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            RemainingSeconds--;
            UpdateTimeText();
        }

        Debug.Log("제한 시간 초과");

        // 콜백 호출 (구독자가 있다면)
        // OnTimeout?.Invoke();
        _printButtonHandler.OnClickPrint();

        _timerRoutine = null;
    }

    private void UpdateTimeText()
    {
        if (_timeText == null) return;

        // 예: "60" 만 찍고 싶으면 이렇게
        _timeText.text = RemainingSeconds.ToString();

        // "60초" 이런 식으로 하고 싶으면:
        // _timeText.text = $"{RemainingSeconds}초";
    }
}
