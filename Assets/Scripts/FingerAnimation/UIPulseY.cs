using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIPulseY : MonoBehaviour
{
    [Header("Target (optional, default = this)")]
    [SerializeField] private RectTransform _target;   // 비워두면 자기 자신의 RectTransform

    [Header("Motion")]
    [SerializeField] private float _downOffset = -10f;  // 아래로 -10
    [SerializeField] private float _downDuration = 0.60f; // 천천히 내려가기
    [SerializeField] private float _upDuration = 0.15f; // 빠르게 원위치
    [SerializeField] private bool _useUnscaledTime = true; // 타임스케일 무시 (UI에 보통 권장)

    private RectTransform _rt;
    private Vector2 _baseAnchoredPos;
    private Coroutine _loopCo;

    private void Awake()
    {
        _rt = _target ? _target : GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        _baseAnchoredPos = _rt.anchoredPosition;
        _loopCo = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _rt.anchoredPosition = _baseAnchoredPos; // 깔끔히 복귀
    }

    private IEnumerator Loop()
    {
        Vector2 from = _baseAnchoredPos;
        Vector2 to = new Vector2(from.x, from.y + _downOffset);

        while (true)
        {
            // 1) 아래로 천천히 (거의 선형)
            yield return AnimateY(from, to, _downDuration, EaseLinear);

            // 2) 위로 빠르게 (스냅 느낌: 약간의 out-ease)
            yield return AnimateY(to, from, _upDuration, EaseOutQuad);
        }
    }

    private IEnumerator AnimateY(Vector2 from, Vector2 to, float duration, System.Func<float, float> ease)
    {
        if (duration <= 0f)
        {
            _rt.anchoredPosition = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / duration;
            float k = Mathf.Clamp01(t);
            float e = ease(k);

            float y = Mathf.LerpUnclamped(from.y, to.y, e);
            _rt.anchoredPosition = new Vector2(from.x, y);

            yield return null;
        }

        _rt.anchoredPosition = to;
    }

    // Eases
    private float EaseLinear(float x) => x;                         // 내려갈 때: 천천히(=길게) 선형
    private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x); // 올라올 때: 빨리
}
