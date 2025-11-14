using UnityEngine;

/// <summary>
/// UI를 살짝 좌우로 흔들고(위치), 기울어지게(회전) 만들어
/// 은은한 동적인 느낌을 주는 컨트롤러
/// - anchoredPosition.x 를 기준으로 좌우로 움직임
/// - localEulerAngles.z 를 기준으로 좌우 기울기
/// </summary>
public class SubtleWiggleUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform _rect;
    // 흔들림 효과를 줄 대상 RectTransform
    // 비워두면 Reset/Awake 에서 자동으로 자신의 RectTransform을 찾음

    [Header("Position Wiggle (X)")]
    [Tooltip("좌우로 움직이는 정도 (픽셀)")]
    [SerializeField] private float _posAmplitude = 5f;
    // 위치 흔들림 크기 (X축, 픽셀 단위)

    [Tooltip("좌우로 움직이는 속도 (진동 빈도)")]
    [SerializeField] private float _posFrequency = 1f;
    // 위치가 흔들리는 속도 (값이 클수록 더 빠르게 좌우로 움직임)

    [Header("Rotation Wiggle (Z)")]
    [Tooltip("좌우로 기울어지는 각도(도 단위)")]
    [SerializeField] private float _rotAmplitude = 5f;
    // 회전 흔들림 크기 (Z축, 도 단위)

    [Tooltip("기울기 흔들리는 속도")]
    [SerializeField] private float _rotFrequency = 1f;
    // 회전이 흔들리는 속도 (값이 클수록 더 빠르게 기울어짐)

    [Header("Options")]
    [Tooltip("여러 개 붙였을 때 서로 다른 타이밍으로 움직이게 할지")]
    [SerializeField] private bool _useRandomPhase = true;
    // 여러 개의 SubtleWiggleUI가 있을 때, 각각 시작 시간을 랜덤으로 줘서
    // 모두 똑같이 움직이지 않고 자연스럽게 들썩이게 만들지 여부

    [Tooltip("Time.time 대신 Time.unscaledTime 사용할지 (일시정지 등 무시)")]
    [SerializeField] private bool _useUnscaledTime = false;
    // true면 Time.unscaledTime 을 사용해서 타임스케일(일시정지 등)과 무관하게 계속 흔들림

    private Vector2 _baseAnchoredPos;
    // 기준이 되는 anchoredPosition (흔들림의 중심 위치)

    private float _baseRotZ;
    // 기준이 되는 Z축 회전 값 (흔들림의 중심 각도)

    private float _phaseOffset;
    // 랜덤 위상 오프셋 (서로 다른 타이밍으로 움직이게 하기 위함)

    [SerializeField] private GameObject _parentObject;
    // 필요 시 부모 오브젝트를 참조할 때 사용할 수 있는 필드 (현재 로직에서는 직접 사용하지 않음)

    private float _time;
    // 내부에서만 쓰는 타이머 (Time.time 대신 누적해서 사용)

    /// <summary>
    /// 에디터에서 컴포넌트를 추가했을 때 기본값을 셋업하는 콜백
    /// - _rect가 비어있으면 자신의 RectTransform을 자동으로 연결
    /// </summary>
    private void Reset()
    {
        _rect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 초기값 설정
    /// - 기준 위치/회전 값 저장
    /// - 랜덤 위상 오프셋 계산
    /// </summary>
    private void Awake()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        // 현재 위치/각도를 기준으로 흔들림 계산
        _baseAnchoredPos = _rect.anchoredPosition;
        _baseRotZ = _rect.localEulerAngles.z;

        // 여러 개가 동시에 쓰일 때 서로 다른 타이밍으로 움직이게 하기 위한 위상
        _phaseOffset = _useRandomPhase ? Random.Range(0f, 100f) : 0f;

        _time = 0f;
    }

    /// <summary>
    /// 오브젝트가 다시 활성화될 때,
    /// 기준 위치/각도 복원 + 타이머 리셋
    /// </summary>
    private void OnEnable()
    {
        if (_rect != null)
        {
            // 처음 기준 위치/각도로 되돌리기
            _rect.anchoredPosition = _baseAnchoredPos;
            _rect.localEulerAngles = new Vector3(0f, 0f, _baseRotZ);
        }

        // 시간도 0부터 다시 시작 → 항상 처음 움직이는 것처럼
        _time = 0f;
    }

    /// <summary>
    /// 비활성화될 때도 위치/각도 리셋 (패널 토글 시 깔끔하게 초기화)
    /// </summary>
    private void OnDisable()
    {
        if (_rect != null)
        {
            _rect.anchoredPosition = _baseAnchoredPos;
            _rect.localEulerAngles = new Vector3(0f, 0f, _baseRotZ);
        }

        _time = 0f;
    }

    /// <summary>
    /// 매 프레임마다 위치/회전 값을 갱신하여
    /// 부드러운 흔들림 효과를 적용
    /// </summary>
    private void Update()
    {
        if (_rect == null) return;

        // 사용할 시간값: 타임스케일 영향을 받을지 여부에 따라 deltaTime 누적
        float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _time += dt;

        float t = _time + _phaseOffset;

        // 좌우 위치 흔들림 (X축)
        float offsetX = Mathf.Sin(t * _posFrequency) * _posAmplitude;
        float x = _baseAnchoredPos.x + offsetX;
        float y = _baseAnchoredPos.y;

        _rect.anchoredPosition = new Vector2(x, y);

        // 회전 흔들림 (좌우 기울기, Z축)
        float rotZ = _baseRotZ + Mathf.Sin(t * _rotFrequency) * _rotAmplitude;
        _rect.localEulerAngles = new Vector3(0f, 0f, rotZ);
    }
}
