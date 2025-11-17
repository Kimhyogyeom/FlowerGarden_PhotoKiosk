using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈 버튼 클릭시 제어할 스크립트
/// </summary>
public class HomButtonCtrl : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;  // 페이드ㅡ 애니메이션 컨트롤러


    [Header("Object Settings commonness")]
    [SerializeField] private GameObject[] _currentPanel;  // 숨길 패널들



    [Header("Object Settings - Select")]
    [SerializeField] private Button _selBackButton;    // 뒤로가기 버튼
    [SerializeField] private Button _selHomeButton;    // 홈 버튼
    [SerializeField] private GameObject _selChangePanel;     // 오픈할 패널 


    [Header("Object Settings - Quantity")]
    [SerializeField] private Button _quaBackButton;
    [SerializeField] private Button _quaHomeButton;    // 홈 버튼
    [SerializeField] private GameObject _quaChangePanel;     // 오픈할 패널 

    [Header("Object Settings - Payment}")]
    [SerializeField] private Button _payBackButton;
    [SerializeField] private Button _payHomeButton;    // 홈 버튼
    [SerializeField] private GameObject _payChangePanel;     // 오픈할 패널 
    void Awake()
    {
        // [Select]
        if (_selHomeButton != null) _selHomeButton.onClick.AddListener(OnHomeButtonClickSel);
        if (_selBackButton != null) _selBackButton.onClick.AddListener(OnHomeButtonClickSel);

        // [Select]
        if (_quaHomeButton != null) _quaHomeButton.onClick.AddListener(OnHomeButtonClickQUan);
        if (_quaBackButton != null) _quaBackButton.onClick.AddListener(OnBackButtonClickQUan);

        // [payment]
        if (_payHomeButton != null) _payHomeButton.onClick.AddListener(OnHomeButtonClickPay);
        if (_payBackButton != null) _payBackButton.onClick.AddListener(OnBackButtonClickQPay);
    }

    // ========================================Select
    /// <summary>
    /// Select 홈 버튼 누르면 실행될 함수
    /// </summary>
    private void OnHomeButtonClickSel()
    {
        _fadeAnimationCtrl._isStateStep = 101;
        _fadeAnimationCtrl.StartFade();
        GameManager.Instance.SetState(KioskState.Ready);
    }
    /// <summary>
    /// 외부에서 호출할 오브젝트 활성 및 비활성화 함수
    /// </summary>
    public void ObjectsActiveCtrlSel()
    {
        foreach (var item in _currentPanel)
        {
            item.gameObject.SetActive(false);
        }
        _selChangePanel.SetActive(true);
    }
    // ========================================Select


    // ========================================Quantity
    /// <summary>
    /// Quantity 홈 버튼 누르면 실행될 함수
    /// </summary>
    private void OnHomeButtonClickQUan()
    {
        _fadeAnimationCtrl._isStateStep = 102;
        _fadeAnimationCtrl.StartFade();
        GameManager.Instance.SetState(KioskState.Ready);
    }
    /// <summary>
    /// Quantity 백 버튼 누르면 실행될 함수
    /// </summary>
    private void OnBackButtonClickQUan()
    {
        _fadeAnimationCtrl._isStateStep = 201;
        _fadeAnimationCtrl.StartFade();
    }
    public void ObjectsActiveCtrlQua()
    {
        foreach (var item in _currentPanel)
        {
            item.gameObject.SetActive(false);
        }
        _quaChangePanel.SetActive(true);
        GameManager.Instance.SetState(KioskState.Select);
    }
    // ========================================Quantity


    // ========================================Payment
    /// <summary>
    /// Payment 홈 버튼 누르면 실행될 함수
    /// </summary>
    private void OnHomeButtonClickPay()
    {
        _fadeAnimationCtrl._isStateStep = 103;
        _fadeAnimationCtrl.StartFade();
        GameManager.Instance.SetState(KioskState.Ready);
    }
    /// <summary>
    /// Payment 백 버튼 누르면 실행될 함수
    /// </summary>
    private void OnBackButtonClickQPay()
    {
        _fadeAnimationCtrl._isStateStep = 202;
        _fadeAnimationCtrl.StartFade();
    }
    public void ObjectsActiveCtrlPay()
    {
        foreach (var item in _currentPanel)
        {
            item.gameObject.SetActive(false);
        }
        _payChangePanel.SetActive(true);
        GameManager.Instance.SetState(KioskState.Quantity);
    }
    // ========================================Payment
}
