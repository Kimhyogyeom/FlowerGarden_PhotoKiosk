using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 스티커 패널 컨트롤러
/// - 모드에 따라 세로/가로 오브젝트를 가져와서 목표 패널에 표시
/// - 세로 모드(Hight) → 세로용 오브젝트 가져오기
/// - 가로 모드(Width) → 가로용 오브젝트 가져오기
/// - 프린트 후 원래 위치로 되돌림
/// </summary>
public class StickerPanelCtrl : MonoBehaviour
{
    [Header("Hight Mode (세로 모드)")]
    [Tooltip("세로 모드에서 가져올 오브젝트")]
    [SerializeField] private GameObject _sourceObjectHight;

    [Tooltip("세로 모드에서 원래 위치로 돌아갈 부모")]
    [SerializeField] private Transform _originalParentHight;

    [Header("Width Mode (가로 모드)")]
    [Tooltip("가로 모드에서 가져올 오브젝트")]
    [SerializeField] private GameObject _sourceObjectWidth;

    [Tooltip("가로 모드에서 원래 위치로 돌아갈 부모")]
    [SerializeField] private Transform _originalParentWidth;

    [Header("Target Panel")]
    [Tooltip("오브젝트를 가져올 목표 패널")]
    [SerializeField] private Transform _targetPanel;

    [Header("Position & Scale Settings")]
    [Tooltip("가져왔을 때 적용할 로컬 좌표")]
    [SerializeField] private Vector3 _targetLocalPosition = Vector3.zero;

    [Tooltip("가져왔을 때 적용할 스케일")]
    [SerializeField] private Vector3 _targetScale = new Vector3(2f, 2f, 2f);

