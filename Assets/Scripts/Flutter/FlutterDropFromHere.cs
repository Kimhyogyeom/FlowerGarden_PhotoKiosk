using System.Collections;
using UnityEngine;

public class FlutterDropFromHere : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private RectTransform _rect;

    [Header("Spawn Range (X)")]
    [Tooltip("현재 위치에서 X축 최소 오프셋")]
    [SerializeField] private float _minXOffset = -200f;

    [Tooltip("현재 위치에서 X축 최대 오프셋")]
    [SerializeField] private float _maxXOffset = 200f;

    [Tooltip("현재 Y에서 얼마나 더 위에서 시작할지 (보통 0이면 현재 Y 그대로)")]
    [SerializeField] private float _startYOffset = 0f;

    [Header("Fall")]
    [Tooltip("기본 떨어지는 속도 (px/sec)")]
    [SerializeField] private float _baseFallSpeed = 250f;
    [SerializeField] private float _deducted = 100f;
    private float _originFallSpeed = 0f;

    [Tooltip("화면 아래로 얼마나 더 나갔을 때 '한 번 떨어지기'를 종료할지 여유 마진")]
    [SerializeField] private float _extraBottomMargin = 200f;

    [Header("Flutter Range (랜덤 범위)")]
    [Tooltip("좌우 흔들림 반경 범위 (px)")]
    [SerializeField] private Vector2 _horizontalAmplitudeRange = new Vector2(30f, 80f);

    [Tooltip("좌우 흔들림 속도 범위")]
    [SerializeField] private Vector2 _horizontalFrequencyRange = new Vector2(0.8f, 2.0f);

    [Tooltip("회전(펄럭임) 각도 범위(도 단위)")]
    [SerializeField] private Vector2 _rotationAmplitudeRange = new Vector2(10f, 25f);

    [Tooltip("회전 속도 범위")]
    [SerializeField] private Vector2 _rotationFrequencyRange = new Vector2(1.0f, 3.0f);

    [Header("Loop Options")]
    [Tooltip("자동으로 계속 반복할지 여부")]
    [SerializeField] private bool _autoLoop = true;

    [Tooltip("각 사이클 시작 전 대기 시간 범위(초)")]
    [SerializeField] private Vector2 _delayBeforeDropRange = new Vector2(0f, 5f);

    [Tooltip("한 사이클 끝나고(아래로 빠진 후) 다음 사이클까지 추가 대기 시간(초)")]
    [SerializeField] private Vector2 _delayAfterDropRange = new Vector2(0.5f, 2f);

    [Tooltip("처음 Enable 될 때 바로 루프 시작할지 여부")]
    [SerializeField] private bool _startOnEnable = true;

    [Header("Visual Hide")]
    [Tooltip("떨어지지 않을 때 이미지를 숨길지 여부")]
    //[SerializeField] private bool _hideWhenIdle = true;

    private RectTransform _parentRect;

    // 런타임용 변수들
    private float _time;
    private float _startX;
    private float _startY;
    private float _baseRotZ;

    [SerializeField] private float _fallSpeed;
    private float _hAmp;
    private float _hFreq;
    private float _rAmp;
    private float _rFreq;

    private bool _isPlaying;

    // 처음(디자인 시점) 위치 기억용
    private Vector2 _originAnchoredPos;
    private bool _originSaved;

    private Coroutine _loopRoutine;
    //private CanvasGroup _canvasGroup;

    private void Reset()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _parentRect = _rect.parent as RectTransform;
        _baseRotZ = _rect.localEulerAngles.z;

        // 처음 위치를 기준 위치로 저장
        _originAnchoredPos = _rect.anchoredPosition;
        _originSaved = true;

        // 시각적 숨김에 사용할 CanvasGroup
        //_canvasGroup = GetComponent<CanvasGroup>();

        _originFallSpeed = _baseFallSpeed;
    }

    private void OnEnable()
    {
        if (_startOnEnable)
        {
            StartLoop();
        }
    }

    private void OnDisable()
    {
        StopLoop();
        _isPlaying = false;
        _time = 0f;
        _rect.localEulerAngles = new Vector3(0f, 0f, _baseRotZ);
        //if (_hideWhenIdle && _canvasGroup != null)
        //    _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 외부에서 루프 시작
    /// </summary>
    public void StartLoop()
    {
        if (_loopRoutine != null)
            StopCoroutine(_loopRoutine);

        _loopRoutine = StartCoroutine(LoopRoutine());
    }

    /// <summary>
    /// 외부에서 루프 정지
    /// </summary>
    public void StopLoop()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    /// <summary>
    /// 자동 반복 루틴
    /// </summary>
    private IEnumerator LoopRoutine()
    {
        // 무한 루프(끄고 싶으면 StopLoop 호출)
        while (true)
        {
            // 1) 떨어지기 전에 랜덤 대기
            float preDelay = Random.Range(_delayBeforeDropRange.x, _delayBeforeDropRange.y);
            if (preDelay > 0f)
                yield return new WaitForSeconds(preDelay);

            // 2) 한 번 떨어지는 과정 실행
            PlayOnce();

            // 3) 떨어지는 동안 기다리기
            while (_isPlaying)
            {
                yield return null;
            }

            // 4) 한 사이클 끝나고 추가 대기
            float postDelay = Random.Range(_delayAfterDropRange.x, _delayAfterDropRange.y);
            if (postDelay > 0f)
                yield return new WaitForSeconds(postDelay);

            if (!_autoLoop)
            {
                // autoLoop가 꺼져 있으면 한 번만 하고 종료
                _loopRoutine = null;
                yield break;
            }
        }
    }

    /// <summary>
    /// 한 번만 떨어지는 설정 (내부용)
    /// </summary>
    private void PlayOnce()
    {
        if (_rect == null)
            return;

        if (_parentRect == null)
            _parentRect = _rect.parent as RectTransform;

        float randomScale = Random.Range(0.4f, 0.7f);
        transform.localScale = Vector3.one * randomScale;
        // 기준 위치: 저장해둔 "처음 위치"
        Vector2 basePos = _originSaved ? _originAnchoredPos : _rect.anchoredPosition;

        // X는 기준 위치에서 min~max 오프셋 랜덤
        float randomXOffset = Random.Range(_minXOffset, _maxXOffset);
        _startX = basePos.x + randomXOffset;

        // Y는 기준 위치 + 옵션 오프셋
        _startY = basePos.y + _startYOffset;

        _rect.anchoredPosition = new Vector2(_startX, _startY);

        // 속도 랜덤 (기본값 ± 약간)
        _baseFallSpeed = _originFallSpeed;
        _fallSpeed = Random.Range(_baseFallSpeed - _deducted, _baseFallSpeed + _deducted);

        // 시간/상태 초기화
        _time = 0f;
        _isPlaying = true;

        // 각 인스턴스마다 펄럭임 세팅 랜덤
        _hAmp = Random.Range(_horizontalAmplitudeRange.x, _horizontalAmplitudeRange.y);
        _hFreq = Random.Range(_horizontalFrequencyRange.x, _horizontalFrequencyRange.y);

        _rAmp = Random.Range(_rotationAmplitudeRange.x, _rotationAmplitudeRange.y);
        _rFreq = Random.Range(_rotationFrequencyRange.x, _rotationFrequencyRange.y);

        // 보이게
        //if (_hideWhenIdle && _canvasGroup != null)
        //    _canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _time += Time.deltaTime;

        // Y: 위에서 아래로 떨어짐
        float y = _startY - _fallSpeed * _time;

        // X: 좌우로 펄럭 (sin 파)
        float x = _startX + Mathf.Sin(_time * _hFreq) * _hAmp;

        _rect.anchoredPosition = new Vector2(x, y);

        // 회전: 휴지 펄럭이는 느낌
        float rotZ = _baseRotZ + Mathf.Sin(_time * _rFreq) * _rAmp;
        _rect.localEulerAngles = new Vector3(0f, 0f, rotZ);

        // 화면 아래로 나갔는지 체크
        if (_parentRect != null)
        {
            float bottomLimit = -_parentRect.rect.height * 0.5f - _extraBottomMargin;
            if (y < bottomLimit)
            {
                FinishOneDrop();
            }
        }
    }

    /// <summary>
    /// 한 번 떨어지는 사이클 끝 처리 (비활성화 대신 여기서 숨김)
    /// </summary>
    private void FinishOneDrop()
    {
        _isPlaying = false;

        // 숨기고 싶으면 알파 0
        //if (_hideWhenIdle && _canvasGroup != null)
        //    _canvasGroup.alpha = 0f;

        // 위치/각도 리셋은 다음 사이클 PlayOnce()에서 다시 설정
    }
}
