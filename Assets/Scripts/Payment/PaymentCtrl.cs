// using System;
// using System.Collections;
// using TMPro;
// using UnityEngine;

// /// <summary>
// /// 결제 처리 컨트롤러
// /// - 결제 패널이 켜졌을 때 자동으로 결제를 시작 (Mock / 실제 결제 분기)
// /// - 로딩 UI 회전, 결제 성공/실패 처리, 패널 전환까지 담당
// /// </summary>
// public class PaymentCtrl : MonoBehaviour
// {
//     [Header("Component")]
//     [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;  // 페이드 애니메이션

//     [Header("Panels")]
//     [SerializeField] private GameObject _paymentPanel;      // 결제용 패널
//     [SerializeField] private GameObject _readyPanel;        // 결제 완료 후 돌아갈 대기(Ready) 패널
//     [SerializeField] private TextMeshProUGUI _textMeshPro;  // 결제 상태 메시지 출력용 텍스트
//     [SerializeField] private GameObject _loadingImage;      // 로딩(스피너) 이미지 오브젝트

//     [Header("Mock Settings")]
//     [SerializeField] private bool _useMock = true;          // 실제 결제 연동 전까지 사용할 모의 결제 플래그 (true = 모의 결제 모드)
//     [SerializeField] private float _mockApproveDelay = 5f;  // 모의 결제 승인까지 기다릴 시간(초)
//     [SerializeField] private bool _alwaysSuccess = true;    // Mock 모드에서 항상 성공 처리할지 여부

//     [Header("Loading Settings")]
//     [Tooltip("로딩 이미지가 회전하는 속도 (도/초, 시계 방향 회전)")]
//     [SerializeField] private float _loadingRotateSpeed = 360f;

//     private bool _isProcessing = false;        // 현재 결제 처리 중인지 여부
//     private Coroutine _loadingCoroutine;       // 로딩 회전 코루틴 핸들

//     // private void OnEnable()
//     // {
//     //     // 결제 패널 활성화 브로드캐스터 이벤트 구독
//     //     PaymentPanelEnableBroadcaster.OnPaymentPanelEnabled += TryStartPayment;

//     //     // 이미 PaymentPanel 이 활성화된 상태로 켜졌다면, 바로 결제 시도
//     //     if (_paymentPanel != null && _paymentPanel.activeInHierarchy)
//     //     {
//     //         TryStartPayment();
//     //     }
//     // }

//     // private void OnDisable()
//     // {
//     //     PaymentPanelEnableBroadcaster.OnPaymentPanelEnabled -= TryStartPayment;
//     //     // 오브젝트가 비활성화될 때 로딩 회전이 남아있지 않도록 정리
//     //     StopLoading();
//     // }

//     /// <summary>
//     /// 결제 시작을 시도하는 함수
//     /// - PaymentPanelEnableBroadcaster 이벤트 및 OnEnable 에서 호출
//     /// </summary>
//     public void TryStartPayment()
//     {
//         // PaymentCtrl 이 비활성화 상태면 결제 시작하지 않음
//         // (이벤트가 먼저 날아와도 여기서 막힘)
//         if (!isActiveAndEnabled)
//             return;

//         if (_isProcessing)
//         {
//             Debug.Log("[PAY] Already processing...");
//             return;
//         }

//         _isProcessing = true;
//         StartLoading();

//         if (_useMock)
//         {
//             Debug.Log("[PAY-MOCK] 모의 결제 시작");
//             StartCoroutine(MockPaymentRoutine());
//             // 실제 연동 시에는 여기 대신 결제 SDK 호출 후
//             // 성공/실패 콜백에서 OnPaymentApproved / OnPaymentFailed 호출.
//         }
//         else
//         {
//             Debug.Log("[PAY-REAL] 실제 결제 요청 시작");
//             StartRealPayment();   // 실제 결제 SDK 연동 위치
//         }
//     }

//     // ---------- 로딩 회전 관련 ----------

//     /// <summary>
//     /// 로딩 이미지 활성화 및 회전 코루틴 시작
//     /// </summary>
//     private void StartLoading()
//     {
//         if (_loadingImage == null)
//             return;

//         // 처음 각도 초기화 후 로딩 이미지 활성화
//         _loadingImage.SetActive(true);
//         _loadingImage.transform.localRotation = Quaternion.identity;

//         if (_loadingCoroutine != null)
//             StopCoroutine(_loadingCoroutine);

//         _loadingCoroutine = StartCoroutine(LoadingCoroutine());
//     }

//     /// <summary>
//     /// 로딩 회전 정지 및 초기 상태로 복원
//     /// </summary>
//     private void StopLoading()
//     {
//         if (_loadingImage == null)
//             return;

//         if (_loadingCoroutine != null)
//         {
//             StopCoroutine(_loadingCoroutine);
//             _loadingCoroutine = null;
//         }

//         _loadingImage.transform.localRotation = Quaternion.identity;
//         _loadingImage.SetActive(false);
//     }

