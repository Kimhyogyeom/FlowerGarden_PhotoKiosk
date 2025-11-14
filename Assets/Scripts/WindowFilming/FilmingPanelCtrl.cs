using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FilmingPanelCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private StepCountdownUI _stepCountdownUI;
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;

    [Header("Setting Object")]
    [SerializeField] private Button _selectPhotoButton;
    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _changedPhotoPanel;

    [SerializeField] private Button _photoButton;
    [SerializeField] private GameObject _photoButtonFake;
    [SerializeField] private TextMeshProUGUI _buttonText;

    [SerializeField] private GameObject _stepsObject;

    [SerializeField] private GameObject _descriptionFingerObject;

    [SerializeField] private GameObject _cameraFocus;

    [Header("Setting Color")]
    [SerializeField] private Color _activeColor = Color.red;
    [SerializeField] private Color _textColor = Color.white;

    private void Awake()
    {
        if (_selectPhotoButton != null)
        {
            _selectPhotoButton.onClick.AddListener(OnSelectPhotoButtonClicked);
        }
        else
        {
            Debug.LogWarning("_selectPhotoButton reference is missing");
        }

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
    /// 프레임 선택 & 사진 촬영 프레임 => 사진 촬영 프레임 선택시 패널 변경
    /// [애니메이션 실행 요청]
    /// </summary>
    private void OnSelectPhotoButtonClicked()
    {
        GameManager.Instance.SetState(KioskState.Filming);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._filmingStartButton);
        _fadeAnimationCtrl.StartFade();        
    }

    /// <summary>
    /// 애니메이션 끝나면 호출됨
    /// </summary>
    public void PanelChanger()
    {
        _currentPanel.SetActive(false);
        _changedPhotoPanel.SetActive(true);
    }

    /// <summary>
    /// 사진 찍기 버튼 클릭 시 호출 (카메라 윈도우에서)
    /// </summary>
    private void OnPhotoButtonClicked()
    {
        if (_photoButton != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._filmingButton);
            _filmingToSelectCtrl.ButtonInActive();
            _photoButtonFake.SetActive(true);
            _stepsObject.SetActive(false);
            _cameraFocus.SetActive(false);

            var cb = _photoButton.colors;
            cb.selectedColor = _activeColor;
            cb.normalColor = _activeColor;
            cb.highlightedColor = _activeColor;
            cb.pressedColor = _activeColor;
            _photoButton.colors = cb;

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

        if (_buttonText != null)
        {
            _descriptionFingerObject.SetActive(false);
            _buttonText.color = _textColor;
            _buttonText.text = "촬영중";
        }
        else
        {
            Debug.LogWarning("_buttonText reference is missing in OnPhotoButtonClicked");
        }

        //Debug.Log("OnClick Filming Button");
    }
}
