using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI RectTransform을 Y축으로 살짝 내려갔다가 다시 올라오게 만드는 반복 애니메이션
/// - 기준 위치에서 아래로 천천히 이동
/// - 다시 위로 빠르게 복귀
/// - 코루틴으로 무한 반복 (Enable 시 시작, Disable 시 정지)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPulseY : MonoBehaviour
{
    [Header("Target (optional, default = this)")]
    [SerializeField] private RectTransform _target;   // 비워두면 자기 자신의 RectTransform 사용

    [Header("Motion")]
    [SerializeField] private float _downOffset = -10f;      // 기준 위치에서 아래로 이동할 거리
    [SerializeField] private float _downDuration = 0.60f;   // 아래로 천천히 내려가는 데 걸리는 시간
    [SerializeField] private float _upDuration = 0.15f;     // 다시 위로 빠르게 올라가는 데 걸리는 시간
    [SerializeField] private bool _useUnscaledTime = true;  // true일 경우 Time.timeScale의 영향을 받지 않음(UI 애니메이션에 권장)

    private RectTransform _rt;              // 실제로 움직일 RectTransform
    private Vector2 _baseAnchoredPos;       // 기준이 되는 시작 위치(anchoredPosition)
    private Coroutine _loopCo;              // 현재 동작 중인 루프 코루틴 참조

    /// <summary>
    /// 타겟 RectTransform 설정
    /// - 인스펙터에서 _target을 지정하지 않았다면 자기 자신의 RectTransform을 사용
    /// </summary>
    private void Awake()
    {
        _rt = _target ? _target : GetComponent<RectTransform>();
    }

    /// <summary>
    /// 활성화될 때:
    /// - 현재 위치를 기준 위치로 저장
    /// - 아래/위로 반복 이동하는 코루틴 시작
    /// </summary>
    private void OnEnable()
    {
        _baseAnchoredPos = _rt.anchoredPosition;
        _loopCo = StartCoroutine(Loop());
    }

    /// <summary>
    /// 비활성화될 때:
    /// - 코루틴 정지
    /// - 위치를 기준 위치로 되돌림
    /// </summary>
    private void OnDisable()
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _rt.anchoredPosition = _baseAnchoredPos; // 깔끔하게 원 위치로 복귀
    }

    /// <summary>
    /// 아래로 갔다가 다시 위로 올라오는 동작을 무한 반복하는 루프 코루틴
    /// </summary>
    private IEnumerator Loop()
    {
        Vector2 from = _baseAnchoredPos;
        Vector2 to = new Vector2(from.x, from.y + _downOffset);

        while (true)
        {
            // 1) 기준 위치 → 아래로 천천히 (거의 선형)
            yield return AnimateY(from, to, _downDuration, EaseLinear);

            // 2) 아래 위치 → 기준 위치로 빠르게 복귀 (Out-Ease 느낌)
            yield return AnimateY(to, from, _upDuration, EaseOutQuad);
        }
    }

    /// <summary>
    /// Y축만 보간해서 from → to로 이동시키는 공용 코루틴
    /// </summary>
    /// <param name="from">시작 위치</param>
    /// <param name="to">도착 위치</param>
    /// <param name="duration">이동에 걸리는 시간</param>
    /// <param name="ease">0~1 구간을 0~1로 매핑하는 이징 함수</param>
    private IEnumerator AnimateY(Vector2 from, Vector2 to, float duration, System.Func<float, float> ease)
    {
        // duration이 0 이하이면 즉시 위치를 옮기고 종료
        if (duration <= 0f)
        {
            _rt.anchoredPosition = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            // 타임스케일을 쓸지 여부에 따라 델타 타임 선택
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / duration;

            float k = Mathf.Clamp01(t);
            float e = ease(k); // 이징 적용 후 보간 비율

            float y = Mathf.LerpUnclamped(from.y, to.y, e);
            _rt.anchoredPosition = new Vector2(from.x, y);

            yield return null;
        }

        // 마지막 프레임에서 정확히 도착 위치로 스냅
        _rt.anchoredPosition = to;
    }

    // ───────────────────────── Easing Functions ─────────────────────────

    /// <summary>
    /// 선형 이징 (그대로 비율 사용)
    /// </summary>
    private float EaseLinear(float x) => x;                         // 내려갈 때: 일정한 속도로 이동

    /// <summary>
    /// Quadratic Ease-Out (처음 빠르게, 끝으로 갈수록 천천히)
    /// </summary>
    private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x); // 올라올 때: 빠르게 시작해서 부드럽게 멈춤
}
