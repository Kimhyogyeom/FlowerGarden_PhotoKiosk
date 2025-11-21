// 대기(Init) 컨트롤러

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 초기화/귀환(돌아가기) 총 관리자
/// - 인쇄 완료 후 일정 시간 동안 아무 입력이 없으면 자동으로 초기 화면으로 복귀
/// - '돌아가기' 버튼 클릭 시 전체 흐름/상태를 초기화
/// - 촬영, 프레임 선택, 프린트, 버튼 상태 등을 한 번에 리셋
/// </summary>
public class InitCtrl : MonoBehaviour
{
    [Header("Add")]
    [SerializeField] private Button _initButton;            // 돌아가기(초기화) 버튼
    [SerializeField] private TextMeshProUGUI _initText;     // 버튼 옆/위에 카운트다운 표시용 텍스트
    [SerializeField] private int _originTimerValue;
    private Coroutine _resetCallbackRoutine = null;         // 자동 리셋 코루틴
    // [SerializeField] private int g_successToBackTime = 10;   // 인쇄 후 초기 화면으로 돌아가기까지 대기 시간(초)

    [Header("Setting Component")]
    [SerializeField] private PhotoFrameSelectCtrl _photoFrameSelectCtrl;    // 프레임 선택 컨트롤러
    [SerializeField] private QuantitySelectCtrl _quantitySelectCtrl;        // 디자인 적용 이후 : 수량 컨트롤러
    [SerializeField] private PaymentMethodSelector _paymentMethodSelector;  // 디자인 적용 이후 : 페이먼트 컨트롤러
    [SerializeField] private FilmingPanelCtrl _filmingPanelCtrl;            // 디자인 적용 이후 : 촬영 컨트롤러
    [SerializeField] private PrintController _printController;              // 프린트 컨트롤러
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;          // 페이드 연출 컨트롤러
    [SerializeField] private PrintButtonHandler _printButtonHandler;        // 출력 버튼 핸들러
    [SerializeField] private StepCountdownUI _stepCountdownUI;              // 촬영 카운트다운 컨트롤러
    // [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;      // 촬영 → 선택 화면 전환 컨트롤러
    // [SerializeField] private FilmingEndCtrl _filmingEndCtrl;                // 촬영 종료 후 처리 컨트롤러 (필요시 확장용)    


    [Header("Setting Object")]
    [SerializeField] private Button _photoButton;               // 촬영 버튼
    // [SerializeField] private GameObject _photoButtonFake;       // 촬영 중 대체/가짜 버튼
    // [SerializeField] private Image _photoImage;                 // 필요 시 사용하는 이미지(예: 프리뷰 등)
    // [SerializeField] private TextMeshProUGUI _buttonText;       // 촬영 버튼 텍스트
    private ColorBlock _originColor;                            // 촬영 버튼 원래 색상 저장용

    [Space(10)]
    [SerializeField] private GameObject _currentPanel;  // 현재 인쇄 완료 Ready패널
    [Tooltip("바뀔 프레임")]
    [SerializeField] private GameObject _changePanel;   // 다시 돌아갈 패널(현재는 결제/대기 패널)
    // [SerializeField] private GameObject _cameraFocus;   // 카메라 조준점(촬영 가이드용)

    [Header("Filming")]
    // [SerializeField] private GameObject _stepsObject;                   // 1~4(5) 스텝 표시 UI
    // [SerializeField] private string _takePictureString = "사진찍기";     // 촬영 버튼 기본 문구
    // [SerializeField] private TextMeshProUGUI _exitMessageText;          // 촬영 종료 안내 텍스트
    // [SerializeField, TextArea(4, 5)]
    // private string _exitMessageString = "사진 촬영이 종료되었습니다.\n사진을 출력하세요."; // 종료 안내 기본 문구

    // [SerializeField] private GameObject _exitMessage;   // 종료 안내 메시지 오브젝트

    // [SerializeField] private GameObject[] _photoNumberObjs; // 각 컷 번호 아이콘/텍스트 오브젝트
    [SerializeField] private TextMeshProUGUI _missionText;  // 미션 텍스트 출력용

