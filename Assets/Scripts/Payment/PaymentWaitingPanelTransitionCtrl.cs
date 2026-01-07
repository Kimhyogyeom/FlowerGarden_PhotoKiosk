using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결제 선택 화면 → WaitingForPayment 패널 전환 컨트롤러
/// - 버튼 클릭 시 현재 패널을 끄고, 실제 결제 진행 패널(WaitingForPayment)을 켬
/// - GameManager 상태도 WaitingForPayment 로 설정
/// </summary>
public class PaymentWaitingPanelTransitionCtrl : MonoBehaviour
{
    [Header("Component Setting")]
    [SerializeField] private PaymentHttpTester _paymentHttpTester;
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    [Header("Button")]
    [SerializeField] private Button _goToPaymentButton;
    // "결제하기", "결제 시작" 같은 버튼

    [Header("Panel Settings")]
    [SerializeField] private GameObject _currentPanel;
    // 수량 선택 + 결제수단 선택 같은 "결제 설정 화면" 패널

    [SerializeField] private GameObject _waitingForPaymentPanel;

    [SerializeField] private GameObject _cameraWindowPanel;
    // PanelCameraWindow - 촬영 버튼 클릭 시 꺼지고, 결제 완료 시 다시 켜짐

    public bool _isAdmin = false;
    // 실제 결제 진행 패널 (여기에 PaymentPanelEnableBroadcaster 가 붙어 있으면
    // SetActive(true) 되는 순간 OnPaymentPanelEnabled 이벤트가 날아감)

    private void Awake()
    {
        if (_goToPaymentButton != null)
        {

            _goToPaymentButton.onClick.AddListener(OnClickGoToPayment);
        }
        else
        {
            Debug.LogWarning("[PaymentWaitingPanelTransitionCtrl] _goToPaymentButton reference is missing");
        }
    }
    private void OnDestroy()
    {
        if (_goToPaymentButton != null)
        {
            _goToPaymentButton.onClick.RemoveListener(OnClickGoToPayment);
        }
    }

    /// <summary>
    /// "결제하기" 버튼 클릭 시 호출
    /// - 상태를 WaitingForPayment 로 변경
    /// - 현재 패널 OFF, 결제 대기 패널 ON
    /// </summary>
    public void OnClickGoToPayment()
    {
        if (!_isAdmin)
        {
            _paymentHttpTester.OnClickStartPayment();
        }
        else
        {
            // 여기서 결제가 완료되면 콜백 받아서 아래 코드 실행시킬거임 (1209)
            // Payment → Guide(촬영 안내) 화면 전환
            _fadeAnimationCtrl.SetState(FadeState.PaymentToGuide);
            _fadeAnimationCtrl.StartFade();
        }

    }
    public void PaymentHttpTesterStart()
    {
        // Payment → Guide(촬영 안내) 화면 전환
        _fadeAnimationCtrl.SetState(FadeState.PaymentToGuide);
        _fadeAnimationCtrl.StartFade();
    }
    /// <summary>
    /// 외부 호출용 콜백 (Fade End에서 실행될 예정)
    /// </summary>
    public void FadeEndCallBack()
    {
        print("여기 호출 몇번됨?");
        // 키오스크 상태를 "결제 대기" 로 설정
        GameManager.Instance.SetState(KioskState.WaitingForPayment);

        // 효과음도 원하면 여기서 재생
        // SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._paymentStartButton);

        // 패널 전환
        if (_currentPanel != null)
            _currentPanel.SetActive(false);
        else
            Debug.LogWarning("[PaymentWaitingPanelTransitionCtrl] _currentPanel reference is missing");

        if (_waitingForPaymentPanel != null)
            _waitingForPaymentPanel.SetActive(true);
        else
            Debug.LogWarning("[PaymentWaitingPanelTransitionCtrl] _waitingForPaymentPanel reference is missing");
    }

    /// <summary>
    /// [외부 호출용] 코드에서 직접 결제 대기 패널로 보내고 싶을 때 사용
    /// (버튼 클릭 없이도 호출 가능)
    /// </summary>
    public void ForceOpenWaitingForPayment()
    {
        OnClickGoToPayment();
    }

    /// <summary>
    /// Filming → Payment 전환 시 호출
    /// - 촬영 버튼 클릭 후 결제 화면으로 전환
    /// - PanelCameraWindow 끄고 PanelPayment 켬
    /// </summary>
    public void OnPaymentWaiting()
    {
        // 키오스크 상태를 "결제" 로 설정
        GameManager.Instance.SetState(KioskState.Payment);

        // 카메라 윈도우 비활성화
        if (_cameraWindowPanel != null)
            _cameraWindowPanel.SetActive(false);

        // 결제 패널 활성화
        if (_currentPanel != null)
            _currentPanel.SetActive(true);
        else
            Debug.LogWarning("[PaymentWaitingPanelTransitionCtrl] _currentPanel reference is missing");
    }

    /// <summary>
    /// [더 이상 직접 사용되지 않음 - GuidePanelCtrl로 대체됨]
    /// 결제 완료 후 촬영 시작 시 호출
    /// - PanelPayment 끄고 PanelCameraWindow 다시 켬
    /// </summary>
    public void OnPaymentCompletedToFilming()
    {
        // 결제 패널 비활성화
        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        // 카메라 윈도우 다시 활성화
        if (_cameraWindowPanel != null)
            _cameraWindowPanel.SetActive(true);
    }

    /// <summary>
    /// Payment → Filming 뒤로가기 시 호출
    /// - 결제 패널만 끔 (카메라 윈도우는 FilmingPanelCtrl에서 처리)
    /// </summary>
    public void ClosePaymentPanel()
    {
        if (_currentPanel != null)
            _currentPanel.SetActive(false);
    }
}
