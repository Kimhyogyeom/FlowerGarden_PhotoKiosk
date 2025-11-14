using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PaymentCtrl : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _paymentPanel;
    [SerializeField] private GameObject _readyPanel;
    [SerializeField] private TextMeshProUGUI _textMeshPro;
    [SerializeField] private GameObject _loadingImage;

    [Header("Mock Settings")]
    [SerializeField] private bool _useMock = true;          // 나중에 false 로 바꾸면 실제 결제 모드
    [SerializeField] private float _mockApproveDelay = 5f;
    [SerializeField] private bool _alwaysSuccess = true;

    [Header("Loading Settings")]
    [Tooltip("로딩 아이콘 회전 속도 (도/초, 오른쪽(시계 방향) 회전)")]
    [SerializeField] private float _loadingRotateSpeed = 360f;

    private bool _isProcessing = false;
    private Coroutine _loadingCoroutine;

    private void OnEnable()
    {
        // 구독
        PaymentPanelEnableBroadcaster.OnPaymentPanelEnabled += TryStartPayment;

        // 이미 PaymentPanel이 켜져 있는 경우 대비
        if (_paymentPanel != null && _paymentPanel.activeInHierarchy)
        {
            TryStartPayment();
        }
    }

    private void OnDisable()
    {
        PaymentPanelEnableBroadcaster.OnPaymentPanelEnabled -= TryStartPayment;
        StopLoading(); // 혹시 꺼질 때 돌고 있으면 정리
    }

    private void TryStartPayment()
    {
        // PaymentCtrl가 비활성 상태면 무시 (이벤트가 남아있을 수도 있어서)
        if (!isActiveAndEnabled)
            return;

        if (_isProcessing)
        {
            Debug.Log("[PAY] Already processing...");
            return;
        }

        _isProcessing = true;
        StartLoading();

        if (_useMock)
        {
            Debug.Log("[PAY-MOCK] 모의 결제 시작");
            StartCoroutine(MockPaymentRoutine());
            // 나중에 Mock 코루틴 대신 실제 SDK 호출 넣고,
            // 성공/실패에서 OnPaymentApproved / OnPaymentFailed 호출.
        }
        else
        {
            Debug.Log("[PAY-REAL] 실제 결제 요청 시작");
            StartRealPayment();   // 여기에 나중에 SDK 호출만 채우면 됨
        }
    }

    // ---------- 로딩 코루틴 ----------

    private void StartLoading()
    {
        if (_loadingImage == null)
            return;

        // 초기 상태 세팅
        _loadingImage.SetActive(true);
        _loadingImage.transform.localRotation = Quaternion.identity;

        if (_loadingCoroutine != null)
            StopCoroutine(_loadingCoroutine);

        _loadingCoroutine = StartCoroutine(LoadingCoroutine());
    }

    private void StopLoading()
    {
        if (_loadingImage == null)
            return;

        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;
        }

        _loadingImage.transform.localRotation = Quaternion.identity;
        _loadingImage.SetActive(false);
    }

    private IEnumerator LoadingCoroutine()
    {
        if (_loadingImage == null)
            yield break;

        var rect = _loadingImage.transform as RectTransform;
        float angle = 0f;

        // _isProcessing 동안 회전
        while (_isProcessing)
        {
            float dt = Time.deltaTime;
            // 오른쪽(시계 방향) 회전 → Z각도를 감소
            angle -= _loadingRotateSpeed * dt;

            // 각도 누적 값 관리 (선택사항)
            if (angle <= -360f)
                angle += 360f;

            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        // 여기까지 오면 결제 완료/실패로 종료된 상태
    }

    // ---------- MOCK 결제 ----------

    private IEnumerator MockPaymentRoutine()
    {
        // 결제 대기 연출
        if (_textMeshPro != null)
            _textMeshPro.text = "결제 진행중 ...";

        yield return new WaitForSeconds(_mockApproveDelay);

        if (_alwaysSuccess)
        {
            if (_textMeshPro != null)
                _textMeshPro.text = "결제 완료";

            StopLoading();  // OnPaymentApproved() 에서도 호출 되지만 임시 테스트용으로 진행

            yield return new WaitForSeconds(2f);

            OnPaymentApproved();
        }
        else
        {
            OnPaymentFailed("MOCK: 결제 실패 (테스트)");
        }
    }

    // ---------- 실제 결제 (나중에 구현) ----------

    private void StartRealPayment()
    {
        // 예시 패턴 1: SDK가 콜백 형태인 경우 (가장 이상적)

        // JtnetSdk.RequestPayment(
        //      amount: 4000,
        //      orderId: "ORDER_001",
        //      onSuccess: () => { OnPaymentApproved(); },
        //      onFail: (err) => { OnPaymentFailed(err); }
        // );
        // aount : 결제금액
        // orderId : 주문번호, 트랜잭션 ID
        // onSuccess : 결제가 성공하면 콜백
        // onFail : 실패 시 부를 함수

        // 예시 패턴 2: EXE 실행 후 결과 파일/코드 읽는 방식이면,
        // 별도 코루틴/쓰레드에서 감시하다가 최종적으로
        // OnPaymentApproved() 또는 OnPaymentFailed(reason)만 호출해주면 됨.

        // 중요한 건:
        // "성공" 시 → 반드시 OnPaymentApproved()
        // "실패" 시 → 반드시 OnPaymentFailed(..)
        // 이 두 개만 지켜주면, 나머지 플로우(Ready 패널, 촬영, 인쇄)는 그대로 동작함.

        if (_textMeshPro != null)
            _textMeshPro.text = "결제 진행중 ...";
    }

    // ---------- 공통 콜백 ----------

    private void OnPaymentApproved()
    {
        _isProcessing = false;
        StopLoading();

        Debug.Log("[PAY] 승인 완료");

        GameManager.Instance.SetState(KioskState.Ready);

        if (_paymentPanel != null) _paymentPanel.SetActive(false);
        if (_readyPanel != null) _readyPanel.SetActive(true);
    }

    private void OnPaymentFailed(string reason)
    {
        _isProcessing = false;
        StopLoading();

        Debug.LogWarning("[PAY] 결제 실패: " + reason);

        GameManager.Instance.SetState(KioskState.WaitingForPayment);

        if (_textMeshPro != null)
            _textMeshPro.text = "결제 실패\n" + reason;

        // 실패 시: 패널 유지, 문구만 변경. 재시도 버튼 등은 여기서 추가 가능.
    }
}