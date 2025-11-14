using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 미션 텍스트 슬라이드 애니메이션 컨트롤러
/// - 왼쪽 화면 밖에서 빠르게 들어와서,
///   살짝 오른쪽으로 튕겼다가(오버슈트) 좌우로 몇 번 흔들린 뒤
///   최종 위치(targetX, targetY)에 안착하는 연출을 담당.
/// - SetTextAndSlideIn(string text) 호출로 텍스트 갱신 + 애니메이션 재생.
/// </summary>
public class MissionTextAnimatorSlide : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _missionText;

    [Header("Position")]
    [Tooltip("왼쪽 화면 밖에서 시작할 X 위치")]
    [SerializeField] private float _offscreenX = -1200f;

    [Tooltip("최종 X 위치 (보통 0)")]
    [SerializeField] private float _targetX = 0f;

    [Tooltip("최종 Y 위치 (기본 0, 필요하면 인스펙터에서 수정)")]
    [SerializeField] private float _targetY = 0f;

    [Header("Timing")]
    [Tooltip("왼쪽 밖 → 첫 오버슈트까지 걸리는 시간")]
    [SerializeField] private float _inDuration = 0.25f;

    [Tooltip("좌우로 흔들리는 한 구간(왕복의 절반) 시간")]
    [SerializeField] private float _wiggleDuration = 0.12f;

    [Header("Overshoot (튕기는 정도)")]
    [Tooltip("처음 오른쪽으로 넘쳐 들어가는 정도 (첫 오버슈트)")]
    [SerializeField] private float _firstOvershoot = 60f;

    [Tooltip("그 다음 왼쪽으로 튕기는 정도")]
    [SerializeField] private float _secondOvershoot = 30f;

    [Tooltip("마지막으로 다시 오른쪽으로 튕기는 정도")]
    [SerializeField] private float _thirdOvershoot = 15f;

    private RectTransform _rect;
    private Coroutine _routine;

    private void Awake()
    {
        if (_missionText == null)
            _missionText = GetComponent<TextMeshProUGUI>();

        if (_missionText != null)
            _rect = _missionText.rectTransform;
        else
            _rect = GetComponent<RectTransform>();

        // targetY가 0으로 되어 있으면, 현재 anchoredPosition.y 를 자동으로 사용
        // (정말로 (0,0)을 쓰고 싶으면 인스펙터에서 직접 0으로 설정)
        if (_rect != null && Mathf.Approximately(_targetY, 0f))
        {
            _targetY = _rect.anchoredPosition.y;
        }
    }

    /// <summary>
    /// 텍스트 설정 + 왼쪽에서 슉 들어와서 자리잡는 애니메이션 실행
    /// - 이전 애니메이션이 돌고 있으면 중단 후 새로 시작
    /// </summary>
    public void SetTextAndSlideIn(string text)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(SlideRoutine(text));
    }

    /// <summary>
    /// 실제 슬라이드 인 + 오버슈트/위글 애니메이션 코루틴
    /// </summary>
    private IEnumerator SlideRoutine(string text)
    {
        if (_rect == null || _missionText == null)
            yield break;

        _missionText.text = text;

        // 시작: 왼쪽 화면 밖 + 투명
        _rect.anchoredPosition = new Vector2(_offscreenX, _targetY);

        Color c = _missionText.color;
        c.a = 0f;
        _missionText.color = c;

        // 1) 왼쪽 밖 → targetX + firstOvershoot 까지 (입장 + 첫 오버슈트)
        float from = _offscreenX;
        float to = _targetX + _firstOvershoot;
        float t = 0f;

        while (t < _inDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / _inDuration);

            // easeOutQuad (처음은 빠르게, 끝으로 갈수록 부드럽게 멈춤)
            n = 1f - (1f - n) * (1f - n);

            float x = Mathf.Lerp(from, to, n);
            _rect.anchoredPosition = new Vector2(x, _targetY);

            // 알파 0 → 1 (서서히 등장)
            c.a = n;
            _missionText.color = c;

            yield return null;
        }

        // 2) 오른쪽(오버슈트) → 왼쪽 약간
        yield return MoveX(to, _targetX - _secondOvershoot, _wiggleDuration);

        // 3) 왼쪽 → 오른쪽 약간
        yield return MoveX(_targetX - _secondOvershoot, _targetX + _thirdOvershoot, _wiggleDuration);

        // 4) 오른쪽 → 정확히 targetX
        yield return MoveX(_targetX + _thirdOvershoot, _targetX, _wiggleDuration);

        // 최종 위치 스냅
        _rect.anchoredPosition = new Vector2(_targetX, _targetY);
        _routine = null;
    }

    /// <summary>
    /// X 좌표를 from → to 로 duration 동안 부드럽게 이동
    /// - Y는 _targetY 고정
    /// </summary>
    private IEnumerator MoveX(float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);

            // smoothstep (시작/끝이 부드러운 보간)
            n = n * n * (3f - 2f * n);

            float x = Mathf.Lerp(from, to, n);
            _rect.anchoredPosition = new Vector2(x, _targetY);

            yield return null;
        }

        _rect.anchoredPosition = new Vector2(to, _targetY);
    }
}
