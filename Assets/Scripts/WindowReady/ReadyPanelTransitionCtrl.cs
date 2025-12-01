using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ready 패널에서 다음 단계로 전환을 제어하는 컨트롤러
/// - 시작 버튼 클릭 시 FadeAnimationCtrl 에게 페이드 요청
/// - 페이드가 끝나면 ReadyPanel 비활성화, CameraPanel 활성화
/// </summary>
public class ReadyPanelTransitionCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    // 페이드 연출을 담당하는 컨트롤러
    // StartFade() 를 호출해서 페이드 시작

    [Header("Setting Object")]
    [SerializeField] private GameObject _readyPanel;
    // 처음 대기 화면(Ready 창) 패널

    [SerializeField] private GameObject _cameraPanel;
    // 촬영 단계로 넘어갔을 때 활성화되는 카메라 패널

    [SerializeField] private Button _startButton;
    // Ready 화면에서 눌러서 다음 단계로 넘어가는 시작 버튼

    /// <summary>
    /// 시작 버튼 클릭 이벤트 등록
    /// </summary>
    private void Awake()
    {
        SoundManager.Instance.PlayBGM(SoundManager.Instance._soundDatabase._basicBGM);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._windowReady);
        if (_startButton != null)
        {
            // 시작 버튼 클릭 시 OnReadyClicked 실행
            _startButton.onClick.AddListener(OnReadyClicked);
        }
        else
        {
            Debug.LogWarning("_startButton reference is missing");
        }
    }

    /// <summary>
    /// 메모리 누수 방지를 위한 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnReadyClicked);
        }
        else
        {
            Debug.LogWarning("_startButton reference is missing");
        }
    }

    /// <summary>
    /// 시작 버튼 클릭 시 호출
    /// - 게임 상태를 Select 로 변경
    /// - 페이드 애니메이션 시작 요청
    /// - 시작 버튼 사운드 출력
    /// + 외부 호출용으로 추가~
    /// </summary>
    public void OnReadyClicked()
    {
        if (_fadeAnimationCtrl != null)
        {
            // 상태 변경 (Ready 화면에서 선택 단계로 전환)
            GameManager.Instance.SetState(KioskState.Mode);
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
            // 페이드 애니메이션 시작
            _fadeAnimationCtrl.StartFade();

            // 시작 버튼 효과음 재생
            // Sound
        }
        else
        {
            Debug.LogWarning("_fadeAnimationCtrl reference is missing");
        }
    }

    /// <summary>
    /// FadeAnimationCtrl 에서 페이드 완료 콜백으로 호출되는 함수
    /// - ReadyPanel 비활성화
    /// - CameraPanel 활성화
    /// </summary>
    public void OnFadeFinished()
    {
        if (_readyPanel != null && _cameraPanel != null)
        {
            // Ready 화면 닫기
            _readyPanel.SetActive(false);

            // 카메라 화면 켜기
            _cameraPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("_readyPanel or _cameraPanel reference is missing");
        }
    }
}
