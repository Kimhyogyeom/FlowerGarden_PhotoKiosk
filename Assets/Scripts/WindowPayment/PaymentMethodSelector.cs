using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결제 수단 종류
/// </summary>
public enum PaymentMethod
{
    None,   // 선택 안됨 (안쓸것 같긴 한데 일단 킵)
    Payco,  // 페이코
    Card,   // 카드
    Cash    // 현금
}

/// <summary>
/// 결제 수단 선택 컨트롤러
/// - 페이코 / 카드 / 현금 버튼 중 하나를 선택
/// - 어떤 결제수단이 선택되었는지 enum 으로 보관
/// - 각 버튼 하위의 강조 이미지(체크/밑줄 등)를 ON/OFF
/// </summary>
public class PaymentMethodSelector : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _paycoButton;   // 페이코 버튼
    [SerializeField] private Button _cardButton;    // 카드 버튼
    [SerializeField] private Button _cashButton;    // 현금 버튼

    [Header("Highlight Images")]
    [Tooltip("선택 시 켜질 하위 이미지 (없으면 버튼의 첫 번째 자식을 자동으로 찾음)")]
    [SerializeField] private GameObject _paycoHighlight;  // 페이코 선택 표시용 이미지
    [SerializeField] private GameObject _cardHighlight;   // 카드 선택 표시용 이미지
    [SerializeField] private GameObject _cashHighlight;   // 현금 선택 표시용 이미지

    [Header("Config")]
    [Tooltip("기본 결제 수단 (리셋 시 이 값으로 돌아감)")]
    [SerializeField] private PaymentMethod _defaultMethod = PaymentMethod.Card;

    [Header("Runtime")]
    [SerializeField] private PaymentMethod _selectedMethod = PaymentMethod.Card;

    /// <summary>
    /// 현재 선택된 결제 수단(외부에서 읽기용)
    /// </summary>
    public PaymentMethod SelectedMethod => _selectedMethod;

    private void Awake()
    {
        // 버튼 리스너 등록
        if (_paycoButton != null)
            _paycoButton.onClick.AddListener(OnClickPayco);
        else
            Debug.LogWarning("[PaymentMethodSelector] _paycoButton reference is missing");

        if (_cardButton != null)
            _cardButton.onClick.AddListener(OnClickCard);
        else
            Debug.LogWarning("[PaymentMethodSelector] _cardButton reference is missing");

        if (_cashButton != null)
            _cashButton.onClick.AddListener(OnClickCash);
        else
            Debug.LogWarning("[PaymentMethodSelector] _cashButton reference is missing");

        // 하이라이트 오브젝트가 비어 있으면
        // 버튼의 첫 번째 자식을 자동으로 강조용으로 사용
        AutoAssignHighlightIfNull(_paycoButton, ref _paycoHighlight);
        AutoAssignHighlightIfNull(_cardButton, ref _cardHighlight);
        AutoAssignHighlightIfNull(_cashButton, ref _cashHighlight);

        // 시작 시 기본 결제 수단으로 맞춰두기
        _selectedMethod = _defaultMethod;
        UpdateHighlight();
    }

    private void OnDestroy()
    {
        // 버튼 리스너 해제
        if (_paycoButton != null)
            _paycoButton.onClick.RemoveListener(OnClickPayco);
        if (_cardButton != null)
            _cardButton.onClick.RemoveListener(OnClickCard);
        if (_cashButton != null)
            _cashButton.onClick.RemoveListener(OnClickCash);
    }

    /// <summary>
    /// 하이라이트 오브젝트가 null 이면,
    /// 버튼의 첫 번째 자식을 강조용으로 자동 지정
    /// </summary>
    private void AutoAssignHighlightIfNull(Button btn, ref GameObject highlightObj)
    {
        if (highlightObj != null || btn == null)
            return;

        if (btn.transform.childCount > 0)
        {
            highlightObj = btn.transform.GetChild(0).gameObject;
        }
    }

    private void OnClickPayco()
    {
        SetMethod(PaymentMethod.Payco);
    }

    private void OnClickCard()
    {
        SetMethod(PaymentMethod.Card);
    }

    private void OnClickCash()
    {
        SetMethod(PaymentMethod.Cash);
    }

    /// <summary>
    /// 내부에서 선택 변경 처리
    /// </summary>
    private void SetMethod(PaymentMethod method)
    {
        _selectedMethod = method;
        Debug.Log($"[PaymentMethodSelector] Selected: {_selectedMethod}");
        UpdateHighlight();

        // 필요하면 여기서 결제 수단 선택 효과음 재생도 가능
        // SoundManager.Instance.PlaySFX(...);
    }

    /// <summary>
    /// 선택된 결제 수단에 따라 하이라이트 이미지 ON/OFF
    /// </summary>
    private void UpdateHighlight()
    {
        if (_paycoHighlight != null)
            _paycoHighlight.SetActive(_selectedMethod == PaymentMethod.Payco);

        if (_cardHighlight != null)
            _cardHighlight.SetActive(_selectedMethod == PaymentMethod.Card);

        if (_cashHighlight != null)
            _cashHighlight.SetActive(_selectedMethod == PaymentMethod.Cash);
    }

    // ─────────────────────────────────────────────────────────
    // [외부 호출용] 선택 초기화
    // 예) 패널 OnEnable() 에서 ResetSelection() 호출
    // ─────────────────────────────────────────────────────────
    /// <summary>
    /// 결제 수단 선택을 기본값(_defaultMethod)으로 초기화
    /// </summary>
    public void ResetSelection()
    {
        _selectedMethod = _defaultMethod;
        UpdateHighlight();
        Debug.Log($"[PaymentMethodSelector] Reset to default: {_selectedMethod}");
    }

    /// <summary>
    /// 필요하다면 특정 값으로 강제 초기화하고 싶을 때 사용
    /// </summary>
    public void ResetSelection(PaymentMethod method)
    {
        _selectedMethod = method;
        UpdateHighlight();
        Debug.Log($"[PaymentMethodSelector] Reset to: {_selectedMethod}");
    }
}
