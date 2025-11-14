using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleSessionExporter : MonoBehaviour
{
    [Header("필터")]
    [SerializeField] private bool _includeInfo = true;
    [SerializeField] private bool _includeWarning = true;
    [SerializeField] private bool _includeError = true;

    [Header("형식")]
    [Tooltip("스택트레이스를 Unity 콘솔처럼 줄바꿈 유지")]
    [SerializeField] private bool _multilineStackTrace = true;

    [Tooltip("반복 로그를 접어서 count만 증가")]
    [SerializeField] private bool _collapseDuplicates = true;

    [Header("파일")]
    [SerializeField] private string _filePrefix = "Kiosk_Console_Session";
    [Tooltip("Windows면 바탕화면, 없으면 persistentDataPath")]
    [SerializeField] private bool _preferDesktop = true;

    private readonly object _lockObj = new object();

    [Header("Object")]
    [SerializeField] private Button _exportButton;
    [SerializeField] private TextMeshProUGUI _exportText;

    private void Awake()
    {
        _exportButton.onClick.AddListener(ExportToTxt);
    }
    // 한 줄 단위 로그 저장
    private struct LogRow
    {
        public DateTime Time;
        public LogType Type;
        public string Message;
        public string Stack;
        public int Count; // collapse 시 누적 횟수
    }

    private readonly List<LogRow> _rows = new List<LogRow>(2048);
    private bool _subscribed;

    private void OnEnable()
    {
        Subscribe();
        ClearSession(); // 이 컴포넌트가 활성화된 시점부터의 세션만 캡처
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        Application.logMessageReceived += OnLogMessage; // 메인스레드 콜백(콘솔 표시 순서와 더 유사)
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        Application.logMessageReceived -= OnLogMessage;
        _subscribed = false;
    }

    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!PassesFilter(type)) return;

        var row = new LogRow
        {
            Time = DateTime.Now,
            Type = type,
            Message = condition ?? string.Empty,
            Stack = stackTrace ?? string.Empty,
            Count = 1
        };

        lock (_lockObj)
        {
            if (_collapseDuplicates && _rows.Count > 0)
            {
                // 직전 항목과 동일 메시지+타입이면 접기
                var last = _rows[_rows.Count - 1];
                if (last.Type == row.Type && last.Message == row.Message && last.Stack == row.Stack)
                {
                    last.Count += 1;
                    _rows[_rows.Count - 1] = last;
                    return;
                }
            }
            _rows.Add(row);
        }
    }

    private bool PassesFilter(LogType type)
    {
        if (type == LogType.Log && !_includeInfo) return false;
        if (type == LogType.Warning && !_includeWarning) return false;
        if ((type == LogType.Error || type == LogType.Assert || type == LogType.Exception) && !_includeError) return false;
        return true;
    }

    // 버튼에 연결: 현재 세션 콘솔을 단일 TXT로 저장
    public void ExportToTxt()
    {
        try
        {
            string dir = GetPreferredFolder();
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, _filePrefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

            var sb = new StringBuilder(1024);
            sb.AppendLine("Unity Console Session");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("App: " + Application.productName + " " + Application.version);
            sb.AppendLine("Company: " + Application.companyName);
            sb.AppendLine("Platform: " + Application.platform);
            sb.AppendLine("Filters: " +
                (_includeInfo ? "INFO " : "") +
                (_includeWarning ? "WARN " : "") +
                (_includeError ? "ERROR " : ""));
            sb.AppendLine("----------------------------------------");

            lock (_lockObj)
            {
                foreach (var r in _rows)
                {
                    // 헤더 라인: 시간 [LEVEL] xCount
                    sb.Append('[').Append(r.Time.ToString("HH:mm:ss")).Append("] [")
                      .Append(MapLevel(r.Type)).Append(']');
                    if (r.Count > 1) sb.Append(" x").Append(r.Count);
                    sb.Append(' ').AppendLine(r.Message);

                    // 스택트레이스 형식
                    if (!string.IsNullOrEmpty(r.Stack))
                    {
                        if (_multilineStackTrace)
                        {
                            // 원본 줄바꿈 유지
                            sb.AppendLine(r.Stack.TrimEnd());
                        }
                        else
                        {
                            // 한 줄로 압축
                            string oneLine = r.Stack.Replace("\r", "").Replace("\n", " | ");
                            sb.AppendLine(oneLine);
                        }
                    }
                }
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); // UTF-8, BOM 없음
            Debug.Log("[ConsoleExport] Saved: " + path);

            // 여기 텍스트
            _exportText.text = path;
        }
        catch (Exception ex)
        {
            Debug.LogError("[ConsoleExport] Export failed: " + ex.Message);
        }
    }

    // 버튼에 연결(선택): 현재 세션 버퍼 삭제
    public void ClearSession()
    {
        lock (_lockObj)
        {
            _rows.Clear();
        }
        Debug.Log("[ConsoleExport] Session cleared");
    }

    private static string MapLevel(LogType t)
    {
        switch (t)
        {
            case LogType.Log: return "INFO";
            case LogType.Warning: return "WARN";
            case LogType.Error: return "ERROR";
            case LogType.Assert: return "ASSERT";
            case LogType.Exception: return "EXCEPTION";
            default: return "INFO";
        }
    }

    private string GetPreferredFolder()
    {
        if (_preferDesktop)
        {
            try
            {
                string desk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (!string.IsNullOrEmpty(desk) && Directory.Exists(desk))
                    return desk;
            }
            catch { }
        }
        return Application.persistentDataPath;
    }
}
