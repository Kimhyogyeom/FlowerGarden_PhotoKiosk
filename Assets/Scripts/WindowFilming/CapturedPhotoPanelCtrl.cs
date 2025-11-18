using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// - 카메라 윈도우의 "다음" 버튼 클릭 시:
///   - 현재 패널 닫고, 다음 패널 열기
///   - StepCountdownUI가 찍어둔 사진을 8개 버튼의 Image에 1:1 매핑
/// - 각 버튼을 클릭하면:
///   - 버튼 하위의 선택 마커(SelectedMark) ON/OFF
///   - 선택된 사진을 메인 4칸에 "고정 슬롯 방식"으로 배치
///   - 선택 개수 텍스트: 0/4, 1/4 ... 업데이트
/// </summary>
public class CapturedPhotoPanelCtrl : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("현재 열려있는 패널(닫힐 패널)")]
    [SerializeField] private GameObject _currentPanel;

    [Tooltip("새로 열릴 패널(8개 버튼이 있는 패널)")]
    [SerializeField] private GameObject _nextPanel;

    [Header("Reference")]
    [Tooltip("촬영 & 캡처를 담당하는 StepCountdownUI")]
    [SerializeField] private StepCountdownUI _stepCountdownUI;

    [Header("Target Buttons")]
    [Tooltip("새 패널에 있는 사진 선택 버튼들 (버튼의 Image에 사진 매핑)")]
    [SerializeField] private Button[] _photoButtons; // 8개 예상

    [Header("Selection Marker Settings")]
    [Tooltip("버튼 하위에서 선택 마커로 사용할 자식 오브젝트 이름")]
    [SerializeField] private string _selectionChildName = "SelectedMark";

    [Header("Main Preview Images")]
    [Tooltip("최종 선택된 사진을 보여줄 4개 이미지 슬롯 (항상 보이고, sprite만 바뀜)")]
    [SerializeField] private Image[] _mainImages = new Image[4];

    [Header("Selection Settings")]
    [Tooltip("최대 선택 가능한 개수 (보통 4)")]
    [SerializeField] private int _maxSelection = 4;

    [Tooltip("캡처가 안 된 슬롯은 버튼을 비활성화할지 여부")]
    [SerializeField] private bool _disableButtonIfNoSprite = true;

    [Header("UI Text")]
    [Tooltip("현재 선택 개수 표시 텍스트 (예: 0/4, 1/4 ...)")]
    [SerializeField] private TextMeshProUGUI _selectedCountText;

    [Header("Camera Window")]
    [Tooltip("카메라윈도우 패널의 '다음' 버튼")]
    [SerializeField] private Button _cameraWindowNextButton;

    // === 내부 상태 ===
    private bool[] _isSelected;            // 각 버튼 선택 여부
    private GameObject[] _selectionMarkers; // 버튼 하위에서 찾은 선택 마커
    private int _currentSelectedCount = 0;

    // 슬롯별로 어떤 버튼이 들어있나? (-1 == 비어있음)
    private int[] _slotOwners;            // 길이 = _mainImages.Length

    // 각 버튼이 어느 슬롯을 쓰고 있나? (-1 == 아직 안 들어감)
    private int[] _buttonAssignedSlot;    // 길이 = _photoButtons.Length

    private void Awake()
    {
        // 최대 선택 수는 메인 슬롯 개수 이상이 될 수 없음
        if (_mainImages != null && _mainImages.Length > 0)
        {
            _maxSelection = Mathf.Min(_maxSelection, _mainImages.Length);
        }

        EnsureArrays();
        FindSelectionMarkersFromButtons();

        // "다음" 버튼 → 사진 매핑 + 패널 전환
        if (_cameraWindowNextButton != null)
        {
            _cameraWindowNextButton.onClick.AddListener(OpenNextPanelAndApplyPhotos);
        }

        // 각 사진 버튼 클릭 → 선택/취소 토글
        if (_photoButtons != null)
        {
            for (int i = 0; i < _photoButtons.Length; i++)
            {
                int idx = i; // 클로저 방지
                if (_photoButtons[i] != null)
                {
                    _photoButtons[i].onClick.AddListener(() => OnPhotoButtonClicked(idx));
                }
            }
        }

        // 시작 상태 초기화
        ResetSelectionOnly();
        UpdateSelectionCountText();
        UpdateMainImages();
    }

    /// <summary>
    /// "다음" 버튼에서 호출.
    /// 사진 매핑 + 패널 전환.
    /// </summary>
    public void OpenNextPanelAndApplyPhotos()
    {
        if (_stepCountdownUI == null)
        {
            Debug.LogWarning("[CapturedPhotoPanelCtrl] StepCountdownUI reference is missing");
            return;
        }

        if (_photoButtons == null || _photoButtons.Length == 0)
        {
            Debug.LogWarning("[CapturedPhotoPanelCtrl] _photoButtons is empty");
            return;
        }

        // 새 패널 진입 시 선택 상태/표시 초기화
        ResetSelectionOnly();
        UpdateSelectionCountText();
        UpdateMainImages();

        // StepCountdownUI에서 캡처된 스프라이트를 버튼들에 매핑
        for (int i = 0; i < _photoButtons.Length; i++)
        {
            Button btn = _photoButtons[i];
            if (btn == null) continue;

            Image targetImg = btn.image;
            if (targetImg == null) continue;

            Sprite captured = _stepCountdownUI.GetCapturedSprite(i);

            if (captured == null)
            {
                targetImg.sprite = null;

                var c = targetImg.color;
                c.a = 0.3f; // 흐리게
                targetImg.color = c;

                if (_disableButtonIfNoSprite)
                    btn.interactable = false;
            }
            else
            {
                targetImg.sprite = captured;
                targetImg.preserveAspect = true;

                var c = targetImg.color;
                c.a = 1f;
                targetImg.color = c;

                btn.interactable = true;
            }

            // 선택 마커 OFF
            if (_selectionMarkers != null &&
                i < _selectionMarkers.Length &&
                _selectionMarkers[i] != null)
            {
                _selectionMarkers[i].SetActive(false);
            }
        }

        // 패널 전환
        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        if (_nextPanel != null)
            _nextPanel.SetActive(true);
    }

    /// <summary>
    /// 개별 사진 버튼 클릭 시 호출 (Awake에서 리스너 연결됨)
    /// </summary>
    private void OnPhotoButtonClicked(int index)
    {
        if (_stepCountdownUI == null) return;

        Sprite captured = _stepCountdownUI.GetCapturedSprite(index);
        if (captured == null)
        {
            Debug.LogWarning($"[CapturedPhotoPanelCtrl] No sprite at index {index}");
            return;
        }

        EnsureArrays();

        // 아직 선택 안 된 상태 → 선택 시도
        if (!_isSelected[index])
        {
            // 이미 4/4면 선택 불가, 로그만
            if (_currentSelectedCount >= _maxSelection)
            {
                Debug.Log("[CapturedPhotoPanelCtrl] 이미 최대 선택 개수(4)에 도달했습니다.");
                return;
            }

            // 비어 있는 슬롯(0→3 순서)에서 첫 슬롯 찾기
            int freeSlot = -1;
            for (int s = 0; s < _slotOwners.Length; s++)
            {
                if (_slotOwners[s] == -1)
                {
                    freeSlot = s;
                    break;
                }
            }

            if (freeSlot == -1)
            {
                // 이 경우는 거의 없겠지만 방어 코드
                Debug.LogWarning("[CapturedPhotoPanelCtrl] 빈 슬롯이 없습니다.");
                return;
            }

            // 상태 반영
            _isSelected[index] = true;
            _slotOwners[freeSlot] = index;
            _buttonAssignedSlot[index] = freeSlot;
            _currentSelectedCount++;

            // 하위 선택 마커 ON
            if (_selectionMarkers != null &&
                index < _selectionMarkers.Length &&
                _selectionMarkers[index] != null)
            {
                _selectionMarkers[index].SetActive(true);
            }
        }
        // 이미 선택된 상태 → 선택 해제
        else
        {
            _isSelected[index] = false;
            _currentSelectedCount = Mathf.Max(0, _currentSelectedCount - 1);

            // 이 버튼이 쓰고 있는 슬롯 찾아서 비우기
            int slot = _buttonAssignedSlot[index];
            if (slot >= 0 && slot < _slotOwners.Length)
            {
                _slotOwners[slot] = -1;
            }
            _buttonAssignedSlot[index] = -1;

            // 하위 선택 마커 OFF
            if (_selectionMarkers != null &&
                index < _selectionMarkers.Length &&
                _selectionMarkers[index] != null)
            {
                _selectionMarkers[index].SetActive(false);
            }
        }

        // 메인 이미지 + 텍스트 갱신
        UpdateMainImages();
        UpdateSelectionCountText();
    }

    // ================== Helpers ==================

    /// <summary>
    /// 내부 배열들(_isSelected, _slotOwners, _buttonAssignedSlot) 크기 맞추기
    /// </summary>
    private void EnsureArrays()
    {
        // 버튼 관련 배열
        if (_photoButtons != null)
        {
            int len = _photoButtons.Length;

            if (_isSelected == null || _isSelected.Length != len)
                _isSelected = new bool[len];

            if (_buttonAssignedSlot == null || _buttonAssignedSlot.Length != len)
            {
                _buttonAssignedSlot = new int[len];
                for (int i = 0; i < len; i++)
                    _buttonAssignedSlot[i] = -1;
            }
        }

        // 슬롯 오너
        if (_mainImages != null)
        {
            int slotLen = _mainImages.Length;

            if (_slotOwners == null || _slotOwners.Length != slotLen)
            {
                _slotOwners = new int[slotLen];
                for (int i = 0; i < slotLen; i++)
                    _slotOwners[i] = -1;
            }
        }
    }

    /// <summary>
    /// 각 버튼 하위에서 선택 마커(SelectedMark)를 자동으로 찾아서 저장
    /// </summary>
    private void FindSelectionMarkersFromButtons()
    {
        if (_photoButtons == null || _photoButtons.Length == 0)
        {
            _selectionMarkers = null;
            return;
        }

        _selectionMarkers = new GameObject[_photoButtons.Length];

        for (int i = 0; i < _photoButtons.Length; i++)
        {
            var btn = _photoButtons[i];
            if (btn == null)
                continue;

            Transform child = btn.transform.Find(_selectionChildName);
            if (child != null)
            {
                _selectionMarkers[i] = child.gameObject;
                _selectionMarkers[i].SetActive(false); // 기본 꺼둠
            }
            else
            {
                Debug.LogWarning($"[CapturedPhotoPanelCtrl] 버튼 {btn.name} 하위에 '{_selectionChildName}' 오브젝트를 찾지 못했습니다.");
            }
        }
    }

    /// <summary>
    /// 선택 상태/마커/슬롯 초기화 (메인 슬롯은 "자리"만 유지, sprite만 비움)
    /// </summary>
    private void ResetSelectionOnly()
    {
        EnsureArrays();

        _currentSelectedCount = 0;

        // 버튼 선택 정보 리셋
        if (_isSelected != null)
        {
            for (int i = 0; i < _isSelected.Length; i++)
                _isSelected[i] = false;
        }

        if (_buttonAssignedSlot != null)
        {
            for (int i = 0; i < _buttonAssignedSlot.Length; i++)
                _buttonAssignedSlot[i] = -1;
        }

        // 슬롯 오너 초기화
        if (_slotOwners != null)
        {
            for (int i = 0; i < _slotOwners.Length; i++)
                _slotOwners[i] = -1;
        }

        // 선택 마커 OFF
        if (_selectionMarkers != null)
        {
            for (int i = 0; i < _selectionMarkers.Length; i++)
            {
                if (_selectionMarkers[i] != null)
                    _selectionMarkers[i].SetActive(false);
            }
        }

        // 메인 4칸: "자리"는 보이되, sprite만 비움
        if (_mainImages != null)
        {
            for (int i = 0; i < _mainImages.Length; i++)
            {
                if (_mainImages[i] != null)
                {
                    _mainImages[i].sprite = null;
                    // color는 그대로 둠 (테두리/배경 등 유지)
                }
            }
        }
    }

    /// <summary>
    /// 슬롯 오너 정보(_slotOwners)에 따라 메인 4칸 sprite 갱신
    /// </summary>
    private void UpdateMainImages()
    {
        if (_mainImages == null || _mainImages.Length == 0) return;
        if (_stepCountdownUI == null) return;

        for (int slot = 0; slot < _mainImages.Length; slot++)
        {
            Image img = _mainImages[slot];
            if (img == null) continue;

            int ownerButtonIndex = (_slotOwners != null && slot < _slotOwners.Length)
                ? _slotOwners[slot]
                : -1;

            if (ownerButtonIndex == -1)
            {
                // 빈 슬롯 → sprite만 비움 (프레임은 그대로)
                img.sprite = null;
            }
            else
            {
                Sprite s = _stepCountdownUI.GetCapturedSprite(ownerButtonIndex);
                img.sprite = s;
                img.preserveAspect = true;
            }
        }
    }

    /// <summary>
    /// 선택 개수 텍스트 갱신 (예: 1/4)
    /// </summary>
    private void UpdateSelectionCountText()
    {
        if (_selectedCountText == null) return;

        _selectedCountText.text = $"{_currentSelectedCount} / {_maxSelection}";
    }
}
