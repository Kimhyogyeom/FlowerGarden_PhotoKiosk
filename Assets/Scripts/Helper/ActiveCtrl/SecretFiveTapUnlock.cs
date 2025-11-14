using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 비밀 다중 탭(연타)로 관리자/힌트 패널 등을 여는 스크립트
/// - 지정된 시간(_windowSeconds) 안에 버튼을 _tapThreshold번 누르면 대상 오브젝트 활성화/토글
/// - 다른 UI를 클릭했을 때 카운트 초기화, 옵션에 따라 자동 닫기 지원
/// </summary>
public class SecretFiveTapUnlock : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Button _button;              // 연타를 감지할 버튼 (비워두면 자기 자신에서 Button 검색)
    [SerializeField] private GameObject _targetImage;     // 임계치 연타 성공 시 켜거나 토글할 오브젝트(관리자 패널 등)

    [Header("Config")]
    [SerializeField] private int _tapThreshold = 10;      // 일정 시간 안에 눌러야 하는 탭 수
    [SerializeField] private float _windowSeconds = 3f;   // 탭을 모을 수 있는 시간 창(초)

    [Tooltip("true: 임계치 달성 시 ON/OFF 토글, false: 항상 켜기만 함")]
    [SerializeField] private bool _toggleOnThreshold = true;

    [Tooltip("열려 있을 때 버튼 외의 다른 UI를 클릭하면 자동으로 닫을지 여부")]
    [SerializeField] private bool _closeOnOutsideClick = false;

    [Header("Debug")]
    [SerializeField] private bool _enableLog = false;     // 디버그 로그 출력 여부

    private int _count;           // 현재 시간 창 내에서 누른 횟수
    private float _windowEnd;     // 현재 시간 창이 끝나는 시각 (Time.unscaledTime 기준)
    private bool _subscribed;     // UiClickBroadcaster 구독 여부

    [SerializeField] private Button _closeButton;         // 관리자 패널 안에서 닫기용 버튼 (선택)
    [SerializeField] private HelporTextReset _helperTextReset; // 헬프 텍스트 초기화용 스크립트 (선택)

    [Header("Test Text")]
    [SerializeField] private TextMeshProUGUI _hlperTouchCount; // 현재 탭 카운트 표시용 테스트 텍스트

    /// <summary>
    /// 버튼과 닫기 버튼에 리스너 연결
    /// </summary>
    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_button == null) Debug.LogError("[SecretFiveTapUnlock] Button reference missing.");

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(CloseTarget);
        }
    }

    /// <summary>
    /// 활성화 시 UI 클릭 브로드캐스터 이벤트 구독 및 카운트 초기화
    /// </summary>
    private void OnEnable()
    {
        if (!_subscribed)
        {
            UiClickBroadcaster.OnAnyUIClick += HandleAnyUIClick;
            _subscribed = true;
        }
        _count = 0;
        _windowEnd = 0f;
    }

    /// <summary>
    /// 비활성화 시 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_subscribed)
        {
            UiClickBroadcaster.OnAnyUIClick -= HandleAnyUIClick;
            _subscribed = false;
        }
    }

    /// <summary>
    /// 외부/닫기 버튼에서 호출:
    /// - 대상 오브젝트 비활성화
    /// - 카운트/시간창 리셋
    /// - 헬프 텍스트 초기화
    /// </summary>
    public void CloseTarget()
    {
        if (_targetImage && _targetImage.activeSelf)
        {
            _targetImage.SetActive(false);
            if (_enableLog) Debug.Log("[FiveTap] CloseTarget() → target OFF");
        }
        // 카운트/시간창 초기화
        _count = 0;
        _windowEnd = 0f;

        _helperTextReset.ResetTexts();
    }

    /// <summary>
    /// 씬 어디서든 발생한 UI 클릭을 받아서
    /// - 우리 버튼/자식이면 다중 탭 카운트 처리
    /// - 다른 UI면 카운트 초기화 및 필요 시 자동 닫기
    /// </summary>
    private void HandleAnyUIClick(GameObject clicked)
    {
        bool isOurButton = false;
        if (clicked != null && _button != null)
        {
            var t = clicked.transform;
            var root = _button.transform;
            isOurButton = (t == root) || t.IsChildOf(root);
        }

        float now = Time.unscaledTime;
        // 시간 창이 지났으면 카운트 초기화
        if (now > _windowEnd) _count = 0;

        if (isOurButton)
        {
            // 우리 버튼 클릭 → 시간 창 갱신 + 카운트 증가
            _windowEnd = now + _windowSeconds;
            _count++;

            if (_enableLog) Debug.Log($"[FiveTap] {name} count={_count}/{_tapThreshold}");

            // 임계치 달성
            if (_count >= _tapThreshold)
            {
                _count = 0;
                _windowEnd = 0f;

                if (_targetImage)
                {
                    if (_toggleOnThreshold)
                        _targetImage.SetActive(!_targetImage.activeSelf);
                    else
                        _targetImage.SetActive(true);

                    if (_enableLog)
                        Debug.Log("[FiveTap] THRESHOLD → " +
                                  (_targetImage.activeSelf ? "ON" : "OFF"));
                }
            }
        }
        else
        {
            // 다른 UI 클릭 → 카운트/시간창 리셋
            if (_count != 0 && _enableLog) Debug.Log("[FiveTap] reset by outside click");
            _count = 0;
            _windowEnd = 0f;

            // 옵션에 따라 열려 있는 대상 자동 닫기
            if (_closeOnOutsideClick && _targetImage && _targetImage.activeSelf)
            {
                _targetImage.SetActive(false);
                if (_enableLog) Debug.Log("[FiveTap] outside click → target OFF");
            }
        }

        // 테스트용 헬프 텍스트 업데이트
        if (_hlperTouchCount != null)
        {
            _hlperTouchCount.text = $"Helper Test\nTouch {_count}/10";
        }
    }
}
