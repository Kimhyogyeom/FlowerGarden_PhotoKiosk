using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 네트워크 Ping 체크 유틸리티
/// - ICMP Ping(UnityEngine.Ping)을 사용해 지정 IP까지 응답 시간 측정
/// - ICMP 실패 시 옵션에 따라 HTTP 요청(Head)으로 핑 대체 측정
/// - 버튼 클릭 시 1회 측정 / 필요 시 연속 측정(Loop)도 지원
/// - 결과를 TMP/UGUI 텍스트에 표시
/// </summary>
public class NetworkPingChecker : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("ICMP ping 대상 (IP 권장, 예: 8.8.8.8 / 1.1.1.1)")]
    [SerializeField] private string _icmpHostOrIp = "8.8.8.8";

    [Tooltip("가벼운 HTTP 테스트용 URL (예: 204 응답)")]
    [SerializeField] private string _httpProbeUrl = "https://www.google.com/generate_204";

    [Header("Settings")]
    [SerializeField] private int _attempts = 4;            // 측정 시도 횟수
    [SerializeField] private int _timeoutMs = 1500;        // 타임아웃(ms)
    [SerializeField] private bool _autoHttpFallback = true; // ICMP 실패 시 HTTP로 자동 폴백 할지 여부

    [Header("UI (optional)")]
    [SerializeField] private TextMeshProUGUI _labelTMP;       // TMP 결과 표시용
    [SerializeField] private UnityEngine.UI.Text _labelUGUI;  // UGUI 결과 표시용

    private Coroutine _loopCo; // 연속 측정용 Loop 코루틴 핸들

    [Header("Object Setting")]
    [SerializeField] private Button _buttonObject;            // 단발 측정 버튼

    /// <summary>
    /// 버튼 리스너 등록
    /// </summary>
    private void Awake()
    {
        _buttonObject.onClick.AddListener(CheckOnce);
    }

    /// <summary>
    /// 1회만 Ping 측정
    /// - 이전 연속 측정 코루틴이 돌고 있으면 먼저 정지
    /// - MeasureOnceAndShow 코루틴 실행
    /// </summary>
    public void CheckOnce()
    {
        if (_loopCo != null) { StopCoroutine(_loopCo); _loopCo = null; }
        StartCoroutine(MeasureOnceAndShow());
    }

    /// <summary>
    /// 일정 주기로 Ping을 계속 측정하고 UI에 갱신
    /// </summary>
    /// <param name="intervalSeconds">측정 간격(초)</param>
    public void StartContinuous(float intervalSeconds = 5f)
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = StartCoroutine(Loop(intervalSeconds));
    }

    /// <summary>
    /// 연속 측정을 중단하고 상태 표시를 갱신
    /// </summary>
    public void StopContinuous()
    {
        if (_loopCo != null) { StopCoroutine(_loopCo); _loopCo = null; }
        SetLabel("Ping: (stopped)");
    }

    /// <summary>
    /// 주기적으로 Ping 측정을 반복하는 루프 코루틴
    /// </summary>
    private IEnumerator Loop(float interval)
    {
        while (true)
        {
            yield return MeasureOnceAndShow();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, interval));
        }
    }

    /// <summary>
    /// 실제 1회 Ping 측정을 수행하고 결과를 UI에 표시하는 코루틴
    /// </summary>
    private IEnumerator MeasureOnceAndShow()
    {
        SetLabel("Ping: measuring...");

        var result = new Result();
        yield return StartCoroutine(MeasurePing(result));

        if (result.SuccessCount > 0)
        {
            SetLabel(
                $"Ping [{result.Method}] - avg {result.AvgMs:F0} ms (min {result.MinMs:F0} / max {result.MaxMs:F0})"
            );
        }
        else
        {
            SetLabel($"Ping FAILED [{result.Method}]");
        }
    }

    /// <summary>
    /// 라벨(TMP/UGUI)에 공통으로 문자열 세팅
    /// </summary>
    private void SetLabel(string msg)
    {
        if (_labelTMP) _labelTMP.text = msg;
        if (_labelUGUI) _labelUGUI.text = msg;
    }

    /// <summary>
    /// Ping 결과를 담는 내부 클래스
    /// - Method: 측정 방식(ICMP / HTTP)
    /// - SuccessCount: 성공한 시도 횟수
    /// - AvgMs / MinMs / MaxMs: 지연시간 통계
    /// </summary>
    [Serializable]
    private class Result
    {
        public string Method = "ICMP";
        public int SuccessCount;
        public float AvgMs;
        public float MinMs = float.MaxValue;
        public float MaxMs = 0f;

        /// <summary>
        /// 시도 1회 결과(ms)를 통계에 반영
        /// </summary>
        public void Add(float ms)
        {
            SuccessCount++;
            AvgMs += ms;
            if (ms < MinMs) MinMs = ms;
            if (ms > MaxMs) MaxMs = ms;
        }

        /// <summary>
        /// 누적된 결과를 바탕으로 평균/최소값 마무리 계산
        /// </summary>
        public void FinalizeAverage()
        {
            if (SuccessCount > 0) AvgMs /= SuccessCount;
            if (MinMs == float.MaxValue) MinMs = 0f;
        }
    }

    /// <summary>
    /// ICMP → (실패 시 옵션에 따라) HTTP 순으로 Ping을 측정
    /// </summary>
    private IEnumerator MeasurePing(Result result)
    {
        result.Method = "ICMP";
        yield return StartCoroutine(MeasureICMP(result));

        // ICMP가 모두 실패했고 HTTP 폴백이 활성화되어 있을 때
        if (result.SuccessCount == 0 && _autoHttpFallback)
        {
            result.Method = "HTTP";
            yield return StartCoroutine(MeasureHTTP(result));
        }

        result.FinalizeAverage();
    }

    /// <summary>
    /// UnityEngine.Ping 을 사용한 ICMP Ping 측정
    /// - _attempts 횟수만큼 시도
    /// - 각 시도마다 _timeoutMs 시간 동안 대기
    /// </summary>
    private IEnumerator MeasureICMP(Result result)
    {
        for (int i = 0; i < _attempts; i++)
        {
            var ping = new UnityEngine.Ping(_icmpHostOrIp);
            float start = Time.realtimeSinceStartup;
            float timeoutSec = _timeoutMs / 1000f;
            bool done = false;

            while (Time.realtimeSinceStartup - start < timeoutSec)
            {
                if (ping.isDone) { done = true; break; }
                yield return null;
            }

            if (done)
            {
                // ping.time 이 -1이면 직접 경과 시간으로 보정
                float ms = ping.time >= 0 ? ping.time : (Time.realtimeSinceStartup - start) * 1000f;
                result.Add(ms);
            }
            else
            {
                // timeout 발생: 실패로 간주(카운트 증가 X)
            }
        }
    }

    /// <summary>
    /// HTTP HEAD 요청을 사용해 네트워크 지연시간 측정
    /// - 주로 ICMP 차단 환경에서 폴백용으로 사용
    /// </summary>
    private IEnumerator MeasureHTTP(Result result)
    {
        for (int i = 0; i < _attempts; i++)
        {
            float start = Time.realtimeSinceStartup;
            using (var req = UnityWebRequest.Head(_httpProbeUrl))
            {
                req.timeout = Mathf.CeilToInt(_timeoutMs / 1000f);
                yield return req.SendWebRequest();

                bool ok = !(req.result == UnityWebRequest.Result.ConnectionError ||
                            req.result == UnityWebRequest.Result.ProtocolError);

                if (ok)
                {
                    float ms = (Time.realtimeSinceStartup - start) * 1000f;
                    result.Add(ms);
                }
                else
                {
                    // HTTP 에러 발생: 실패로 간주(카운트 증가 X)
                }
            }
        }
    }
}
