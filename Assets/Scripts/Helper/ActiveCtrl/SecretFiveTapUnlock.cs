using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SecretFiveTapUnlock : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Button _button;              // 이 버튼 (비워두면 자동)
    [SerializeField] private GameObject _targetImage;     // 5연타 성공 시 토글/켜줄 오브젝트

    [Header("Config")]
    [SerializeField] private int _tapThreshold = 10;
    [SerializeField] private float _windowSeconds = 3f;

    [Tooltip("true: 임계치 달성 시 ON/OFF 토글, false: 항상 켜기")]
    [SerializeField] private bool _toggleOnThreshold = true;

    [Tooltip("열려 있을 때 버튼 외의 다른 UI를 클릭하면 자동으로 닫기")]
    [SerializeField] private bool _closeOnOutsideClick = false;

    [Header("Debug")]
    [SerializeField] private bool _enableLog = false;

    private int _count;
    private float _windowEnd;
    private bool _subscribed;

    [SerializeField] private Button _closeButton;
    [SerializeField] private HelporTextReset _helperTextReset;

    [Header("Test Text")]
    [SerializeField] private TextMeshProUGUI _hlperTouchCount;

    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_button == null) Debug.LogError("[SecretFiveTapUnlock] Button reference missing.");

        if(_closeButton != null)
        {
            _closeButton.onClick.AddListener(CloseTarget);
        }
    }

    private void OnEnable()
    {
        if (!_subscribed)
        {
            UiClickBroadcaster.OnAnyUIClick += HandleAnyUIClick;
            _subscribed = true;
        }
        _count = 0; _windowEnd = 0f;
    }

    private void OnDisable()
    {
        if (_subscribed)
        {
            UiClickBroadcaster.OnAnyUIClick -= HandleAnyUIClick;
            _subscribed = false;
        }
    }

    public void CloseTarget()
    {
        if (_targetImage && _targetImage.activeSelf)
        {
            _targetImage.SetActive(false);
            if (_enableLog) Debug.Log("[FiveTap] CloseTarget() → target OFF");
        }
        // 카운트/창도 초기화
        _count = 0; _windowEnd = 0f;
        _helperTextReset.ResetTexts();
    }

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
        if (now > _windowEnd) _count = 0; // 시간창 만료 시 초기화

        if (isOurButton)
        {
            _windowEnd = now + _windowSeconds;
            _count++;

            if (_enableLog) Debug.Log($"[FiveTap] {name} count={_count}/{_tapThreshold}");

            if (_count >= _tapThreshold)
            {
                _count = 0; _windowEnd = 0f;

                if (_targetImage)
                {
                    if (_toggleOnThreshold)
                        _targetImage.SetActive(!_targetImage.activeSelf);
                    else
                        _targetImage.SetActive(true);

                    if (_enableLog) Debug.Log("[FiveTap] THRESHOLD → " +
                        (_targetImage.activeSelf ? "ON" : "OFF"));
                }
            }
        }
        else
        {
            // 다른 UI 클릭 → 카운트 초기화
            if (_count != 0 && _enableLog) Debug.Log("[FiveTap] reset by outside click");
            _count = 0; _windowEnd = 0f;

            // 필요하면 열려 있는 팝업 자동 닫기
            if (_closeOnOutsideClick && _targetImage && _targetImage.activeSelf)
            {
                _targetImage.SetActive(false);
                if (_enableLog) Debug.Log("[FiveTap] outside click → target OFF");
            }
        }
        _hlperTouchCount.text = $"Helper Test\nTouch {_count}/10";
    }
}
