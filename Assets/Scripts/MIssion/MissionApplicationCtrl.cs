using UnityEngine;

public class MissionApplicationCtrl : MonoBehaviour
{
    // °øÅë ÇÁ¸®ÇÈ½º (¹«Áö°³ "MISSION ")
    private const string MissionPrefix =
        "<color=#FF0000>M</color>" +
        "<color=#FFA500>I</color>" +
        "<color=#FFFF00>S</color>" +
        "<color=#00FF00>S</color>" +
        "<color=#0000FF>I</color>" +
        "<color=#000080>O</color>" +
        "<color=#800080>N</color> ";

    // 1ÄÆ: ¼¼ °¡Áö ·£´ý ¹®±¸
    private string _missionMessage00_0 = MissionPrefix + "1ÄÆ: »ìÂ¦ ¹Ì¼Ò~";
    private string _missionMessage00_1 = MissionPrefix + "1ÄÆ: ¼öÁÝ°Ô ¹Ì¼Ò~";
    private string _missionMessage00_2 = MissionPrefix + "1ÄÆ: »óÅ­ÇÏ°Ô ¿ô±â~";

    // 2ÄÆ: ¼¼ °¡Áö ·£´ý ¹®±¸
    private string _missionMessage01_0 = MissionPrefix + "2ÄÆ: ³î¶õ Ç¥Á¤~!";
    private string _missionMessage01_1 = MissionPrefix + "2ÄÆ: ±ôÂ¦! ³î¶õ ´«~";
    private string _missionMessage01_2 = MissionPrefix + "2ÄÆ: ¿Í! °¨Åº Ç¥Á¤~";

    // 3ÄÆ: ¼¼ °¡Áö ·£´ý ¹®±¸
    private string _missionMessage02_0 = MissionPrefix + "3ÄÆ: ¼Õ°¡¶ô ÇÏÆ®~";
    private string _missionMessage02_1 = MissionPrefix + "3ÄÆ: µÎ ¼Õ ÇÏÆ®~";
    private string _missionMessage02_2 = MissionPrefix + "3ÄÆ: º¼ Âô±ß ÇÏÆ®~";

    // 4ÄÆ: ¼¼ °¡Áö ·£´ý ¹®±¸
    private string _missionMessage03_0 = MissionPrefix + "4ÄÆ: ÀÚÀ¯ Æ÷Áî~~";
    private string _missionMessage03_1 = MissionPrefix + "4ÄÆ: Á¦ÀÏ ¿ô±ä Æ÷Áî!";
    private string _missionMessage03_2 = MissionPrefix + "4ÄÆ: ´ÜÃ¼ Æ÷Áî ÂûÄ¬!";

    /// <summary>
    /// È£Ãâ¿ë ÇÔ¼ö : Step
    /// </summary>
    /// <param name="stepIndex"></param>
    /// <returns></returns>
    public string GetRandomMissionMessage(int stepIndex)
    {
        switch (stepIndex)
        {
            case 0:
                return GetRandomFrom(
                    _missionMessage00_0,
                    _missionMessage00_1,
                    _missionMessage00_2);

            case 1:
                return GetRandomFrom(
                    _missionMessage01_0,
                    _missionMessage01_1,
                    _missionMessage01_2);

            case 2:
                return GetRandomFrom(
                    _missionMessage02_0,
                    _missionMessage02_1,
                    _missionMessage02_2);

            case 3:
                return GetRandomFrom(
                    _missionMessage03_0,
                    _missionMessage03_1,
                    _missionMessage03_2);

            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// ·£´ýÀ¸·Î µ¹¸±³à¼®
    /// </summary>
    /// <param name="candidates"></param>
    /// <returns></returns>
    private string GetRandomFrom(params string[] candidates)
    {
        if (candidates == null || candidates.Length == 0)
            return string.Empty;

        int index = Random.Range(0, candidates.Length);
        return candidates[index];
    }
}
