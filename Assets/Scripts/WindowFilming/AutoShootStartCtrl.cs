using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Select -> Filming
/// 입력 없을 때 지정 시간 뒤 자동으로 전환되는 컨트롤러
/// </summary>
public class AutoShootStartCtrl : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("카운트다운 시작 초(기본값)")]
    [SerializeField] private float _startSeconds = 10f;

    [Header("Runtime")]
    [SerializeField] private float _timer;                   // 현재 남은 시간
    [SerializeField] private TextMeshProUGUI _timerText;     // 타이머 표시용 텍스트 (선택)

    //[Header("Events")]
    //[Tooltip("타이머가 0이 되었을 때 호출할 동작 (Filming 화면 전환 등)")]
    //[SerializeField] private UnityEvent _onTimerFinished;

    [SerializeField] private FilmingPanelCtrl _filmingPanelCtrl;

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

        // 실제 전환 로직
        if (_filmingPanelCtrl != null)
        {
            _filmingPanelCtrl.OnPhotoButtonClicked();
        }
    }

    // ─────────────────────────────
    // 초기화용 함수
    // ─────────────────────────────

    /// <summary>
    /// 이 컨트롤러를 완전히 초기 상태로 되돌리는 함수
    /// - 코루틴 중단
    /// - 타이머 값 리셋
    /// - 텍스트 클리어
    /// </summary>
    public void ResetAutoShootStartCtrl()
    {
        // 이 컴포넌트에서 돌고 있는 코루틴만 정지
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        // 타이머 값 초기화
        _timer = _startSeconds;

        // 텍스트 초기화
        if (_timerText != null)
            _timerText.text = string.Empty;
    }

    /// <summary>
    /// 오브젝트가 비활성화될 때
    /// 중간 상태로 남지 않게 정리하고 싶으면 사용
    /// </summary>
    private void OnDisable()
    {
        ResetAutoShootStartCtrl();
    }
}
