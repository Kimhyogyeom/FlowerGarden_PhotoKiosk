using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DiskSpaceChecker : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private TextMeshProUGUI _labelTMP;          // TMP Text
    [SerializeField] private UnityEngine.UI.Text _labelUGUI;     // UGUI Text

    [Header("Options")]
    [SerializeField] private bool _onlyFixedDrives = true;       // Windows: fixed drives only (C:, D:)
    [SerializeField] private bool _markPersistent = true;        // Mark drive that holds persistentDataPath

    [Header("Object Setting")]
    [SerializeField] private Button _diskSpaceCheckerButton;

    private void Awake()
    {
        _diskSpaceCheckerButton.onClick.AddListener(ShowOnClick);
    }

    // Button → OnClick에 연결할 메서드
    public void ShowOnClick()
    {
        string report = BuildReport();
        if (_labelTMP) _labelTMP.text = report;
        if (_labelUGUI) _labelUGUI.text = report;
        // Debug.Log(report);
    }

    private string BuildReport()
    {
        try
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var drives = DriveInfo.GetDrives()
                .Where(d =>
                {
                    if (!d.IsReady) return false;
                    if (_onlyFixedDrives && d.DriveType != DriveType.Fixed) return false;
                    return true;
                })
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

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
                if (_markPersistent && !string.IsNullOrEmpty(persistentRoot) &&
                    string.Equals(d.Name, persistentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    line += "  [Persistent]";
                }
                sb.AppendLine(line);
            }
            return sb.ToString();
#else
            // Non-Windows: show only the drive that backs persistentDataPath
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
            return "Disk Space: error\n" + ex.Message;
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static long SafeTotalSize(DriveInfo d)
    {
        try { return d.TotalSize; } catch { return 0; }
    }
    private static long SafeAvailableFreeSpace(DriveInfo d)
    {
        try { return d.AvailableFreeSpace; } catch { return 0; }
    }
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
    private static void HumanizeForUnknownFS(string anyPath, out long totalBytes, out long freeBytes)
    {
        // Placeholder for non-Windows. Many platforms need native calls (statvfs) to get real values.
        // Return zeros so formatting still works.
        totalBytes = 0;
        freeBytes  = 0;
    }
#endif

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
