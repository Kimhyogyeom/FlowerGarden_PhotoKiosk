using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 페이드 애니메이션 상태 관리용 Enum
/// - 화면 전환 시 사용되는 모든 상태를 명시적으로 정의
/// - 기존 int 기반 _isStateStep을 대체
/// </summary>
public enum FadeState
{
    // ===== 초기화 상태 =====
    /// <summary>Ready 화면 초기화 (리셋 시 돌아오는 상태)</summary>
    InitializeReady,

    /// <summary>Ready → Mode 화면 전환 시작</summary>
    ReadyToMode,

    // ===== 메인 플로우 (화면 전환) =====
    /// <summary>Mode → Select 화면 전환</summary>
    ModeToSelect,

    /// <summary>Select → Filming(촬영) 화면 전환</summary>
    SelectToFilming,

    /// <summary>Filming → Quantity(수량) 화면 전환</summary>
    FilmingToQuantity,

    /// <summary>Quantity → Payment(결제) 화면 전환</summary>
    QuantityToPayment,

    /// <summary>Payment → Filming Start(촬영 시작) 화면 전환</summary>
    PaymentToFilmingStart,

    /// <summary>Filming Start → Captured Photo(포토 선택) 화면 전환</summary>
    FilmingStartToPhotoSelect,

    /// <summary>Photo Select → Sticker(스티커 편집) 화면 전환</summary>
    PhotoSelectToSticker,

    /// <summary>Sticker → Print(프린트) 화면 전환</summary>
    StickerToPrint,

    /// <summary>Print → Output(출력 완료) 화면 전환</summary>
    PrintToOutput,

    /// <summary>Output → Ready(초기화) 화면 복귀</summary>
    OutputToReady,

    // ===== 홈 버튼 (각 화면에서 Ready로 복귀) =====
    /// <summary>Mode 화면에서 홈 버튼 클릭</summary>
    HomeFromMode,

    /// <summary>Select 화면에서 홈 버튼 클릭</summary>
    HomeFromSelect,

    /// <summary>Quantity 화면에서 홈 버튼 클릭</summary>
    HomeFromQuantity,

    /// <summary>Payment 화면에서 홈 버튼 클릭</summary>
    HomeFromPayment,

    /// <summary>Chroma Key 화면에서 홈 버튼 클릭</summary>
    HomeFromChromaKey,

    // ===== 뒤로가기 버튼 (이전 화면으로 복귀) =====
    /// <summary>Mode → Ready 뒤로가기</summary>
    BackFromMode,

    /// <summary>Select → Mode 뒤로가기</summary>
    BackFromSelect,

    /// <summary>Quantity → Select 뒤로가기</summary>
    BackFromQuantity,

    /// <summary>Payment → Quantity 뒤로가기</summary>
    BackFromPayment,

    /// <summary>Chroma Key → Select 뒤로가기</summary>
    BackFromChromaKey,
}

