using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

/// <summary>
/// 1차: tPayDaemon HTTP (K1) 승인 요청/응답
/// 2차: 받은 K1 응답 JSON을 우리 서버로 그대로 POST 전송
/// ⚠ _ignoreCertificateError = true 일 때 HTTPS 인증서 검증 우회 (테스트용)
/// </summary>
public class PaymentHttpTester : MonoBehaviour
{
    [Header("K1 (tPayDaemon) HTTP 설정")]
    [Tooltip("tPayDaemon Auth URL (예: http://127.0.0.1:6444/tPayDaemon/Auth)")]
    [SerializeField] private string _k1Url = "http://127.0.0.1:6444/tPayDaemon/Auth";

    [Header("우리 서버(백엔드) URL")]
    [Tooltip("K1 응답을 그대로 보낼 서버 URL")]
    [SerializeField] private string _backendUrl = "https://6c038f8e8a65.ngrok-free.app/complete";

    [Header("결제/가맹점 설정")]
    [SerializeField] private string _tid = "1004930001";               // TEST용 TID
    [SerializeField] private string _posSerialNo = "JTPOSDM16011E278"; // 단말 시리얼
    public int _amount = 100;                        // 결제 금액(원)
    [SerializeField] private int _tax = 0;                             // 세금(원)

