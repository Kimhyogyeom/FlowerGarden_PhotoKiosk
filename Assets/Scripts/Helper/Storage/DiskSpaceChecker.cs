using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 디스크 용량 체크 유틸리티
/// - 버튼 클릭 시 현재 디스크 사용량 정보를 문자열로 만들어 UI(Text / TextMeshPro)에 표시
/// - Windows에서는 각 드라이브(C:, D: …)별로 Total / Used / Free 표시
/// - 그 외 플랫폼에서는 persistentDataPath 기준 스토리지 한 줄만 표시(더미 처리)
/// </summary>
public class DiskSpaceChecker : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private TextMeshProUGUI _labelTMP;          // TMP Text 출력용
    [SerializeField] private UnityEngine.UI.Text _labelUGUI;     // UGUI Text 출력용

    [Header("Options")]
    [SerializeField] private bool _onlyFixedDrives = true;       // (Windows) 고정 드라이브(내장 디스크)만 표시할지 여부
    [SerializeField] private bool _markPersistent = true;        // persistentDataPath가 있는 드라이브에 [Persistent] 표시 여부

    [Header("Object Setting")]
    [SerializeField] private Button _diskSpaceCheckerButton;     // 용량 체크 버튼

    private void Awake()
    {
        // 버튼 클릭 시 디스크 정보 표시
        _diskSpaceCheckerButton.onClick.AddListener(ShowOnClick);
    }

    /// <summary>
    /// 버튼 OnClick에 연결되는 메서드
    /// - BuildReport()로 문자열을 만들고, TMP/UGUI 텍스트에 출력
    /// </summary>
    public void ShowOnClick()
    {
        string report = BuildReport();
        if (_labelTMP) _labelTMP.text = report;
        if (_labelUGUI) _labelUGUI.text = report;
        // Debug.Log(report); // 필요하면 콘솔에도 출력
    }

    /// <summary>
    /// 디스크 사용량 리포트 문자열 생성
    /// </summary>
    private string BuildReport()
    {
        try
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Windows: 드라이브 목록 조회
            var drives = DriveInfo.GetDrives()
                .Where(d =>
                {
                    if (!d.IsReady) return false;                       // 준비 안 된 드라이브는 제외
                    if (_onlyFixedDrives && d.DriveType != DriveType.Fixed)
                        return false;                                   // 옵션에 따라 고정 디스크만 사용
                    return true;
                })
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // persistentDataPath가 위치한 루트 드라이브 (예: "C:\")
            string persistentRoot = GetRootDrive(Application.persistentDataPath);

            var sb = new StringBuilder();
            sb.AppendLine("Disk Space:");
            if (drives.Count == 0)
            {
                sb.AppendLine("No drives found.");
                return sb.ToString();
            }

            foreach (var d in drives)
            {
                long total = SafeTotalSize(d);
                long free = SafeAvailableFreeSpace(d);
                long used = Math.Max(0, total - free);

                string line = $"{d.Name}  Total {Human(total)}, Used {Human(used)}, Free {Human(free)}";
                // persistentDataPath와 동일한 드라이브면 [Persistent] 태그 추가
                if (_markPersistent && !string.IsNullOrEmpty(persistentRoot) &&
                    string.Equals(d.Name, persistentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    line += "  [Persistent]";
                }
                sb.AppendLine(line);
            }
            return sb.ToString();
#else
            // Non-Windows: persistentDataPath가 위치한 스토리지만 간단히 표시
            long total, free;
            HumanizeForUnknownFS(Application.persistentDataPath, out total, out free);
            long used = Math.Max(0, total - free);

            var sb = new StringBuilder();
            sb.AppendLine("Disk Space:");
            sb.AppendLine($"Storage  Total {Human(total)}, Used {Human(used)}, Free {Human(free)}  [Persistent]");
            return sb.ToString();
#endif
        }
        catch (Exception ex)
        {
            // 예외 발생 시 메시지 반환
            return "Disk Space: error\n" + ex.Message;
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    /// <summary>
    /// TotalSize 접근 시 예외가 날 수 있으므로 안전하게 감싸는 함수
    /// </summary>
    private static long SafeTotalSize(DriveInfo d)
    {
        try { return d.TotalSize; } catch { return 0; }
    }

    /// <summary>
    /// AvailableFreeSpace 접근 시 예외가 날 수 있으므로 안전하게 감싸는 함수
    /// </summary>
    private static long SafeAvailableFreeSpace(DriveInfo d)
    {
        try { return d.AvailableFreeSpace; } catch { return 0; }
    }

    /// <summary>
    /// 경로에서 루트 드라이브(예: "C:\")만 추출
    /// </summary>
    private static string GetRootDrive(string path)
    {
        try
        {
            string root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root)) return null;
            if (!root.EndsWith("\\")) root += "\\";
            return root;
        }
        catch { return null; }
    }
#else
    /// <summary>
    /// Non-Windows에서의 용량 값 구하기용 더미 함수
    /// - 실제 플랫폼별 용량 조회(statvfs 등)는 별도 구현 필요
    /// - 현재는 0을 반환해도 포맷이 깨지지 않도록만 처리
    /// </summary>
    private static void HumanizeForUnknownFS(string anyPath, out long totalBytes, out long freeBytes)
    {
        // 현재는 실제 값을 알 수 없으므로 0으로 설정
        totalBytes = 0;
        freeBytes  = 0;
    }
#endif

    /// <summary>
    /// 바이트 단위를 사람이 읽기 쉬운 문자열(XX.X MB / GB 등)로 변환
    /// </summary>
    private static string Human(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        if (bytes >= TB) return (bytes / (double)TB).ToString("F1") + " TB";
        if (bytes >= GB) return (bytes / (double)GB).ToString("F1") + " GB";
        if (bytes >= MB) return (bytes / (double)MB).ToString("F1") + " MB";
        if (bytes >= KB) return (bytes / (double)KB).ToString("F1") + " KB";
        return bytes + " B";
    }
}
