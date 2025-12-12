using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
/// <summary>
/// 1차: tPayDaemon HTTP (K1) 승인 요청/응답
/// 2차: 받은 K1 응답 JSON을 우리 서버로 그대로 POST 전송
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
    [SerializeField] private string _posSerialNo = "JTPOSDM16011E278"; // 예시 시리얼
    [SerializeField] private int _amount = 913;                        // 예시 금액
    [SerializeField] private int _tax = 91;                         // 예시 세금

    [Header("UI (옵션)")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [Header("모든 처리 완료 시 호출되는 이벤트")]
    [SerializeField] private UnityEvent _onAllCompleted;

    private bool _isRequesting = false;
    private long _msgNoCounter = 1;

    private void Start()
    {
        Debug.Log("[PAY-HTTP] PaymentHttpTester Start() 호출됨");
        Debug.Log("[PAY-HTTP] _k1Url = " + _k1Url);
        Debug.Log("[PAY-HTTP] _backendUrl = " + _backendUrl);
    }

    /// <summary>
    /// "결제 시작" 버튼 OnClick 에 연결
    /// </summary>
    public void OnClickStartPayment()
    {
        Debug.Log("[PAY-HTTP] >>> OnClickStartPayment() 호출됨");

        if (_isRequesting)
        {
            Debug.Log("[PAY-HTTP] 이미 요청 중입니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_k1Url))
        {
            Debug.LogError("[PAY-HTTP] K1 URL 이 비어있습니다. 인스펙터에서 _k1Url 설정 필요");
            SetStatus("K1 URL 미설정");
            return;
        }

        Debug.Log("========== [PAY-HTTP] 결제 요청 시작 (K1) ==========");
        StartCoroutine(SendPaymentRequestToK1Coroutine());
    }

    /// <summary>
    /// 카드 승인 + K1 응답 수신 + 우리 서버 저장까지
    /// 전부 성공했을 때 한 번만 호출되는 콜백
    /// </summary>
    private void OnAllProcessCompleted()
    {
        Debug.Log("[PAY-HTTP] ### 모든 결제/서버 저장 프로세스 완료 ###");
        Debug.Log("[PAY-HTTP] ### 여기서 외부 스크립트 실행하면 됩니다. ###");

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
        Debug.Log("[PAY-HTTP] K1 Request JSON = " + requestJson);

        // 요청 JSON도 필드별로 보고 싶으면:
        LogAllJsonFields(requestJson, "[PAY-HTTP] K1 REQUEST FIELD");

        // 2) HTTP POST 전송 (K1)
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
        using (UnityWebRequest request = new UnityWebRequest(_k1Url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("[PAY-HTTP] K1 HTTP POST 보내는 중... " + _k1Url);

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"[PAY-HTTP] K1 요청 실패: {request.error}");
                Debug.LogError("[PAY-HTTP] K1 Response(에러) = " + request.downloadHandler.text);
                SetStatus("K1 결제 요청 실패: " + request.error);
                _isRequesting = false;
                yield break;
            }

            string k1Response = request.downloadHandler.text;

            Debug.Log("========== [PAY-HTTP] K1 응답 수신 ==========");
            Debug.Log("[PAY-HTTP] K1 RAW RESPONSE = " + k1Response);

            string pretty = PrettyPrintJson(k1Response);
            Debug.Log("[PAY-HTTP] K1 PRETTY JSON:\n" + pretty);

            // 응답 필드 모두 출력
            LogAllJsonFields(k1Response, "[PAY-HTTP] K1 RESPONSE FIELD");

            SetStatus("K1 결제 응답 수신\n서버로 전송 준비 중...");

            // REPLY 코드 한번 뽑아보기 (0000 이면 승인)
            string replyCode = ExtractJsonStringField(k1Response, "REPLY");
            Debug.Log("[PAY-HTTP] K1 REPLY 코드 = " + replyCode);

            // 2단계: 받은 JSON 그대로 우리 서버로 전송
            yield return StartCoroutine(ForwardK1ResponseToBackendCoroutine(k1Response, replyCode));
        }

        _isRequesting = false;
    }

    // ─────────────────────────────────────────────
    // 2단계: 받은 K1 응답을 서버로 그대로 POST
    // ─────────────────────────────────────────────

    private IEnumerator ForwardK1ResponseToBackendCoroutine(string k1Json, string replyCode)
    {
        if (string.IsNullOrWhiteSpace(_backendUrl))
        {
            Debug.LogError("[PAY-HTTP] Backend URL 이 비어있습니다. 인스펙터에서 _backendUrl 설정 필요");
            SetStatus("백엔드 URL 미설정");
            yield break;
        }

        SetStatus("결제 결과를 서버로 전송 중...");

        // ★ 요구사항: "받은 정보 모두 그대로" → k1Json 을 그대로 보냄
        byte[] bodyRaw = Encoding.UTF8.GetBytes(k1Json);

        using (UnityWebRequest request = new UnityWebRequest(_backendUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("[PAY-HTTP] Backend HTTP POST 보내는 중... " + _backendUrl);
            Debug.Log("[PAY-HTTP] Backend Request Body(K1 그대로) = " + k1Json);

            yield return request.SendWebRequest();

            long statusCode = request.responseCode;
#if UNITY_2020_1_OR_NEWER
            bool netOk = (request.result == UnityWebRequest.Result.Success);
#else
            bool netOk = !(request.isNetworkError || request.isHttpError);
#endif

            string backendResp = request.downloadHandler.text;

            Debug.Log("========== [PAY-HTTP] Backend 응답 수신 ==========");
            Debug.Log($"[PAY-HTTP] Backend HTTP Status = {statusCode}");
            Debug.Log("[PAY-HTTP] Backend RAW RESPONSE = " + backendResp);

            // 서버 응답 JSON도 필드별로 찍어보기
            LogAllJsonFields(backendResp, "[PAY-HTTP] Backend RESPONSE FIELD");

            // 서버 스펙:
            // 200 + {"success": true, "receiptNo": "...", "message": "저장 완료"}
            // 400/500 + {"success": false, "message": "..."}
            bool isSuccess = netOk && statusCode == 200;

            // success 필드를 한번 더 확인 (true/false)
            bool? bodySuccess = ExtractJsonBoolField(backendResp, "success");
            if (bodySuccess.HasValue)
                isSuccess = bodySuccess.Value && statusCode == 200;

            string msg = ExtractJsonStringField(backendResp, "message");

            if (isSuccess)
            {
                Debug.Log("[PAY-HTTP] ▶ 서버 저장 성공");
                SetStatus("서버 저장 성공: " + (string.IsNullOrEmpty(msg) ? "저장 완료" : msg));

                // ★ 이 시점이 '모든 게 끝난 시점'
                OnAllProcessCompleted();
            }
            else
            {
                // 카드 승인 실패 케이스면 서버에서
                // "카드 승인 실패: 9999" 이런 메시지를 내려준다고 했음
                Debug.LogWarning("[PAY-HTTP] ▶ 서버 저장 실패");
                SetStatus("서버 저장 실패: " + (string.IsNullOrEmpty(msg) ? "알 수 없는 오류" : msg));
            }
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
            Debug.Log($"{prefix} {key} = {value}");
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

        Debug.Log("[PAY-HTTP] " + msg);
    }
}