    [Header("UI (옵션)")]
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("모든 처리 완료 시 호출되는 이벤트")]
    [SerializeField] private UnityEvent _onAllCompleted;

    [Header("승인 성공 + 서버 연동 실패 시 자동 취소 시도 여부 (미구현 훅)")]
    [SerializeField] private bool _tryAutoCancelOnBackendFail = false;

    [Header("⚠ HTTPS 인증서 검증 무시 (테스트용)")]
    [SerializeField] private bool _ignoreCertificateError = false;

    [Header("로컬 백업 설정")]
    [Tooltip("K1 승인 성공 시 백엔드 실패해도 다음 화면으로 진행")]
    [SerializeField] private bool _proceedOnK1ApprovalOnly = true;

    [Tooltip("백엔드 전송 실패 시 즉시 재시도 횟수")]
    [SerializeField] private int _immediateRetryCount = 3;

    [Tooltip("재시도 간격 (초)")]
    [SerializeField] private float _retryInterval = 2f;

    private bool _isRequesting = false;
    private long _msgNoCounter = 1;

    // 결과 텍스트 자동 초기화용 코루틴 핸들
    private Coroutine _clearStatusCoroutine;

    [SerializeField] private float _messageTextTimer = 5f;

    // 현재 결제 건의 고유 ID (로컬 백업에서 찾기 위해)
    private string _currentPaymentId = null;

    // 로컬 백업 파일 경로
    private string BackupFolderPath => Path.Combine(Application.dataPath, "..", "PaymentBackup");
    private string BackupFilePath => Path.Combine(BackupFolderPath, "failed_payments.json");
    /// <summary>
    /// HTTPS 인증서 검증 우회용 핸들러 (테스트 환경 전용)
    /// </summary>
    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // 무조건 신뢰 (매우 위험, 테스트/내부망에서만!)
            return true;
        }
    }

    private void Start()
    {
        // Debug.Log("[PAY-HTTP] PaymentHttpTester Start() 호출됨");
        // Debug.Log("[PAY-HTTP] _k1Url = " + _k1Url);
        // Debug.Log("[PAY-HTTP] _backendUrl = " + _backendUrl);

        // 시작 시에도 한 번 초기화
        ClearStatusTextImmediate();

        // 앱 시작 시 미전송 결제 데이터 재전송 시도
        StartCoroutine(RetryFailedPaymentsCoroutine());
    }

    private void OnDisable()
    {
        // 패널 비활성화될 때도 텍스트는 항상 비워두기
        ClearStatusTextImmediate();
    }

    /// <summary>
    /// "결제 시작" 버튼 OnClick 에 연결
    /// </summary>
    public void OnClickStartPayment()
    {
        // Debug.Log("[PAY-HTTP] >>> OnClickStartPayment() 호출됨");

        if (_isRequesting)
        {
            // Debug.Log("[PAY-HTTP] 이미 요청 중입니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_k1Url))
        {
            Debug.LogError("[PAY-HTTP] K1 URL 이 비어있습니다. 인스펙터에서 _k1Url 설정 필요");
            SetStatus("K1 URL 미설정");
            return;
        }

        // Debug.Log("========== [PAY-HTTP] 결제 요청 시작 (K1) ==========");
        StartCoroutine(SendPaymentRequestToK1Coroutine());
    }

    /// <summary>
    /// 카드 승인 + K1 응답 수신 + 우리 서버 저장까지
    /// 전부 성공했을 때 한 번만 호출되는 콜백
    /// </summary>
    private void OnAllProcessCompleted()
    {
        // Debug.Log("[PAY-HTTP] ### 모든 결제/서버 저장 프로세스 완료 ###");
        // Debug.Log("[PAY-HTTP] ### 여기서 외부 스크립트 실행하면 됩니다. ###");

        // 인스펙터에서 연결해둔 외부 함수들 호출
        _onAllCompleted?.Invoke();
    }

    // ─────────────────────────────────────────────
    // 1단계: K1(tPayDaemon)으로 승인 요청
    // ─────────────────────────────────────────────

    private IEnumerator SendPaymentRequestToK1Coroutine()
    {
        _isRequesting = true;
        SetStatus("카드 결제 요청 중... (K1)");

        // 1) 요청 JSON 구성
        string requestJson = BuildRequestJson();
        // Debug.Log("[PAY-HTTP] K1 Request JSON = " + requestJson);

        // 요청 JSON도 필드별로 보고 싶으면:
        // LogAllJsonFields(requestJson, "[PAY-HTTP] K1 REQUEST FIELD");

        // 2) HTTP POST 전송 (K1)
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
        using (UnityWebRequest request = new UnityWebRequest(_k1Url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // HTTPS 이고, 우회 옵션이 켜져 있으면 인증서 무시
            if (_ignoreCertificateError &&
                _k1Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.disposeCertificateHandlerOnDispose = true;
                Debug.LogWarning("[PAY-HTTP] ⚠ K1 HTTPS 인증서 검증을 무시하고 요청합니다. (테스트 전용)");
            }

            // Debug.Log("[PAY-HTTP] K1 HTTP POST 보내는 중... " + _k1Url);

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"[PAY-HTTP] K1 요청 실패: {request.error}");
                Debug.LogError("[PAY-HTTP] K1 Response(에러) = " + request.downloadHandler.text);

                // ☆ 1) 네트워크/연결 문제
                ShowResultMessageTemporary(
                    "카드 승인 서버 연결에 실패했습니다.\n네트워크 상태를 확인 후 다시 시도해주세요.",
                    _messageTextTimer
                );

                _isRequesting = false;
                yield break;
            }

            string k1Response = request.downloadHandler.text;

            // Debug.Log("========== [PAY-HTTP] K1 응답 수신 ==========");
            // Debug.Log("[PAY-HTTP] K1 RAW RESPONSE = " + k1Response);

            // string pretty = PrettyPrintJson(k1Response);
            // Debug.Log("[PAY-HTTP] K1 PRETTY JSON:\n" + pretty);

            // 응답 필드 모두 출력
            // LogAllJsonFields(k1Response, "[PAY-HTTP] K1 RESPONSE FIELD");

            // 에러 체크 필드들
            string errorCheckResult = ExtractJsonStringField(k1Response, "ERROR_CHECK_RESULT");
            string errorCheckCode = ExtractJsonStringField(k1Response, "ERROR_CHECK_CODE");
            string errorCheckMessage = ExtractJsonStringField(k1Response, "ERROR_CHECK_MESSAGE");
            string replyCode = ExtractJsonStringField(k1Response, "REPLY");

            // ☆ 2) K1에서 자체 에러 리턴 (환경/설정 문제 등)
            if (!string.IsNullOrEmpty(errorCheckResult) && errorCheckResult != "S")
            {
                string msg = $"카드 승인 중 오류가 발생했습니다.\n" +
                             $"[코드 {errorCheckCode}] {errorCheckMessage}";
                ShowResultMessageTemporary(msg, _messageTextTimer);

                _isRequesting = false;
                yield break;
            }

            // ☆ 3) 카드사 측 승인 실패 (REPLY != 0000)
            if (!string.IsNullOrEmpty(replyCode) && replyCode != "0000")
            {
                string msg = $"결제가 승인되지 않았습니다.\n응답 코드: {replyCode}";
                ShowResultMessageTemporary(msg, _messageTextTimer);

                _isRequesting = false;
                yield break;
            }

            // 여기까지 왔으면 카드 승인 성공
            // Debug.Log("[PAY-HTTP] K1 REPLY 코드 = " + replyCode);
            SetStatus("카드 승인 완료");

            // ★ 안전 모드: K1 승인 성공 즉시 로컬에 백업 저장 (데이터 유실 방지)
            _currentPaymentId = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + UnityEngine.Random.Range(1000, 9999);
            SavePaymentToLocalBackup(k1Response, _currentPaymentId);
            Debug.Log($"[PAY-HTTP] K1 승인 성공 → 로컬 백업 저장 완료 (ID: {_currentPaymentId})");

            // ★ K1 승인만 성공하면 바로 다음 화면으로 전환 (사용자 경험 우선)
            if (_proceedOnK1ApprovalOnly)
            {
                Debug.Log("[PAY-HTTP] K1 승인 성공 → 바로 다음 화면 전환");
                OnAllProcessCompleted();
            }

            // 2단계: 받은 JSON 그대로 우리 서버로 전송 (백그라운드, 재시도 포함)
            yield return StartCoroutine(ForwardK1ResponseToBackendWithRetryCoroutine(k1Response, replyCode, _currentPaymentId));
        }

        _isRequesting = false;
    }

    // ─────────────────────────────────────────────
    // 2단계: 받은 K1 응답을 서버로 그대로 POST (재시도 포함)
    // ─────────────────────────────────────────────

    private IEnumerator ForwardK1ResponseToBackendWithRetryCoroutine(string k1Json, string replyCode, string paymentId)
    {
        bool k1Approved = replyCode == "0000";

        if (string.IsNullOrWhiteSpace(_backendUrl))
        {
            Debug.LogError("[PAY-HTTP] Backend URL 이 비어있습니다. 인스펙터에서 _backendUrl 설정 필요");
            if (!_proceedOnK1ApprovalOnly)
            {
                ShowResultMessageTemporary("서버 URL 설정이 되어 있지 않습니다.\n관리자에게 문의해주세요.", _messageTextTimer);
            }
            yield break;
        }

        // 즉시 재시도 포함해서 전송 시도
        bool success = false;
        int totalAttempts = _immediateRetryCount + 1; // 최초 1회 + 재시도 횟수

        for (int attempt = 1; attempt <= totalAttempts; attempt++)
        {
            Debug.Log($"[PAY-HTTP] 백엔드 전송 시도 {attempt}/{totalAttempts}");

            yield return StartCoroutine(SendToBackendOnceCoroutine(k1Json, (result) => success = result));

            if (success)
            {
                Debug.Log($"[PAY-HTTP] 백엔드 전송 성공 (시도 {attempt}회)");

                // ★ 성공하면 로컬 백업에서 삭제
                RemovePaymentFromLocalBackup(paymentId);
                Debug.Log($"[PAY-HTTP] 로컬 백업에서 삭제 완료 (ID: {paymentId})");

                // _proceedOnK1ApprovalOnly가 false일 때만 여기서 콜백
                if (!_proceedOnK1ApprovalOnly)
                {
                    ShowResultMessageTemporary("결제가 정상적으로 완료되었습니다.", _messageTextTimer);
                    OnAllProcessCompleted();
                }

                yield break; // 성공했으므로 종료
            }

            // 실패했으면 재시도 전 대기
            if (attempt < totalAttempts)
            {
                Debug.Log($"[PAY-HTTP] 백엔드 전송 실패, {_retryInterval}초 후 재시도...");
                yield return new WaitForSeconds(_retryInterval);
            }
        }

        // 모든 시도 실패
        Debug.LogError($"[PAY-HTTP] 백엔드 전송 최종 실패 ({totalAttempts}회 시도)");
        Debug.Log($"[PAY-HTTP] 로컬 백업에 유지됨 (ID: {paymentId}) - 앱 재시작 시 재전송 시도");

        // _proceedOnK1ApprovalOnly가 true면 이미 화면 전환됨, 메시지 표시 안 함
        if (!_proceedOnK1ApprovalOnly && k1Approved)
        {
            ShowResultMessageTemporary("결제는 승인되었으나 서버와 통신에 실패했습니다.\n관리자에게 문의해주세요.", _messageTextTimer);
        }

        // (선택) 서버 연동 실패 + 승인됨 → 추후 자동 취소 구현 위치
        if (_tryAutoCancelOnBackendFail && k1Approved)
        {
            TryAutoCancelPayment(k1Json);
        }
    }

    /// <summary>
    /// 백엔드로 1회 전송 시도 (재시도 없음)
    /// </summary>
    private IEnumerator SendToBackendOnceCoroutine(string k1Json, Action<bool> onComplete)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(k1Json);

        using (UnityWebRequest request = new UnityWebRequest(_backendUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (_ignoreCertificateError &&
                _backendUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request.certificateHandler = new BypassCertificateHandler();
                request.disposeCertificateHandlerOnDispose = true;
            }

            yield return request.SendWebRequest();

            long statusCode = request.responseCode;
#if UNITY_2020_1_OR_NEWER
            bool netOk = (request.result == UnityWebRequest.Result.Success);
#else
            bool netOk = !(request.isNetworkError || request.isHttpError);
#endif

            if (!netOk || statusCode == 0)
            {
                Debug.LogError($"[PAY-HTTP] Backend 요청 실패: {request.error}");
                onComplete?.Invoke(false);
                yield break;
            }

            string backendResp = request.downloadHandler.text;
            bool isSuccess = (statusCode == 200);
            bool? bodySuccess = ExtractJsonBoolField(backendResp, "success");
            if (bodySuccess.HasValue)
                isSuccess = isSuccess && bodySuccess.Value;

            onComplete?.Invoke(isSuccess);
        }
    }

    // ─────────────────────────────────────────────
    // K1 요청 JSON 생성
    // ─────────────────────────────────────────────

    private string BuildRequestJson()
    {
        string transTime = DateTime.Now.ToString("yyMMddHHmmss");   // TRANSTIME
        string amountStr = Mathf.Max(0, _amount).ToString("D9");    // AMOUNT (9자리)
        string taxStr = Mathf.Max(0, _tax).ToString("D9");       // TAX (9자리)
        string msgNoStr = (_msgNoCounter++).ToString("D12");       // MSGNO (12자리)

        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"TIMEOUT\":\"02\",");          // 고정
        sb.Append("\"MSGTYPE\":\"1010\",");        // 고정
        sb.AppendFormat("\"TID\":\"{0}\",", _tid);
        sb.AppendFormat("\"MSGNO\":\"{0}\",", msgNoStr);
        sb.AppendFormat("\"TRANSTIME\":\"{0}\",", transTime);
        sb.Append("\"INSTALLMENT\":\"00\",");      // 고정 (일시불)
        sb.AppendFormat("\"AMOUNT\":\"{0}\",", amountStr);
        sb.AppendFormat("\"TAX\":\"{0}\",", taxStr);
        sb.Append("\"SERVICE\":\"000000000\",");   // 고정
        sb.Append("\"CURRENCY\":\"KRW\",");        // 고정
        sb.Append("\"NOTAX\":\"000000000\",");     // 고정
        sb.AppendFormat("\"POSSERIALNO\":\"{0}\",", _posSerialNo);
        sb.Append("\"SIGNKBN\":\" \",");           // 고정
        sb.Append("\"CR\":\" \"");                 // 고정
        sb.Append('}');

        return sb.ToString();
    }

    // ─────────────────────────────────────────────
    // 결과 텍스트를 잠깐 보여주고 자동 초기화하는 유틸
    // ─────────────────────────────────────────────

    private void ShowResultMessageTemporary(string msg, float duration)
    {
        if (_statusText != null)
            _statusText.text = msg;

        // Debug.Log("[PAY-HTTP][UI] RESULT: " + msg);

        if (_clearStatusCoroutine != null)
        {
            StopCoroutine(_clearStatusCoroutine);
            _clearStatusCoroutine = null;
        }

        _clearStatusCoroutine = StartCoroutine(ClearStatusAfterDelay(duration));
    }

    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        ClearStatusTextImmediate();
        _clearStatusCoroutine = null;
    }

    private void ClearStatusTextImmediate()
    {
        if (_statusText != null)
            _statusText.text = string.Empty;
    }

    // ─────────────────────────────────────────────
    // 로컬 백업 저장/로드/재전송
    // ─────────────────────────────────────────────

    /// <summary>
    /// K1 승인 즉시 로컬에 백업 저장 (ID 포함)
    /// </summary>
    private void SavePaymentToLocalBackup(string k1Json, string paymentId)
    {
        try
        {
            // 폴더 생성
            if (!Directory.Exists(BackupFolderPath))
            {
                Directory.CreateDirectory(BackupFolderPath);
            }

            // 기존 데이터 로드
            List<FailedPaymentData> failedList = LoadFailedPayments();

            // 새 데이터 추가
            var newData = new FailedPaymentData
            {
                paymentId = paymentId,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                amount = _amount,
                k1Response = k1Json,
                retryCount = 0
            };
            failedList.Add(newData);

            // JSON으로 저장
            string json = JsonListToString(failedList);
            File.WriteAllText(BackupFilePath, json, Encoding.UTF8);

            Debug.Log($"[PAY-HTTP] 로컬 백업 저장 완료: {BackupFilePath} (총 {failedList.Count}건)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PAY-HTTP] 로컬 백업 저장 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 백엔드 전송 성공 시 로컬 백업에서 해당 건 삭제
    /// </summary>
    private void RemovePaymentFromLocalBackup(string paymentId)
    {
        try
        {
            List<FailedPaymentData> failedList = LoadFailedPayments();

            int removedCount = failedList.RemoveAll(x => x.paymentId == paymentId);

            if (removedCount > 0)
            {
                if (failedList.Count > 0)
                {
                    string json = JsonListToString(failedList);
                    File.WriteAllText(BackupFilePath, json, Encoding.UTF8);
                }
                else
                {
                    // 모두 삭제되면 파일도 삭제
                    if (File.Exists(BackupFilePath))
                    {
                        File.Delete(BackupFilePath);
                    }
                }
                Debug.Log($"[PAY-HTTP] 로컬 백업에서 삭제 완료 (ID: {paymentId})");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PAY-HTTP] 로컬 백업 삭제 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 로컬에 저장된 실패 결제 데이터 로드
    /// </summary>
    private List<FailedPaymentData> LoadFailedPayments()
    {
        var result = new List<FailedPaymentData>();

        if (!File.Exists(BackupFilePath))
            return result;

        try
        {
            string json = File.ReadAllText(BackupFilePath, Encoding.UTF8);
            result = JsonStringToList(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PAY-HTTP] 로컬 백업 로드 실패: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 앱 시작 시 미전송 결제 데이터 재전송 시도
    /// </summary>
    private IEnumerator RetryFailedPaymentsCoroutine()
    {
        // 앱 시작 후 잠시 대기
        yield return new WaitForSeconds(5f);

        List<FailedPaymentData> failedList = LoadFailedPayments();
        if (failedList.Count == 0)
        {
            Debug.Log("[PAY-HTTP] 미전송 결제 데이터 없음");
            yield break;
        }

        Debug.Log($"[PAY-HTTP] 미전송 결제 데이터 {failedList.Count}건 재전송 시도");

        var successIds = new List<string>(); // 성공한 ID 목록

        for (int i = 0; i < failedList.Count; i++)
        {
            var data = failedList[i];
            data.retryCount++;

            Debug.Log($"[PAY-HTTP] 재전송 시도 #{i + 1}: {data.savedAt} (ID: {data.paymentId}, 시도 횟수: {data.retryCount})");

            // 재시도 횟수 포함해서 시도
            bool success = false;
            int retryAttempts = _immediateRetryCount + 1;

            for (int attempt = 1; attempt <= retryAttempts; attempt++)
            {
                yield return StartCoroutine(SendToBackendOnceCoroutine(data.k1Response, (result) => success = result));

                if (success)
                {
                    Debug.Log($"[PAY-HTTP] 재전송 성공 #{i + 1} (시도 {attempt}회)");
                    successIds.Add(data.paymentId);
                    break;
                }

                if (attempt < retryAttempts)
                {
                    yield return new WaitForSeconds(_retryInterval);
                }
            }

            if (!success)
            {
                Debug.LogWarning($"[PAY-HTTP] 재전송 최종 실패 #{i + 1} (ID: {data.paymentId})");
            }

            // 요청 간 간격
            yield return new WaitForSeconds(1f);
        }

        // 성공한 건 삭제
        foreach (var id in successIds)
        {
            RemovePaymentFromLocalBackup(id);
        }

        // 결과 로그
        int remaining = failedList.Count - successIds.Count;
        if (remaining > 0)
        {
            Debug.Log($"[PAY-HTTP] 재전송 완료 - 성공: {successIds.Count}건, 실패: {remaining}건 (다음 앱 시작 시 재시도)");
        }
        else
        {
            Debug.Log("[PAY-HTTP] 모든 미전송 건 재전송 완료");
        }
    }

    // ─────────────────────────────────────────────
    // 로컬 백업용 데이터 클래스 & JSON 헬퍼
    // ─────────────────────────────────────────────

    [Serializable]
    private class FailedPaymentData
    {
        public string paymentId;
        public string savedAt;
        public int amount;
        public string k1Response;
        public int retryCount;
    }

    /// <summary>
    /// List를 JSON 문자열로 변환 (간단한 수동 직렬화)
    /// </summary>
    private string JsonListToString(List<FailedPaymentData> list)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('{');
            sb.AppendFormat("\"paymentId\":\"{0}\",", EscapeJsonString(list[i].paymentId ?? ""));
            sb.AppendFormat("\"savedAt\":\"{0}\",", EscapeJsonString(list[i].savedAt));
            sb.AppendFormat("\"amount\":{0},", list[i].amount);
            sb.AppendFormat("\"k1Response\":\"{0}\",", EscapeJsonString(list[i].k1Response));
            sb.AppendFormat("\"retryCount\":{0}", list[i].retryCount);
            sb.Append('}');
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// JSON 문자열을 List로 변환 (간단한 수동 역직렬화)
    /// </summary>
    private List<FailedPaymentData> JsonStringToList(string json)
    {
        var result = new List<FailedPaymentData>();
        if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
            return result;

        // 간단한 파싱: 각 객체를 분리
        int depth = 0;
        int objStart = -1;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '{')
            {
                if (depth == 0) objStart = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && objStart >= 0)
                {
                    string objJson = json.Substring(objStart, i - objStart + 1);
                    var data = ParseFailedPaymentData(objJson);
                    if (data != null)
                        result.Add(data);
                    objStart = -1;
                }
            }
        }

        return result;
    }

    private FailedPaymentData ParseFailedPaymentData(string objJson)
    {
        try
        {
            var data = new FailedPaymentData();
            data.paymentId = ExtractJsonStringField(objJson, "paymentId") ?? "";
            data.savedAt = ExtractJsonStringField(objJson, "savedAt") ?? "";
            data.k1Response = UnescapeJsonString(ExtractJsonStringField(objJson, "k1Response") ?? "");

            string amountStr = ExtractJsonNumberField(objJson, "amount");
            int.TryParse(amountStr, out data.amount);

            string retryStr = ExtractJsonNumberField(objJson, "retryCount");
            int.TryParse(retryStr, out data.retryCount);

            // paymentId가 없는 기존 데이터 호환성
            if (string.IsNullOrEmpty(data.paymentId))
            {
                data.paymentId = data.savedAt.Replace("-", "").Replace(":", "").Replace(" ", "") + "_legacy";
            }

            return data;
        }
        catch
        {
            return null;
        }
    }

    private string ExtractJsonNumberField(string json, string fieldName)
    {
        string pattern = "\"" + fieldName + "\"";
        int idx = json.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return null;

        idx = json.IndexOf(':', idx);
        if (idx < 0) return null;

        int start = idx + 1;
        while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            end++;

        if (end > start)
            return json.Substring(start, end - start);

        return null;
    }

    private string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private string UnescapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r");
    }

    // ─────────────────────────────────────────────
    // (미구현) 승인 취소 훅
    // ─────────────────────────────────────────────

    /// <summary>
    /// TODO: K1 / VAN "승인 취소" 프로토콜 연결 시 여기서 호출.
    /// 지금은 단순히 로그만 찍고 아무 것도 하지 않는다.
    /// </summary>
    private void TryAutoCancelPayment(string k1Json)
    {
        Debug.LogWarning(
            "[PAY-HTTP] 자동 승인취소 시도를 해야 하는 상황입니다. " +
            "실제 취소는 VAN / tPayDaemon 승인취소 API 스펙에 맞춰 별도 구현이 필요합니다.\n" +
            "참고용 K1 응답 데이터: " + k1Json
        );
    }

    // ─────────────────────────────────────────────
    // JSON Helper
    // ─────────────────────────────────────────────

    private void LogAllJsonFields(string json, string prefix)
    {
        if (string.IsNullOrEmpty(json)) return;

        var matches = Regex.Matches(
            json,
            "\"(?<key>[^\"\\r\\n]+)\"\\s*:\\s*\"(?<value>[^\"\\r\\n]*)\""
        );

        foreach (Match m in matches)
        {
            var key = m.Groups["key"].Value;
            var value = m.Groups["value"].Value;
            // Debug.Log($"{prefix} {key} = {value}");
        }
    }

    private string PrettyPrintJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        var sb = new StringBuilder();
        bool inQuotes = false;
        int indent = 0;

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            if (ch == '\"')
            {
                sb.Append(ch);
                bool escaped = false;
                int index = i;
                while (index > 0 && json[--index] == '\\')
                    escaped = !escaped;
                if (!escaped) inQuotes = !inQuotes;
            }
            else if (!inQuotes)
            {
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        continue;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent--;
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                        continue;
                    case ',':
                        sb.Append(ch);
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                        continue;
                    case ':':
                        sb.Append(" : ");
                        continue;
                }
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private string ExtractJsonStringField(string json, string fieldName)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName))
            return null;

        string pattern = "\"" + fieldName + "\"";
        int idx = json.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return null;

        idx = json.IndexOf(':', idx);
        if (idx < 0) return null;

        int firstQuote = json.IndexOf('\"', idx);
        if (firstQuote < 0) return null;
        int secondQuote = json.IndexOf('\"', firstQuote + 1);
        if (secondQuote < 0) return null;

        return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
    }

    private bool? ExtractJsonBoolField(string json, string fieldName)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName))
            return null;

        string pattern = "\"" + fieldName + "\"";
        int idx = json.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return null;

        idx = json.IndexOf(':', idx);
        if (idx < 0) return null;

        // ':' 다음 true/false 찾기 (따옴표 유무 상관없이)
        int i = idx + 1;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

        if (i >= json.Length) return null;

        // 따옴표로 둘러쌀 수도 있으니 한 번 건너뛰기
        if (json[i] == '\"')
            i++;

        if (json.Substring(i).StartsWith("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (json.Substring(i).StartsWith("false", StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null)
            _statusText.text = msg;

        // Debug.Log("[PAY-HTTP] " + msg);
    }
}
