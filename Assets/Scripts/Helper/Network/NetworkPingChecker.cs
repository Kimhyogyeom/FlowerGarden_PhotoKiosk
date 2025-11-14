using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class NetworkPingChecker : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("ICMP ping target (IP recommended, e.g., 8.8.8.8 / 1.1.1.1)")]
    [SerializeField] private string _icmpHostOrIp = "8.8.8.8";

    [Tooltip("Lightweight HTTP probe URL (e.g., 204 response)")]
    [SerializeField] private string _httpProbeUrl = "https://www.google.com/generate_204";

    [Header("Settings")]
    [SerializeField] private int _attempts = 4;
    [SerializeField] private int _timeoutMs = 1500;
    [SerializeField] private bool _autoHttpFallback = true;

    [Header("UI (optional)")]
    [SerializeField] private TextMeshProUGUI _labelTMP;
    [SerializeField] private UnityEngine.UI.Text _labelUGUI;

    private Coroutine _loopCo;

    [Header("Object Setting")]
    [SerializeField] private Button _buttonObject;

    private void Awake()
    {
        _buttonObject.onClick.AddListener(CheckOnce);
    }
    public void CheckOnce()
    {
        if (_loopCo != null) { StopCoroutine(_loopCo); _loopCo = null; }
        StartCoroutine(MeasureOnceAndShow());
    }

    public void StartContinuous(float intervalSeconds = 5f)
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = StartCoroutine(Loop(intervalSeconds));
    }

    public void StopContinuous()
    {
        if (_loopCo != null) { StopCoroutine(_loopCo); _loopCo = null; }
        SetLabel("Ping: (stopped)");
    }

    private IEnumerator Loop(float interval)
    {
        while (true)
        {
            yield return MeasureOnceAndShow();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, interval));
        }
    }

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

    private void SetLabel(string msg)
    {
        if (_labelTMP) _labelTMP.text = msg;
        if (_labelUGUI) _labelUGUI.text = msg;
    }

    [Serializable]
    private class Result
    {
        public string Method = "ICMP";
        public int SuccessCount;
        public float AvgMs;
        public float MinMs = float.MaxValue;
        public float MaxMs = 0f;

        public void Add(float ms)
        {
            SuccessCount++;
            AvgMs += ms;
            if (ms < MinMs) MinMs = ms;
            if (ms > MaxMs) MaxMs = ms;
        }

        public void FinalizeAverage()
        {
            if (SuccessCount > 0) AvgMs /= SuccessCount;
            if (MinMs == float.MaxValue) MinMs = 0f;
        }
    }

    private IEnumerator MeasurePing(Result result)
    {
        result.Method = "ICMP";
        yield return StartCoroutine(MeasureICMP(result));

        if (result.SuccessCount == 0 && _autoHttpFallback)
        {
            result.Method = "HTTP";
            yield return StartCoroutine(MeasureHTTP(result));
        }

        result.FinalizeAverage();
    }

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
                float ms = ping.time >= 0 ? ping.time : (Time.realtimeSinceStartup - start) * 1000f;
                result.Add(ms);
            }
            else
            {
                // timeout
            }
        }
    }

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
                    // failure
                }
            }
        }
    }
}
