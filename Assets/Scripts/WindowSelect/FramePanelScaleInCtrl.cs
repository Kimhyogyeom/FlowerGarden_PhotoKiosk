using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임 스케일 인/아웃 컨트롤러
/// - 버튼 클릭 시 Panel 활성화 + 자식 UI 스케일 0 → 1
/// - 옵션 버튼(5개) 중 하나 선택 시 선택 인덱스(0~4) 저장 + 스케일 1 → 0 후 패널 비활성화
/// </summary>
public class FramePanelScaleInCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private PhotoFrameSelectCtrl _photoFrameSelectCtrl;
    [SerializeField] private RectTransform[] _children;

    [Header("Setting Object")]
    [SerializeField] private Button _triggerButton;
    [SerializeField] private GameObject _panelFrameCurrent;
    [SerializeField] private GameObject _panelFrameChange;
    [SerializeField] private Button[] _optionButtons = new Button[5];

    [SerializeField] private Button _changeFrameApplication;

    [Header("Setting Value")]
    [SerializeField] private float _duration = 0.4f;   // 스케일 인/아웃 시간
    [SerializeField] private float _interval = 0.05f;  // 순차 등장 간격
    private int _selectedIndex = -1;
    private bool _isAnimating = false;

    [Header("Application Setting")]
    [SerializeField] private Button _applicationButton;

    private void Awake()
    {
        if (_changeFrameApplication != null)
        {
            // 프레임 적용하기
            _changeFrameApplication.onClick.AddListener(OnFrameApplicationClicked);            
        }
        else
        {
            Debug.LogWarning("_changeFrameApplication reference is missing");
        }

        // 열기 버튼 리스너 등록
        if (_triggerButton != null)
        {
            _triggerButton.onClick.AddListener(OnTriggerClicked);
        }
        else
        {
            Debug.LogWarning("_triggerButton reference is missing");
        }

        // 옵션 버튼(0~4) 리스너 등록
        if (_optionButtons != null && _optionButtons.Length > 0)
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                int captured = i; // 클로저 캡처
                if (_optionButtons[i] != null)
                {
                    _optionButtons[i].onClick.AddListener(() => OnOptionSelected(captured));
                }
                else
                {
                    Debug.LogWarning($"_optionButtons[{i}] reference is missing");
                }
            }
        }
        else
        {
            Debug.LogWarning("_optionButtons is empty or missing");
        }
    }

    private void OnDestroy()
    {
        if (_triggerButton != null)
        {
            _triggerButton.onClick.RemoveListener(OnTriggerClicked);
        }
        else
        {
            Debug.LogWarning("_triggerButton reference is missing on OnDestroy");
        }

        if (_optionButtons != null && _optionButtons.Length > 0)
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                int captured = i;
                if (_optionButtons[i] != null)
                    _optionButtons[i].onClick.RemoveListener(() => OnOptionSelected(captured));
            }
        }
        else
        {
            Debug.LogWarning("_optionButtons is empty or missing on OnDestroy");
        }
    }

    private void OnFrameApplicationClicked()
    {
        GameManager.Instance.SetState(KioskState.Select);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameApplicationButton);
        // 스케일 아웃 시작
        StartCoroutine(ScaleOutAndClose());
    }

    /// <summary>
    /// 패널 열기(스케일 인 시작)
    /// </summary>
    private void OnTriggerClicked()
    {
        if (_isAnimating)
        {
            return;
        }
        else
        {
            if (_panelFrameChange == null)
            {
                Debug.LogWarning("_panelFrameChange reference is missing");
                return;
            }
            GameManager.Instance.SetState(KioskState.Frame);
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameChangeButton);
            // 패널 Active
            _panelFrameCurrent.SetActive(false);
            _panelFrameChange.SetActive(true);

            // 순차 스케일 인
            StartCoroutine(ScaleInSequence());
        }
    }

    /// <summary>
    /// 옵션 선택(0~4) → 선택 저장 + 스케일 아웃 + 패널 비활성화
    /// </summary>
    private void OnOptionSelected(int index)
    {
        if (_isAnimating)
        {
            return; // 인/아웃 중엔 무시
        }
        else
        {
            GameManager.Instance.SetState(KioskState.Select);
            _selectedIndex = index;

            {
                // 추후 이미지 받으면 _selectedIndex로 선택한 것을 판단해 여기서 처리 예정
                if (-1 < _selectedIndex && _selectedIndex <= 0)
                {
                    Debug.Log($"select Number 0 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect0();
                }
                else if (0 < _selectedIndex && _selectedIndex <= 1)
                {
                    Debug.Log($"select Number 1 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect1();
                }
                else if (1 < _selectedIndex && _selectedIndex <= 2)
                {
                    Debug.Log($"select Number 2 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect2();
                }
            }
        }
    }

    /// <summary>
    /// 순차 스케일 인(0 → 1)
    /// </summary>
    private IEnumerator ScaleInSequence()
    {
        _isAnimating = true;

        for (int i = 0; i < _children.Length; i++)
        {
            StartCoroutine(ScaleUp(_children[i]));
            yield return new WaitForSeconds(_interval);
        }

        // 모든 자식이 1이 될 때까지 살짝 대기(보수적)
        yield return new WaitForSeconds(_duration);

        _applicationButton.interactable = true;
        _isAnimating = false;
    }

    /// <summary>
    /// 스케일 아웃(1 → 0) 후 패널 비활성화
    /// </summary>
    private IEnumerator ScaleOutAndClose()
    {
        _isAnimating = true;

        // 모든 자식 동시에 1 → 0 스케일 다운 시작 (대기 간격 없음)
        for (int i = 0; i < _children.Length; i++)
        {
            StartCoroutine(ScaleDown(_children[i]));
        }

        // 전체 애니메이션 시간만큼 한 번만 대기
        yield return new WaitForSeconds(_duration);

        // 패널 비활성화
        if (_panelFrameChange != null)
            _panelFrameChange.SetActive(false);
        if (_panelFrameCurrent != null)
            _panelFrameCurrent.SetActive(true);

        _applicationButton.interactable = false;

        _isAnimating = false;
    }

    /// <summary>
    /// 개별 RectTransform 스케일 업 (0 → 1)
    /// </summary>
    private IEnumerator ScaleUp(RectTransform rect)
    {
        float time = 0f;
        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;
            // 부드러운 곡선(스무스스텝) 적용
            t = t * t * (3f - 2f * t);
            rect.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    /// <summary>
    /// 개별 RectTransform 스케일 다운 (1 → 0)
    /// </summary>
    private IEnumerator ScaleDown(RectTransform rect)
    {
        float time = 0f;
        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;
            t = t * t * (3f - 2f * t);
            rect.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        rect.localScale = Vector3.zero;
    }
}
