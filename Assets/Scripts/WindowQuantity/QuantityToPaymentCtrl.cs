using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수량 선택 화면 → 결제(Payment) 화면으로 전환하는 컨트롤러
/// - "다음" 버튼 클릭 시 수량 패널 비활성화
/// - Payment 패널 활성화
/// </summary>
public class QuantityToPaymentCtrl : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _nextButton;          // "다음" 버튼

    [Header("Panels")]
    [SerializeField] private GameObject _quantityPanel;   // 수량 선택 패널
    [SerializeField] private GameObject _paymentPanel;    // 결제 패널 (PaymentPanelEnableBroadcaster 붙어 있는 쪽)

    [Header("Component")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;

    private void Awake()
    {
        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(OnClickNext);
        }
        else
        {
            Debug.LogWarning("[QuantityToPaymentCtrl] _nextButton reference is missing");
        }
    }

    private void OnDestroy()
    {
        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(OnClickNext);
        }
    }

    /// <summary>
    /// "다음" 버튼 클릭 시 호출
    /// </summary>
    private void OnClickNext()
    {
        // 상태를 결제 대기 상태로 두고 싶다면 (원하는 경우 사용)
        // GameManager.Instance.SetState(KioskState.WaitingForPayment);

        _fadeAnimationCtrl._isStateStep = 3;
        _fadeAnimationCtrl.StartFade();

    }
    public void ObjectActiveCtrl()
    {
        GameManager.Instance.SetState(KioskState.Payment);
        // 수량 선택 패널 닫기
        if (_quantityPanel != null)
        {
            _quantityPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[QuantityToPaymentCtrl] _quantityPanel reference is missing");
        }

        // 결제 패널 열기
        if (_paymentPanel != null)
        {
            _paymentPanel.SetActive(true);

            // PaymentPanelEnableBroadcaster 가 Payment 패널에 붙어 있다면,
            // SetActive(true) 시점에 OnEnable 이 호출되면서
            // PaymentCtrl.TryStartPayment 가 알아서 동작하게 됨.
        }
        else
        {
            Debug.LogWarning("[QuantityToPaymentCtrl] _paymentPanel reference is missing");
        }

        // 필요하면 버튼 클릭 사운드도 여기서 재생 가능 하긴 한데 사운드가 들어가는지 안들어가는지 물어보는거 깜빡함 헷핫훗헷홋
        // SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._startButton);
    }
}