    [Header("Test")]
    [SerializeField] private GameObject _startFilming;       // (테스트용) 촬영 시작 UI
    [SerializeField] private GameObject _endFilming;         // (테스트용) 촬영 종료 UI
    [SerializeField] private Button _endFilimgButton;        // (테스트용) 촬영 종료 버튼

    [Header("Slider")]
    [SerializeField] private GameObject _filimgObject;           // 촬영 중 버튼 오브젝트
    [SerializeField] private GameObject _finishedFilimgObject;   // 촬영 완료 후 버튼 오브젝트
    [SerializeField] private Image _progressFillImage;           // 진행 바(프로그레스 바) 이미지

    private void Awake()
    {
        // 초기화(돌아가기) 버튼 클릭 리스너 등록
        _initButton.onClick.AddListener(ResetManager);
        _originColor = _photoButton.colors;
        // _originTimerValue = GameManager.Instance._printToSuccessTimer;
    }

    /// <summary>
    /// 비활성화 될 때 코루틴이 남아 있지 않도록 방지
    /// </summary>
    private void OnDisable()
    {
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }
    }

    /// <summary>
    /// 파괴될 때도 방어적으로 코루틴 정지
    /// (실제 운용에서는 파괴될 일은 거의 없겠지만 예외 상황 대비)
    /// </summary>
    private void OnDestroy()
    {
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }
    }

    // ────────────────────────────────────────────
    // 인쇄 완료 후, 사용자가 '돌아가기' 버튼을 누르지 않았을 때 자동으로 초기화
    // 현재  _successToBackTime 초 동안 아무 입력이 없으면 자동으로 ResetManager() 호출
    // ────────────────────────────────────────────

    /// <summary>
    /// 자동 초기화(콜백) 시작용 함수
    /// - 인쇄 완료 시점 등에서 호출
    /// - 기존 코루틴이 있으면 정지 후 다시 시작
    /// </summary>
    public void ResetCallBack()
    {
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }
        _resetCallbackRoutine = StartCoroutine(ResetCallBackCoroutine());
    }

    /// <summary>
    /// 일정 시간 카운트다운 후 자동으로 ResetManager 호출
    /// </summary>
    private IEnumerator ResetCallBackCoroutine()
    {

        _originTimerValue = GameManager.Instance._printToSuccessTimer;

        for (int i = _originTimerValue; i >= 1; i--)
        {
            if (_initText != null)
                _initText.text = $"{i}";

            yield return new WaitForSeconds(1f);
        }

        // 지정 시간이 모두 지나면 자동 초기화
        ResetManager();


    }

    // ─────────────────────────────────────────────────────────
    // 리셋 총 관리자
    // - 각종 서브 시스템 리셋 함수들을 순서대로 호출
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 전체 리셋 총괄 함수
    /// - 자동 콜백 코루틴 정지
    /// - 텍스트/버튼/프레임/촬영/프린트/핸들러 등 일괄 초기화
    /// - 페이드 연출과 함께 초기 상태로 복귀
    /// </summary>
    private void ResetManager()
    {
        // 자동 콜백 코루틴 정리
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }

        // 코루틴 참조 초기화
        _resetCallbackRoutine = null;


        // 효과음 + 페이드 시작
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._outputSuccess);
        _fadeAnimationCtrl.StartFade();

        // ─────────────────────────────────────────────────────────
        FrameSelectReset();     // 프레임 관련 리셋
        FilmingPanelReset();    // 촬영 패널 관련 리셋
        CaptureReset();         // 캡처 관련 리셋
        PrintHandlerReset();    // 출력 핸들러 관련 리셋
        PrintReset();           // 프린트 관련 리셋        
        // ─────────────────────────────────────────────────────────
        ButtonReset();          // 기타 버튼/테스트 관련 리셋
        // ─────────────────────────────────────────────────────────
        //          디자인적용 이후 리셋 함수 추가 작업 1119
        // ─────────────────────────────────────────────────────────
        QuantityReset();        // 수량 관련
        PaymentReset();         // 페이먼트 관련
        FilmingReset();         // 촬영 관련

        // '돌아가기' 카운트 텍스트 초기화
        if (_initText != null)
            _initText.text = "";
        // '버튼 오브젝트 비활성화'
        if (_initButton != null)
            _initButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 프레임 선택 관련 리셋
    /// - PhotoFrameSelectCtrl 의 AllReset 호출
    /// </summary>
    private void FrameSelectReset()
    {
        _photoFrameSelectCtrl.AllReset();
    }

    /// <summary>
    /// 촬영 패널 관련 리셋
    /// - 스텝 UI, 촬영 버튼 색/텍스트, 가짜 버튼, 메시지, 미션, 번호 UI 등 초기화
    /// </summary>
    private void FilmingPanelReset()
    {
        // _stepsObject.SetActive(true);

        // _photoButton.colors = _originColor;
        // _buttonText.color = Color.black;
        // _buttonText.text = _takePictureString;

        // _photoButtonFake.SetActive(false);

        // _exitMessageText.text = _exitMessageString;
        // _exitMessage.SetActive(false);

        // _cameraFocus.SetActive(true);

        // 미션 텍스트 카운트 초기화
        // _stepCountdownUI._missionCount = 0;
        // 미션 텍스트 초기화
        // _missionText.text = "";

        // // 각 컷 번호 아이콘 다시 활성화
        // foreach (var item in _photoNumberObjs)
        // {
        //     item.SetActive(true);
        // }

        {
            // 미션 텍스트 카운트 초기화
            _stepCountdownUI._missionCount = 0;
            // 미션 텍스트 초기화
            _missionText.text = "";
        }
    }

    /// <summary>
    /// 캡처 관련 리셋
    /// - StepCountdownUI 의 캡처/슬롯/상태 초기화
    /// </summary>
    private void CaptureReset()
    {
        _stepCountdownUI.ResetSequence();
    }

    /// <summary>
    /// 프린트 핸들러 관련 리셋
    /// - 출력 버튼에 걸려 있는 카운트다운 및 상태 초기화
    /// </summary>
    private void PrintHandlerReset()
    {
        // _printButtonHandler.ResetPrintButtonHandler();
        _printButtonHandler.ResetAndRestartCountdown();
    }

    /// <summary>
    /// 프린트 관련 리셋
    /// - PrintController 의 내부 상태/옵션/임시 파일 등을 초기화
    /// </summary>
    private void PrintReset()
    {
        _printController.ResetPrintState();
    }

    /// <summary>
    /// 버튼 및 테스트용 상태 리셋
    /// - 촬영 시작/종료 테스트 오브젝트
    /// - 촬영 중/완료 버튼 그룹
    /// - 진행바, 뒤로가기 버튼 상태 등
    /// </summary>
    private void ButtonReset()
    {
        _startFilming.SetActive(true);
        // _endFilming.SetActive(false);    // 이거 초기화 할 필요 없이 항시 뜨게 변경됨
        _endFilimgButton.interactable = true;
        _printButtonHandler._busy = false;

        _filimgObject.SetActive(true);
        _finishedFilimgObject.SetActive(false);

        // 프로그래스바 초기화
        _progressFillImage.fillAmount = 0;

        // 뒤로가기 버튼 활성화
        // _filmingToSelectCtrl.ButtonActive();
    }

    /// <summary>
    /// 촬영 종료 후 → 결제/대기 화면(Ready/결제 쪽 패널)으로 전환
    /// ※ 현재(1110) 수정/실험 중인 흐름
    /// </summary>
    public void PanaelActiveCtrl()
    {
        GameManager.Instance.SetState(KioskState.Ready);
        _currentPanel.SetActive(false);
        _changePanel.SetActive(true);
    }

    // ──────────────────────────────────────────────────────────────────
    //              디자인 적용 이후 새로 추가된 패널 리셋 관련 ↓↓↓↓↓↓↓ 
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 수량 선택 관련 리셋
    /// </summary>
    public void QuantityReset()
    {
        _quantitySelectCtrl.ResetQuantity();
    }
    /// <summary>
    /// 페이먼트 관련 리셋
    /// </summary>
    public void PaymentReset()
    {
        _paymentMethodSelector.ResetSelection();
    }
    /// <summary>
    /// 촬영 관련 리셋
    /// </summary>
    public void FilmingReset()
    {
        _filmingPanelCtrl.ResetFilming();
    }
}
