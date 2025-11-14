using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 콘솔 세션 로그를 메모리에 모았다가 TXT 파일로 내보내는 유틸리티
/// - Application.logMessageReceived 를 구독해서 현재 세션 동안 발생한 로그를 모두 축적
/// - Info/Warning/Error 필터링 가능
/// - 같은 내용이 연속으로 찍히는 로그를 접어서(count 증가) 저장 가능
/// - 버튼 클릭 시 바탕화면(또는 persistentDataPath)에 텍스트 파일로 저장
/// </summary>
public class ConsoleSessionExporter : MonoBehaviour
{
    [Header("필터")]
    [SerializeField] private bool _includeInfo = true;      // 일반 로그(Log) 포함 여부
    [SerializeField] private bool _includeWarning = true;   // 경고(Warning) 포함 여부
    [SerializeField] private bool _includeError = true;     // 오류/Error/Exception 포함 여부

    [Header("형식")]
    [Tooltip("스택 트레이스를 Unity 콘솔처럼 줄바꿈 그대로 유지할지 여부")]
    [SerializeField] private bool _multilineStackTrace = true;

    [Tooltip("동일한 로그가 연속으로 들어올 때 접어서 count만 증가시킬지 여부")]
    [SerializeField] private bool _collapseDuplicates = true;

    [Header("파일")]
    [SerializeField] private string _filePrefix = "Kiosk_Console_Session"; // 파일 이름 앞부분
    [Tooltip("Windows면 바탕화면, 아니라면 persistentDataPath에 저장")]
    [SerializeField] private bool _preferDesktop = true;

    private readonly object _lockObj = new object(); // 멀티스레드 대비용 잠금 오브젝트

    [Header("Object")]
    [SerializeField] private Button _exportButton;         // 내보내기 버튼
    [SerializeField] private TextMeshProUGUI _exportText;  // 저장 경로 표시용 텍스트

    private void Awake()
    {
        // 내보내기 버튼 클릭 시 ExportToTxt 실행
        _exportButton.onClick.AddListener(ExportToTxt);
    }

    /// <summary>
    /// 한 줄 단위로 저장되는 로그 구조체
    /// - Time: 기록 시각
    /// - Type: LogType (Log/Warning/Error/Exception 등)
    /// - Message: 로그 본문
    /// - Stack: 스택 트레이스
    /// - Count: 해당 메시지가 연속으로 몇 번 나왔는지(접기 옵션 사용 시)
    /// </summary>
    private struct LogRow
    {
        public DateTime Time;
        public LogType Type;
        public string Message;
        public string Stack;
        public int Count; // collapse 시 누적 횟수
    }

    // 현재 세션 동안 수집된 로그 목록
    private readonly List<LogRow> _rows = new List<LogRow>(2048);
    private bool _subscribed;

    private void OnEnable()
    {
        // 콘솔 로그 구독 시작 + 세션 초기화
        Subscribe();
        ClearSession(); // 이 컴포넌트가 활성화된 이후부터의 로그만 캡처
    }

    private void OnDisable()
    {
        // 비활성화 시 구독 해제
        Unsubscribe();
    }

    /// <summary>
    /// Application.logMessageReceived 구독
    /// </summary>
    private void Subscribe()
    {
        if (_subscribed) return;
        // 메인 스레드 콜백: Unity 에디터 콘솔에 찍히는 순서와 유사
        Application.logMessageReceived += OnLogMessage;
        _subscribed = true;
    }

    /// <summary>
    /// Application.logMessageReceived 구독 해제
    /// </summary>
    private void Unsubscribe()
    {
        if (!_subscribed) return;
        Application.logMessageReceived -= OnLogMessage;
        _subscribed = false;
    }

    /// <summary>
    /// Unity 로그 콜백
    /// - condition: 로그 메시지
    /// - stackTrace: 스택 트레이스 문자열
    /// - type: 로그 타입(Log, Warning, Error, Exception 등)
    /// </summary>
    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        // 필터에 걸리면 무시
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
            // 같은 로그를 접어 쓰기 옵션이 켜져 있을 때
            if (_collapseDuplicates && _rows.Count > 0)
            {
                // 직전 항목과 타입/메시지/스택이 모두 같으면 count만 증가
                var last = _rows[_rows.Count - 1];
                if (last.Type == row.Type && last.Message == row.Message && last.Stack == row.Stack)
                {
                    last.Count += 1;
                    _rows[_rows.Count - 1] = last;
                    return;
                }
            }
            // 새 로그 추가
            _rows.Add(row);
        }
    }

    /// <summary>
    /// 현재 필터 설정에 따라 로그 타입을 통과시킬지 여부 판단
    /// </summary>
    private bool PassesFilter(LogType type)
    {
        if (type == LogType.Log && !_includeInfo) return false;
        if (type == LogType.Warning && !_includeWarning) return false;
        if ((type == LogType.Error || type == LogType.Assert || type == LogType.Exception) && !_includeError) return false;
        return true;
    }

    /// <summary>
    /// 버튼에 연결: 현재 세션의 콘솔 로그를 TXT 파일로 내보냄
    /// - 우선 바탕화면(Windows), 실패 시 persistentDataPath에 저장
    /// - 저장 후 경로를 _exportText에 표시
    /// </summary>
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
                    // 헤더 라인: [시간] [LEVEL] xCount 메시지
                    sb.Append('[').Append(r.Time.ToString("HH:mm:ss")).Append("] [")
                      .Append(MapLevel(r.Type)).Append(']');
                    if (r.Count > 1) sb.Append(" x").Append(r.Count);
                    sb.Append(' ').AppendLine(r.Message);

                    // 스택 트레이스 출력
                    if (!string.IsNullOrEmpty(r.Stack))
                    {
                        if (_multilineStackTrace)
                        {
                            // 원본 줄바꿈 유지
                            sb.AppendLine(r.Stack.TrimEnd());
                        }
                        else
                        {
                            // 한 줄로 평탄화 (줄바꿈을 구분자(|)로 변경)
                            string oneLine = r.Stack.Replace("\r", "").Replace("\n", " | ");
                            sb.AppendLine(oneLine);
                        }
                    }
                }
            }

            // UTF-8 (BOM 없이)로 파일 쓰기
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[ConsoleExport] Saved: " + path);

            // 저장 경로를 UI에 표시
            _exportText.text = path;
        }
        catch (Exception ex)
        {
            Debug.LogError("[ConsoleExport] Export failed: " + ex.Message);
        }
    }

    /// <summary>
    /// 버튼에 연결(선택): 현재 세션 버퍼 삭제
    /// - 이후부터 새로 발생하는 로그만 기록
    /// </summary>
    public void ClearSession()
    {
        lock (_lockObj)
        {
            _rows.Clear();
        }
        Debug.Log("[ConsoleExport] Session cleared");
    }

    /// <summary>
    /// LogType → 텍스트 레벨 코드 변환
    /// </summary>
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

    /// <summary>
    /// 로그 파일을 저장할 기본 폴더 경로 반환
    /// - Windows + _preferDesktop = true → 바탕화면
    /// - 그 외에는 Application.persistentDataPath
    /// </summary>
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
            catch
            {
                // 바탕화면 접근 실패 시 예외 무시하고 아래 경로로
            }
        }
        return Application.persistentDataPath;
    }
}
