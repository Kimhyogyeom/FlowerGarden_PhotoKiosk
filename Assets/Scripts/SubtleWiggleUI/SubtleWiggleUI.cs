using UnityEngine;

public class SubtleWiggleUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform _rect;

    [Header("Position Wiggle (X)")]
    [Tooltip("좌우로 움직이는 정도 (픽셀)")]
    [SerializeField] private float _posAmplitude = 5f;

    [Tooltip("좌우로 움직이는 속도 (진동 빈도)")]
    [SerializeField] private float _posFrequency = 1f;

    [Header("Rotation Wiggle (Z)")]
    [Tooltip("좌우로 기울어지는 각도(도 단위)")]
    [SerializeField] private float _rotAmplitude = 5f;

    [Tooltip("기울기 흔들리는 속도")]
    [SerializeField] private float _rotFrequency = 1f;

    [Header("Options")]
    [Tooltip("여러 개 붙였을 때 서로 다른 타이밍으로 움직이게 할지")]
    [SerializeField] private bool _useRandomPhase = true;

    [Tooltip("Time.time 대신 Time.unscaledTime 사용할지 (일시정지 등 무시)")]
    [SerializeField] private bool _useUnscaledTime = false;

    private Vector2 _baseAnchoredPos;
    private float _baseRotZ;
    private float _phaseOffset;

    [SerializeField] private GameObject _parentObject;

    private void Reset()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _baseAnchoredPos = _rect.anchoredPosition;
        _baseRotZ = _rect.localEulerAngles.z;

        _phaseOffset = _useRandomPhase ? Random.Range(0f, 100f) : 0f;
    }

    private void OnEnable()
    {
        // Enable 시점에 기준값 갱신 (씬에서 위치/각도 바꿨을 수 있으니까)
        if (_rect != null)
        {
            _baseAnchoredPos = _rect.anchoredPosition;
            _baseRotZ = _rect.localEulerAngles.z;
        }
    }

    private void Update()
    {
        //print("2222222222");

        if (_rect == null) return;        

        float t = _useUnscaledTime ? Time.unscaledTime : Time.time;
        t += _phaseOffset;

        // 좌우 위치 흔들림
        float offsetX = Mathf.Sin(t * _posFrequency) * _posAmplitude;
        float x = _baseAnchoredPos.x + offsetX;
        float y = _baseAnchoredPos.y;

        _rect.anchoredPosition = new Vector2(x, y);

        // 회전 흔들림 (좌우 기울기)
        float rotZ = _baseRotZ + Mathf.Sin(t * _rotFrequency) * _rotAmplitude;
        _rect.localEulerAngles = new Vector3(0f, 0f, rotZ);
    }
}