/// <summary>
/// 화면 전환용 페이드 애니메이션 제어 스크립트
/// - Ready / Select / Filming / Ready 로 이어지는 패널 전환의 "게이트" 역할
/// - 외부(ReadyPanelTransitionCtrl, FilmingPanelCtrl, InitCtrl, FilmingToSelectCtrl)에서
///   StartFade()를 호출하면 페이드 인/아웃 실행
/// - 애니메이션 마지막 프레임에서 Animation Event로 OnFadeEnd()가 호출되며,
///   CurrentState 값에 따라 다음 화면으로 전환
/// </summary>
public class FadeAnimationCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private InitCtrl _initCtrl;
    // 초기화 및 패널 전환 총괄 컨트롤러

    [Space(10)]
    [SerializeField] private Animator _fadeAnimator;
    // Fade 애니메이션을 재생하는 Animator
    [SerializeField] private ReadyPanelTransitionCtrl _readyPanelTransitionCtrl;
    // Ready → Camera 패널 전환 담당
    [SerializeField] private FilmingPanelCtrl _filmingPanelCtrl;
    // 프레임 선택 → 촬영 패널 전환 담당
    // [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;s
    // 촬영 화면 → 선택 화면으로 돌아갈 때 사용

    [SerializeField] private ChromakeyPanelCtrl _chromakeyPanelCtrl;

    // [SerializeField] private PaymentCtrl _paymentCtrl;
    // 결제 완료 시스템
    [SerializeField] private QuantityToPaymentCtrl _quantityToPaymentCtrl;
    // 수량 -> 결제 컨트롤러
    [SerializeField] private PaymentToNextStageCtrl _paymentToNextStageCtrl;
    // 결제 -> 결제완료 자동 : (결제 완료 -> 필름 패널로 변경)
    [SerializeField] private HomAndBackButtonCtrl _homeButtonCtrl;
    // 홈 버튼 누르면 실행될 제어 컨트롤러 

    [SerializeField] private PaymentWaitingPanelTransitionCtrl _paymentWatingPanelTranstionCtrl;
    // 이거 결제 -> 결제 대기 할 때 로직 컨트롤러

    [SerializeField] private PrintButtonHandler _printButtonHandler;

    [SerializeField] private CapturedPhotoPanelCtrl _capturePhotoPanelCtrl;
    // 촬영 끝나고 포토 선택 화면으로 페이드 인아웃 되게끔?

    [SerializeField] private WindowModePanelCtrl _windowModePanelCtrl;


    // 근데 오토 안 쓸 예정 (최대한 디자인 된 PDF 파일 따라하고 추후 시간날때 할 예정?)
    [Header("Auto")]
    [SerializeField] private ReadyAutoTransitionCtrl _readyAutoTransitionCtrl;      // 페이드 아웃 됐을때 타이머 호출
    [SerializeField] private SelectAutoTransitionCtrl _selectAutoTransitionCtrl;    // 페이드 아웃 될 때 타이머 호출
    [SerializeField] private AutoShootStartCtrl _autoShootStartCtrl;                // 페이드 아웃 될 때 타이머 호출

    // [결제 시스템 없는 버전으로 테스트용 추가]
    [SerializeField] private GameObject _panelWaitingForPayment;
    [SerializeField] private GameObject _panelPayment;


    /// <summary>
    /// 현재 페이드 애니메이션 상태
    /// - Enum으로 관리하여 가독성 및 타입 안정성 확보
    /// - 외부에서는 SetState()를 통해서만 변경 가능
    /// </summary>
    public FadeState CurrentState { get; private set; } = FadeState.InitializeReady;

    /// <summary>
    /// 페이드 시작 (외부에서 버튼 클릭 시 호출)  
    /// - Animator의 "Fade" Bool 파라미터를 true로 설정하여 페이드 인 시작  
    /// - 페이드 인 사운드 재생
    /// </summary>
    public void StartFade()
    {
        print("StartFade 호출이요~!");

        if (_fadeAnimator != null)
        {
            _fadeAnimator.SetBool("Fade", true);
            // Sound
        }
        else
        {
            UnityEngine.Debug.LogWarning("_fadeAnimator reference is missing");
        }
    }

    /// <summary>
    /// 상태 변경 메서드 (외부에서 호출)
    /// - private set을 통해 외부에서 직접 CurrentState 수정 불가
    /// - 이 메서드를 통해서만 상태 변경 가능 (디버그 로그 추가 가능)
    /// </summary>
    public void SetState(FadeState newState)
    {
        CurrentState = newState;
        // UnityEngine.Debug.Log($"[FadeState] {CurrentState}");
    }

    private void Update()
    {
        // 디버그용 (상태 값 확인용)
        // UnityEngine.Debug.Log($"CurrentState : {CurrentState}");
    }

    /// <summary>
    /// 애니메이션 이벤트(Animation Event)에서 호출됨
    /// - 페이드 애니메이션이 끝나는 타이밍에 Animator 상태 복구
    /// - CurrentState 상태 값에 따라 다음 패널 전환/초기화 로직 실행
    /// </summary>
    public void OnFadeEnd()
    {
        print("OnFadeEnd 호출이요~!");

        if (_fadeAnimator == null)
        {
            UnityEngine.Debug.LogWarning("_fadeAnimator reference is missing");
            return;
        }

        // 페이드 애니메이션 플래그 초기화 및 페이드 아웃 사운드 재생
        _fadeAnimator.SetBool("Fade", false);
        // Sound

        // switch 문으로 가독성 향상
        switch (CurrentState)
        {
            // ===== 메인 플로우 (화면 전환) =====
            case FadeState.ReadyToMode:
                // Ready → Mode 전환
                CurrentState = FadeState.ModeToSelect;
                if (_readyPanelTransitionCtrl != null)
                {
                    _readyPanelTransitionCtrl.OnFadeFinished();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_readyPanelTransitionCtrl reference is missing");
                }
                break;

            case FadeState.ModeToSelect:
                // Mode → Select 전환
                CurrentState = FadeState.SelectToFilming;
                if (_windowModePanelCtrl != null)
                {
                    _windowModePanelCtrl.FadeFinishEvent();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_windowModePanelCtrl reference is missing");
                }
                break;

            case FadeState.SelectToFilming:
                // Select → Filming(Quantity) 전환
                CurrentState = FadeState.FilmingToQuantity;
                if (_filmingPanelCtrl != null)
                {
                    _filmingPanelCtrl.PanelChanger();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_filmingPanelCtrl reference is missing");
                }
                break;

            case FadeState.FilmingToQuantity:
                // Quantity → Payment 전환
                CurrentState = FadeState.QuantityToPayment;
                _quantityToPaymentCtrl.ObjectActiveCtrl();
                break;

            case FadeState.QuantityToPayment:
                // Payment → Filming Start 전환
                CurrentState = FadeState.PaymentToFilmingStart;
                _paymentToNextStageCtrl.OnPaymentCompleted();
                break;

            case FadeState.PaymentToFilmingStart:
                // Filming Start → Photo Select 전환
                CurrentState = FadeState.FilmingStartToPhotoSelect;
                _filmingPanelCtrl.FadeEndCallBack();
                break;

            case FadeState.FilmingStartToPhotoSelect:
                // Photo Select → Sticker 전환
                CurrentState = FadeState.PhotoSelectToSticker;
                _capturePhotoPanelCtrl.FadeEndCallBack();
                break;

            case FadeState.PhotoSelectToSticker:
                // Photo Select → Sticker 전환 완료
                CurrentState = FadeState.StickerToPrint;
                if (_capturePhotoPanelCtrl != null)
                {
                    _capturePhotoPanelCtrl.FadeEndCallBackToSticker();
                }
                break;

            case FadeState.StickerToPrint:
                // Print → Output 전환
                CurrentState = FadeState.PrintToOutput;
                _printButtonHandler.FadeEndCallBack();
                break;

            case FadeState.PrintToOutput:
                // Output → Ready (초기화)
                CurrentState = FadeState.InitializeReady;
                _initCtrl.PanaelActiveCtrl();
                break;

            // ===== 홈 버튼 (모든 화면 → Ready로 복귀) =====
            case FadeState.HomeFromMode:
            case FadeState.HomeFromSelect:
            case FadeState.HomeFromQuantity:
            case FadeState.HomeFromPayment:
            case FadeState.HomeFromChromaKey:
                UnityEngine.Debug.Log($"[Home Button] {CurrentState} → Ready");
                CurrentState = FadeState.InitializeReady;
                _homeButtonCtrl.ObjectsActiveCtrlReset();
                break;

            // ===== 뒤로가기 버튼 (이전 화면으로 복귀) =====
            case FadeState.BackFromMode:
                // Mode → Ready
                CurrentState = FadeState.InitializeReady;
                _homeButtonCtrl.ObjectsActiveCtrlMod();
                break;

            case FadeState.BackFromSelect:
                // Select → Mode
                CurrentState = FadeState.ModeToSelect;
                _homeButtonCtrl.ObjectsActiveCtrlSel();
                break;

            case FadeState.BackFromQuantity:
                // Quantity → Select
                CurrentState = FadeState.SelectToFilming;
                _homeButtonCtrl.ObjectsActiveCtrlQua();
                break;

            case FadeState.BackFromPayment:
                // Payment → Quantity
                CurrentState = FadeState.FilmingToQuantity;
                _homeButtonCtrl.ObjectsActiveCtrlPay();
                break;

            case FadeState.BackFromChromaKey:
                // Chroma → Select
                CurrentState = FadeState.SelectToFilming;
                _homeButtonCtrl.ObjectsActiveCtrlChr();
                break;

            default:
                UnityEngine.Debug.LogWarning($"[FadeState] Unhandled state: {CurrentState}");
                break;
        }
    }
    private void CoroutineAllStopFunction()
    {
        _readyAutoTransitionCtrl.StopAndResetTimer();
        _selectAutoTransitionCtrl.StopAutoTransitionTimer();
        _autoShootStartCtrl.ResetAutoShootStartCtrl();
    }
}
