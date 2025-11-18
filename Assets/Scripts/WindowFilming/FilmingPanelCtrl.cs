using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 촬영 패널 전체 흐름 컨트롤러
/// - 프레임 선택 → 촬영 화면 전환
/// - 촬영 버튼 클릭 시 UI 상태 변경 + StepCountdownUI 시퀀스 시작
/// </summary>
public class FilmingPanelCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private StepCountdownUI _stepCountdownUI;
    // 실제 촬영 단계(카운트다운, 캡처, 인쇄 등)를 담당하는 컨트롤러

    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    // 패널 전환 시 사용하는 페이드 애니메이션 컨트롤러

    // [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;
    // 촬영 이후 선택 화면으로 돌아가는 흐름 제어 컨트롤러

    [Header("Setting Object")]
    [SerializeField] private Button _selectPhotoButton;
    // "사진 촬영" 모드로 진입하는 버튼 (프레임 선택 화면에서 사용)

    [SerializeField] private GameObject _currentPanel;
    // 현재 보여지고 있는 패널 (프레임 선택 + 설명 등)

    [SerializeField] private GameObject _changedPhotoPanel;
    // 페이드 이후에 보여줄 촬영용 패널

    [SerializeField] private Button _photoButton;
    // 카메라 화면에서 실제로 촬영을 시작하는 버튼

    [SerializeField] private GameObject _photoButtonFake;
    // 촬영 중에 실 버튼 대신 보여줄 페이크 버튼(잠금용/연출용)

    [SerializeField] private TextMeshProUGUI _buttonText;
    // 촬영 버튼 주변에 보여줄 텍스트 (예: "촬영중" 등 상태 표시)

    // [SerializeField] private GameObject _stepsObject;
    // 촬영 전에 보여줄 단계 안내 UI 오브젝트

    [SerializeField] private GameObject _descriptionFingerObject;
    // 손가락 설명(가이드) UI 오브젝트 (촬영 버튼 안내용)

    // [SerializeField] private GameObject _cameraFocus;
    // 카메라 중앙 포커스 표시용 오브젝트

    [Header("Setting Color")]
    [SerializeField] private Color _activeColor = Color.red;
    // 촬영 버튼이 활성화(촬영 중) 되었을 때 사용할 버튼 컬러

    [SerializeField] private Color _textColor = Color.white;
    // 촬영 중에 버튼 텍스트에 적용할 색상

    // 추가 ──────────────────────────────────────────────────────────── 
    // 해봐야 6개밖에 안되는데 배열로 나눠서 관리할지 고민?
    [SerializeField] private GameObject _titleGameobjectEnd;    // 기본 타이틀 (촬영시작 버튼을 눌러주세요)
    [SerializeField] private GameObject _titleGameobjectStart;  // 변경 타이틀 (움직이지 말라)

    [SerializeField] private GameObject _descriptionBackgroundEnd;      // 기본 백그라운드
    [SerializeField] private GameObject _descriptionBackgroundStart;    // 변경 백그라운드 (라인 테두리)


    [SerializeField] private GameObject _descriptionText;   // 촬영을 시작하겠습니다. 화면 위쪽에 어쩌구저쩌구
    [SerializeField] private GameObject _photoPlayer;       // 이거 캐릭터 이름 모르겠음 = 플레이어로 통일


    private void Awake()
    {
        // 사진 촬영 모드 진입 버튼 리스너 등록
        if (_selectPhotoButton != null)
        {
            _selectPhotoButton.onClick.AddListener(OnSelectPhotoButtonClicked);
        }
        else
        {
            Debug.LogWarning("_selectPhotoButton reference is missing");
        }

        // 실제 촬영 시작 버튼 리스너 등록
        if (_photoButton != null)
        {
            _photoButton.onClick.AddListener(OnPhotoButtonClicked);
        }
        else
        {
            Debug.LogWarning("_photoButton reference is missing");
        }
    }

    private void OnDestroy()
    {
        // 촬영 버튼 리스너 해제
        if (_photoButton != null)
        {
            _photoButton.onClick.RemoveListener(OnPhotoButtonClicked);
        }
        else
        {
            Debug.LogWarning("_photoButton reference is missing on OnDestroy");
        }
    }

    /// <summary>
    /// 프레임 선택 & 사진 촬영 프레임 중
    /// - "사진 촬영" 버튼 선택 시 호출
    /// - 상태를 Filming 으로 변경하고 페이드 애니메이션 실행
    /// </summary>
    public void OnSelectPhotoButtonClicked()
    {
        // 상태를 촬영 모드로 전환
        GameManager.Instance.SetState(KioskState.Quantity);

        // 촬영 시작 버튼 사운드 재생
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._filmingStartButton);

        // 화면 전환용 페이드 애니메이션 실행
        _fadeAnimationCtrl.StartFade();
    }

    /// <summary>
    /// 페이드 애니메이션이 끝난 뒤 호출되는 패널 변경 함수
    /// - 현재 패널 비활성화
    /// - 촬영용 패널 활성화
    /// </summary>
    public void PanelChanger()
    {
        _currentPanel.SetActive(false);
        _changedPhotoPanel.SetActive(true);
    }
    // 
    /// <summary>
    /// 카메라 윈도우에서 "사진 찍기" 버튼 클릭 시 호출
    /// - 버튼 색상 및 텍스트 변경
    /// - 일부 안내 UI 숨김
    /// - StepCountdownUI 시퀀스 시작
    /// </summary>
    public void OnPhotoButtonClicked()
    {
        _fadeAnimationCtrl.StartFade();
        // 촬영 버튼 사운드 재생
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._filmingButton);
    }
    public void FadeEndCallBack()
    {
        if (_photoButton != null)
        {
            GameManager.Instance.SetState(KioskState.Filming);
            // 촬영 후 선택 화면으로 가는 버튼/동작 비활성화
            // _filmingToSelectCtrl.ButtonInActive();

            // 실제 버튼 대신 페이크 버튼을 활성화해 재입력 방지/연출
            _photoButtonFake.SetActive(true);

            // 촬영 단계 안내/카메라 포커스 등 숨기기
            // _stepsObject.SetActive(false);
            // _cameraFocus.SetActive(false);

            FilmingStart();

            // 버튼 컬러를 촬영 중 상태 색상으로 변경
            var cb = _photoButton.colors;
            cb.selectedColor = _activeColor;
            cb.normalColor = _activeColor;
            cb.highlightedColor = _activeColor;
            cb.pressedColor = _activeColor;
            _photoButton.colors = cb;

            // 촬영 시퀀스 시작 요청
            if (_stepCountdownUI != null)
            {
                _stepCountdownUI.StartSequence();
            }
            else
            {
                Debug.LogWarning("_stepCountdownUI reference is missing in OnPhotoButtonClicked");
            }
        }
        else
        {
            Debug.LogWarning("_photoButton reference is missing in OnPhotoButtonClicked");
        }

        // // 버튼 텍스트/설명 UI 업데이트
        // if (_buttonText != null)
        // {
        //     // 손가락 가이드 숨기기
        //     _descriptionFingerObject.SetActive(false);

        //     // 텍스트 색상 및 내용 변경
        //     _buttonText.color = _textColor;
        //     _buttonText.text = "촬영중";
        // }
        // else
        // {
        //     Debug.LogWarning("_buttonText reference is missing in OnPhotoButtonClicked");
        // }

        //Debug.Log("OnClick Filming Button");        
    }
    /// <summary>
    /// [추가]
    /// FilmingStart에서 변경된것 초기화를 할 함수로 사용될 예정
    /// 외부 호출용?
    /// </summary>
    public void FilmingEnd()
    {

    }
    /// <summary>
    /// [추가]
    /// 버튼을 눌렀을때 호출될것 (타이틀 변경, 백그라운드 변경, 캐릭터 숨기기)
    /// </summary>
    private void FilmingStart()
    {
        // (타이틀)
        _titleGameobjectEnd.SetActive(false);   // 기본 타이틀 비활성화
        _titleGameobjectStart.SetActive(true);  // 변경 타이틀 활성화

        // (백그라운드)
        _descriptionBackgroundEnd.SetActive(false);     // 기본 백그라운드 비활성화
        _descriptionBackgroundStart.SetActive(true);    // 변경 백그라운드 활성화

        // 설명 텍스트
        _descriptionText.SetActive(false);
        // (캐릭터)
        _photoPlayer.SetActive(false);  // 캐릭터 숨기기
    }
}
