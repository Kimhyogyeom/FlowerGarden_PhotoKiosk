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
    // 실제 포토 프레임 선택(0,1,2) 처리 담당 컨트롤러

    [SerializeField] private RectTransform[] _children;
    // 패널 안에서 스케일 인/아웃 될 자식 UI 요소들
    // 순서대로 하나씩 등장(ScaleInSequence), 동시에 사라짐(ScaleOutAndClose)

    [Header("Setting Object")]
    // // [SerializeField] private Button _triggerButton;
    // // 프레임 변경 패널을 여는 버튼 (예: "프레임 변경" 버튼)

    // [SerializeField] private GameObject _panelFrameCurrent;
    // // 현재 프레임을 보여주는 패널 (Frame 선택 전 상태)

    // [SerializeField] private GameObject _panelFrameChange;
    // // 프레임 선택 패널 (Frame 변경 시 활성화)

    [SerializeField] private Button[] _optionButtons = new Button[5];
    // 프레임 선택 옵션 버튼들 (총 5개 가정, index 0~4)
    // 각 버튼이 OnOptionSelected(index)를 호출하도록 Awake에서 리스너 등록

    [SerializeField] private Button _changeFrameApplication;
    // 선택한 프레임을 실제로 적용하는 버튼 (예: "프레임 적용" 버튼)

    [Header("Setting Value")]
    [SerializeField] private float _duration = 0.4f;   // 개별 스케일 인/아웃에 걸리는 시간
    [SerializeField] private float _interval = 0.05f;  // 자식 UI 순차 등장 간격
    public int _selectedIndex = -1;                    // 현재 선택된 옵션 인덱스 (0~4, 미선택 시 -1) : 무슨 프레임을 선택했는지 보여줄것임.
    private bool _isAnimating = false;                 // 스케일 인/아웃 중인지 여부(중복 입력 방지용)

    [Header("Application Setting")]
    [SerializeField] private Button _applicationButton;
    // 패널에서 최종 적용/확정 버튼 (스케일 인이 모두 끝난 후에만 interactable true)

    private void Awake()
    {
        // 프레임 적용 버튼 리스너 등록
        if (_changeFrameApplication != null)
        {
            // 프레임 적용하기 버튼 클릭 시
            _changeFrameApplication.onClick.AddListener(OnFrameApplicationClicked);
        }
        else
        {
            Debug.LogWarning("_changeFrameApplication reference is missing");
        }

        // 패널 열기(트리거) 버튼 리스너 등록
        // if (_triggerButton != null)
        // {
        //     _triggerButton.onClick.AddListener(OnTriggerClicked);
        // }
        // else
        // {
        //     Debug.LogWarning("_triggerButton reference is missing");
        // }

        // 옵션 버튼(0~4) 리스너 등록
        if (_optionButtons != null && _optionButtons.Length > 0)
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                int captured = i; // 람다 클로저용 인덱스 캡처
                if (_optionButtons[i] != null)
                {
                    // 각 버튼 클릭 시 선택 인덱스를 전달
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
        // // 트리거 버튼 리스너 제거
        // if (_triggerButton != null)
        // {
        //     _triggerButton.onClick.RemoveListener(OnTriggerClicked);
        // }
        // else
        // {
        //     Debug.LogWarning("_triggerButton reference is missing on OnDestroy");
        // }

        // 옵션 버튼들 리스너 제거
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
        // 프레임 적용 상태로 상태 전환
        GameManager.Instance.SetState(KioskState.Select);

        // 프레임 적용 버튼 클릭 사운드
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        // Sound

        // 스케일 아웃 시작 (UI 축소 후 패널 닫기)
        StartCoroutine(ScaleOutAndClose());
    }

    /// <summary>
    /// 패널 열기(스케일 인 시작)
    /// - 현재 프레임 패널 비활성화
    /// - 프레임 변경 패널 활성화
    /// - 자식 UI 순차 스케일 인
    /// </summary>
    private void OnTriggerClicked()
    {
        // 애니메이션 중에는 중복 입력 방지
        if (_isAnimating)
        {
            return;
        }
        else
        {
            // if (_panelFrameChange == null)
            // {
            //     Debug.LogWarning("_panelFrameChange reference is missing");
            //     return;
            // }

            // 프레임 선택 상태로 전환
            GameManager.Instance.SetState(KioskState.Select);

            // 프레임 변경 버튼 사운드
            // Sound

            // // 현재 프레임 패널 비활성화, 프레임 변경 패널 활성화
            // _panelFrameCurrent.SetActive(false);
            // _panelFrameChange.SetActive(true);

            // 자식 UI 순차 스케일 인 시작
            StartCoroutine(ScaleInSequence());
        }
    }

    /// <summary>
    /// 옵션 선택(0~4)
    /// - 선택 인덱스 저장
    /// - GameManager 상태 변경
    /// - 선택 인덱스에 따라 PhotoFrameSelectCtrl 쪽 프레임 선택 호출
    /// (현재는 0,1,2에 대해서만 처리)
    /// </summary>
    private void OnOptionSelected(int index)
    {
        // 인/아웃 애니메이션 중에는 선택 무시
        if (_isAnimating)
        {
            return;
        }
        else
        {
            // 선택 상태로 전환
            // GameManager.Instance.SetState(KioskState.Select);
            _selectedIndex = index;

            {
                // 추후 이미지 받으면 _selectedIndex로 선택한 것을 판단해 여기서 처리 예정
                // 현재는 인덱스 범위를 나눠서 0,1,2 프레임 선택 처리
                // ─────────────────────────────────────────────────────────────────── hight
                if (-1 < _selectedIndex && _selectedIndex <= 0)
                {
                    Debug.Log($"select Number 0 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect0Hight();
                }
                else if (0 < _selectedIndex && _selectedIndex <= 1)
                {
                    Debug.Log($"select Number 1 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect1Hight();
                }
                else if (1 < _selectedIndex && _selectedIndex <= 2)
                {
                    Debug.Log($"select Number 2 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect2Hight();
                }
                // ─────────────────────────────────────────────────────────────────── hight

                // ─────────────────────────────────────────────────────────────────── Width
                else if (2 < _selectedIndex && _selectedIndex <= 3)
                {
                    Debug.Log($"select Number 3 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect0Width();
                }
                else if (3 < _selectedIndex && _selectedIndex <= 4)
                {
                    Debug.Log($"select Number 4 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect1Width();
                }
                else if (4 < _selectedIndex && _selectedIndex <= 5)
                {
                    Debug.Log($"select Number 5 : {_selectedIndex}");
                    _photoFrameSelectCtrl.OnPhotoFrameSelect2Width();
                }
                // ─────────────────────────────────────────────────────────────────── Width
            }
        }
    }

    /// <summary>
    /// 순차 스케일 인(0 → 1)
    /// - children 배열에 있는 RectTransform들을 순서대로 스케일 업
    /// - 각 요소 사이에 _interval 만큼 대기
    /// - 마지막에 전체 duration만큼 추가 대기 후 Apply 버튼 활성화
    /// </summary>
    private IEnumerator ScaleInSequence()
    {
        _isAnimating = true;

        // 패널 열릴 때 자식 스케일을 0으로 강제 초기화(안전장치)
        if (_children != null)
        {
            foreach (var child in _children)
            {
                if (child != null)
                    child.localScale = Vector3.zero;
            }
        }

        // Apply 버튼은 애니메이션 끝나기 전까지 비활성화
        if (_applicationButton != null)
            _applicationButton.interactable = false;

        // 자식들을 순서대로 스케일 업 시작
        for (int i = 0; i < _children.Length; i++)
        {
            StartCoroutine(ScaleUp(_children[i]));
            yield return new WaitForSeconds(_interval);
        }

        // 모든 자식이 스케일 1에 도달하도록 보수적으로 duration 만큼 대기
        yield return new WaitForSeconds(_duration);

        // 스케일 인이 모두 끝난 후에만 적용 버튼 활성화
        if (_applicationButton != null)
            _applicationButton.interactable = true;

        _isAnimating = false;
    }

    /// <summary>
    /// 스케일 아웃(1 → 0) 후 패널 비활성화
    /// - 모든 자식 RectTransform을 동시에 스케일 다운
    /// - duration 동안 대기 후 패널 전환
    /// </summary>
    private IEnumerator ScaleOutAndClose()
    {
        _isAnimating = true;

        // 모든 자식을 동시에 스케일 다운 시작
        for (int i = 0; i < _children.Length; i++)
        {
            StartCoroutine(ScaleDown(_children[i]));
        }

        // 전체 애니메이션 시간만큼 대기
        yield return new WaitForSeconds(_duration);

        // 프레임 선택 패널 비활성화, 현재 프레임 패널 활성화
        // if (_panelFrameChange != null)
        //     _panelFrameChange.SetActive(false);
        // if (_panelFrameCurrent != null)
        //     _panelFrameCurrent.SetActive(true);

        // 적용 버튼 비활성화 (다음에 다시 열릴 때까지)
        if (_applicationButton != null)
            _applicationButton.interactable = false;

        _isAnimating = false;
    }

    /// <summary>
    /// 개별 RectTransform 스케일 업 (0 → 1)
    /// - duration 동안 스무스스텝 곡선으로 보간
    /// </summary>
    private IEnumerator ScaleUp(RectTransform rect)
    {
        if (rect == null) yield break;

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
    /// - duration 동안 스무스스텝 곡선으로 보간
    /// </summary>
    private IEnumerator ScaleDown(RectTransform rect)
    {
        if (rect == null) yield break;

        float time = 0f;
        while (time < _duration)
        {
            time += Time.deltaTime;
            float t = time / _duration;

            // 부드러운 곡선(스무스스텝) 적용
            t = t * t * (3f - 2f * t);

            rect.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        rect.localScale = Vector3.zero;
    }

    // ================== 초기화 함수 ==================

    /// <summary>
    /// 이 컨트롤러에서 돌고 있는 모든 코루틴을 멈추고,
    /// 패널/자식 스케일/선택 상태를 "닫힌 상태" 기준으로 초기화.
    /// </summary>
    public void ResetFramePanel()
    {
        // 이 컴포넌트에서 돌고 있는 모든 코루틴 정지
        StopAllCoroutines();

        _isAnimating = false;
        _selectedIndex = -1;

        // // 패널 상태: 현재 프레임 패널 ON, 프레임 변경 패널 OFF (기본 상태 가정)
        // if (_panelFrameCurrent != null)
        //     _panelFrameCurrent.SetActive(true);
        // if (_panelFrameChange != null)
        //     _panelFrameChange.SetActive(false);

        // 자식 스케일: 닫힌 상태 기준으로 0으로 맞춰두고,
        // 다음에 열릴 때 ScaleInSequence에서 0→1로 자연스럽게 등장
        if (_children != null)
        {
            foreach (var child in _children)
            {
                if (child != null)
                    child.localScale = Vector3.zero;
            }
        }

        // 적용 버튼은 기본적으로 비활성화
        if (_applicationButton != null)
            _applicationButton.interactable = false;
    }

    /// <summary>
    /// 이 컴포넌트가 비활성화될 때도
    /// 중간 상태로 남지 않게 정리하고 싶으면 사용
    /// </summary>
    private void OnDisable()
    {
        ResetFramePanel();
    }
}