//     /// <summary>
//     /// 결제 진행 중 로딩 이미지를 계속 회전시키는 코루틴
//     /// </summary>
//     private IEnumerator LoadingCoroutine()
//     {
//         if (_loadingImage == null)
//             yield break;

//         var rect = _loadingImage.transform as RectTransform;
//         float angle = 0f;

//         // _isProcessing 이 true 인 동안 회전
//         while (_isProcessing)
//         {
//             float dt = Time.deltaTime;
//             // 초당 _loadingRotateSpeed 도만큼 Z축으로 회전(시계 방향)
//             angle -= _loadingRotateSpeed * dt;

//             // 각도 값이 너무 커지지 않도록 한 바퀴 넘으면 보정
//             if (angle <= -360f)
//                 angle += 360f;

//             rect.localRotation = Quaternion.Euler(0f, 0f, angle);
//             yield return null;
//         }

//         // 결제가 끝나면 StopLoading() 에서 초기화 처리
//     }

//     // ---------- MOCK 결제 처리 ----------

//     /// <summary>
//     /// 모의 결제 플로우
//     /// - 지정 시간 대기 후 항상 성공 혹은 실패 콜백 호출
//     /// </summary>
//     private IEnumerator MockPaymentRoutine()
//     {
//         // 0초 지점: 바로 실행
//         if (_textMeshPro != null)
//             _textMeshPro.text = "결제 처리 중입니다...";

//         // ⏱ 1단계: _mockApproveDelay 만큼 대기
//         yield return new WaitForSeconds(_mockApproveDelay);

//         // (여기 도착) 결제 승인 분기
//         if (_alwaysSuccess)
//         {
//             if (_textMeshPro != null)
//             {
//                 _textMeshPro.text = "결제 성공";
//             }

//             // ⏱ 2단계: 2초 더 대기
//             yield return new WaitForSeconds(5f);

//             // (여기서부터 순서대로 실행)
//             GameManager.Instance.SetState(KioskState.Filming);
//             print("어디서 호출되냐");
//             _fadeAnimationCtrl.StartFade();
//             OnPaymentApproved();
//             StopLoading();
//         }
//     }


//     // ---------- 실제 결제 (SDK 연동 지점) ----------

//     /// <summary>
//     /// 실제 결제 연동 시작 지점
//     /// - 상용 환경에서는 여기에서 결제 SDK 또는 외부 EXE 를 호출
//     /// - 콜백/응답을 받은 후 OnPaymentApproved 또는 OnPaymentFailed 호출
//     /// </summary>
//     private void StartRealPayment()
//     {
//         // 실제 결제 연동 예시 (SDK 사용 시)
//         // JtnetSdk.RequestPayment(
//         //      amount: 4000,
//         //      orderId: "ORDER_001",
//         //      onSuccess: () => { OnPaymentApproved(); },
//         //      onFail:    (err) => { OnPaymentFailed(err); }
//         // );
//         // amount : 결제 금액
//         // orderId : 주문 번호, 트랜잭션 ID
//         // onSuccess : 결제 승인 시 호출될 콜백
//         // onFail : 결제 실패 시 호출될 콜백

//         // 또는 별도 EXE 호출 방식:
//         // - 외부 프로그램과 통신하여 결제 결과를 받아온 뒤
//         //   성공 시 OnPaymentApproved(), 실패 시 OnPaymentFailed(reason) 호출.

//         if (_textMeshPro != null)
//             _textMeshPro.text = "결제 처리 중입니다...";
//     }

//     // ---------- 결제 결과 콜백 ----------

//     /// <summary>
//     /// 결제 성공 시 호출
//     /// </summary>
//     private void OnPaymentApproved()
//     {
//         _isProcessing = false;
//         StopLoading();

//         Debug.Log("[PAY] 결제 승인");
//     }
//     /// <summary>
//     /// 외부 호출용 콜백 함수 (페이드 Start가 끝났을 때 호출됨)
//     /// </summary>
//     public void OnCallbackEnd()
//     {
//         // 상태를 Ready 로 되돌림
//         GameManager.Instance.SetState(KioskState.Filming);

//         // _fadeAnimationCtrl._isStateStep = 3;
//         // 결제 패널은 닫고, Ready 패널을 다시 표시
//         if (_paymentPanel != null) _paymentPanel.SetActive(false);
//         else print("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
//         if (_readyPanel != null) _readyPanel.SetActive(true);
//         else print("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
//     }

//     /// <summary>
//     /// 결제 실패 시 호출
//     /// </summary>
//     private void OnPaymentFailed(string reason)
//     {
//         _isProcessing = false;
//         StopLoading();

//         Debug.LogWarning("[PAY] 결제 실패: " + reason);

//         // 결제 대기 상태로 되돌림 (재시도 가능 상태)
//         GameManager.Instance.SetState(KioskState.WaitingForPayment);

//         if (_textMeshPro != null)
//             _textMeshPro.text = "결제에 실패했습니다.\n" + reason;

//         // 이후 UX 예시:
//         // - '다시 시도' 버튼으로 결제 재시도
//         // - '처음으로' 버튼으로 Ready 화면 복귀 등 추가 처리 가능
//     }
// }