    [Header("Timer Display")]
    [Tooltip("카운트다운 시간 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI _countdownText;

    [SerializeField] private GameObject _panelSticker;

    // 내부 상태
    private GameObject _currentFrame;           // 현재 가져온 오브젝트
    private Transform _currentOriginalParent;   // 현재 오브젝트의 원래 부모
    private Vector3 _originalLocalPosition;     // 원래 로컬 좌표
    private Vector3 _originalLocalScale;        // 원래 스케일
    private int _originalSiblingIndex;          // 원래 Hierarchy 순서
    private bool _isHightMode;                  // 현재 모드 저장

    // 타이머 관련
    private Coroutine _countdownRoutine;
    private int _timerValue;

    private void Awake()
    {
        Debug.Log("[StickerPanelCtrl] ===== Awake 호출됨 =====");
    }

    private void Start()
    {
        Debug.Log("[StickerPanelCtrl] ===== Start 호출됨 =====");
    }

    private void OnEnable()
    {
        Debug.Log("[StickerPanelCtrl] ===== OnEnable 호출됨 =====");

        // OutputEnableBroadcaster 이벤트 구독
        OutputEnableBroadcaster.OnOutputEnabled += OnStickerPanelEnabled;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        OutputEnableBroadcaster.OnOutputEnabled -= OnStickerPanelEnabled;

        // 타이머 정리
        StopCountdown();
    }

    /// <summary>
    /// 프레임을 목표 패널로 가져오기
    /// 모드에 따라 세로/가로 오브젝트 선택
    /// </summary>
    public void LoadFrame()
    {
        Debug.Log("[StickerPanelCtrl] ===== LoadFrame 시작 =====");

        // 현재 모드 확인
        _isHightMode = GameManager.Instance != null && GameManager.Instance.CurrentMode == KioskMode.Hight;
        Debug.Log($"[StickerPanelCtrl] 현재 모드: {(_isHightMode ? "Hight (세로)" : "Width (가로)")}");

        // 모드에 따라 오브젝트와 원래 부모 선택
        GameObject sourceObject = _isHightMode ? _sourceObjectHight : _sourceObjectWidth;
        _currentOriginalParent = _isHightMode ? _originalParentHight : _originalParentWidth;

        if (sourceObject == null)
        {
            Debug.LogWarning($"[StickerPanelCtrl] {(_isHightMode ? "세로" : "가로")} 모드용 소스 오브젝트가 설정되지 않았습니다.");
            return;
        }

        if (_currentOriginalParent == null)
        {
            Debug.LogWarning($"[StickerPanelCtrl] {(_isHightMode ? "세로" : "가로")} 모드용 원래 부모가 설정되지 않았습니다.");
        }

        // 목표 패널 확인 (없으면 자기 자신 사용)
        Transform targetParent = _targetPanel != null ? _targetPanel : transform;

        _currentFrame = sourceObject;

        // 원래 위치/스케일 저장
        _originalLocalPosition = _currentFrame.transform.localPosition;
        _originalLocalScale = _currentFrame.transform.localScale;
        _originalSiblingIndex = _currentFrame.transform.GetSiblingIndex();
        Debug.Log($"[StickerPanelCtrl] 원래 위치 저장 - 부모: {(_currentOriginalParent != null ? _currentOriginalParent.name : "NULL")}, 좌표: {_originalLocalPosition}, 스케일: {_originalLocalScale}, 순서: {_originalSiblingIndex}");

        // 목표 패널의 자식으로 이동
        _currentFrame.transform.SetParent(targetParent, false);

        // 지정된 좌표와 스케일 적용
        _currentFrame.transform.localPosition = _targetLocalPosition;
        _currentFrame.transform.localScale = _targetScale;

        Debug.Log($"[StickerPanelCtrl] ✅ {_currentFrame.name}를 {targetParent.name} 자식으로 이동 완료, 좌표: {_targetLocalPosition}, 스케일: {_targetScale}");
    }

    /// <summary>
    /// 프레임을 원래 위치로 되돌림 (프린트 완료 후 호출)
    /// 리셋 함수// 호출 될꺼임 리셋
    /// </summary>
    public void RestoreFrame()
    {
        if (_currentFrame == null)
        {
            Debug.LogWarning("[StickerPanelCtrl] 복원할 프레임이 없습니다.");
            return;
        }

        // 원래 부모로 되돌림
        if (_currentOriginalParent != null)
        {
            _currentFrame.transform.SetParent(_currentOriginalParent, false);
            _currentFrame.transform.localPosition = _originalLocalPosition;
            _currentFrame.transform.localScale = _originalLocalScale;
            _currentFrame.transform.SetSiblingIndex(_originalSiblingIndex);

            Debug.Log($"[StickerPanelCtrl] {_currentFrame.name}를 원래 위치로 복원 - 부모: {_currentOriginalParent.name}, 좌표: {_originalLocalPosition}, 스케일: {_originalLocalScale}");
        }
        else
        {
            Debug.LogWarning("[StickerPanelCtrl] 원래 부모가 설정되지 않아 복원할 수 없습니다.");
        }

        _currentFrame = null;
        _currentOriginalParent = null;

        if (_panelSticker != null)
        {
            _panelSticker.SetActive(false);
        }
    }

    // ==================  타이머 관련 ==================

    /// <summary>
    /// OutputEnableBroadcaster 이벤트 수신 시 호출
    /// 스티커 패널이 활성화되면 자동 타이머 시작
    /// </summary>
    private void OnStickerPanelEnabled()
    {
        StartCountdown();
    }

    /// <summary>
    /// 카운트다운 시작
    /// </summary>
    private void StartCountdown()
    {
        // 이미 돌고 있으면 먼저 정지
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }

        _countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// 카운트다운 정지
    /// </summary>
    private void StopCountdown()
    {
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }

        // 텍스트 초기화
        if (_countdownText != null)
        {
            _countdownText.text = string.Empty;
        }
    }

    /// <summary>
    /// 카운트다운 코루틴
    /// </summary>
    private IEnumerator CountdownRoutine()
    {
        // GameManager에서 타이머 값 가져오기
        if (GameManager.Instance != null)
        {
            _timerValue = GameManager.Instance._photoSelectToPrintTimer;
        }
        else
        {
            _timerValue = 10; // 기본값
        }

        Debug.Log($"[StickerPanelCtrl] 카운트다운 시작: {_timerValue}초");

        while (_timerValue > 0)
        {
            // 텍스트 업데이트
            if (_countdownText != null)
            {
                _countdownText.text = _timerValue.ToString();
            }

            yield return new WaitForSeconds(1f);
            _timerValue--;
        }

        // 마지막 0 표시
        if (_countdownText != null)
        {
            _countdownText.text = "0";
        }

        Debug.Log("[StickerPanelCtrl] 카운트다운 종료 - 자동으로 프린트 진행하지 않음 (수동 버튼 클릭 대기)");

        // 텍스트 지우기
        if (_countdownText != null)
        {
            _countdownText.text = string.Empty;
        }

        _countdownRoutine = null;

        // 참고: 여기서는 자동 프린트를 하지 않습니다.
        // 사용자가 직접 프린트 버튼을 눌러야 합니다.
    }
}
