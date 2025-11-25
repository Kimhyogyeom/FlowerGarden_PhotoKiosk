using UnityEngine;

/// <summary>
/// 미션 텍스트를 관리하는 컨트롤러
/// - 각 컷(1~8컷)에 대해 3개씩 미션 문구를 가지고 있음
/// - GetRandomMissionMessage(stepIndex)를 호출하면
///   해당 컷에 맞는 문구 중 하나를 랜덤으로 반환
/// </summary>
public class MissionApplicationCtrl : MonoBehaviour
{
    // 공통 프리픽스 (무지개 "MISSION ")
    private const string MissionPrefix = "";
    // "<color=#FF0000>M</color>" +
    // "<color=#FFA500>I</color>" +
    // "<color=#FFFF00>S</color>" +
    // "<color=#00FF00>S</color>" +
    // "<color=#0000FF>I</color>" +
    // "<color=#000080>O</color>" +
    // "<color=#800080>N</color> ";

    // 1컷: 세 가지 랜덤 문구
    private string _missionMessage00_0 = MissionPrefix + "1컷: 살짝 미소~";
    private string _missionMessage00_1 = MissionPrefix + "1컷: 수줍게 미소~";
    private string _missionMessage00_2 = MissionPrefix + "1컷: 상큼하게 웃기~";

    // 2컷: 세 가지 랜덤 문구
    private string _missionMessage01_0 = MissionPrefix + "2컷: 놀란 표정~!";
    private string _missionMessage01_1 = MissionPrefix + "2컷: 깜짝! 놀란 눈~";
    private string _missionMessage01_2 = MissionPrefix + "2컷: 와! 감탄 표정~";

    // 3컷: 세 가지 랜덤 문구
    private string _missionMessage02_0 = MissionPrefix + "3컷: 손가락 하트~";
    private string _missionMessage02_1 = MissionPrefix + "3컷: 두 손 하트~";
    private string _missionMessage02_2 = MissionPrefix + "3컷: 볼 찡긋 하트~";

    // 4컷: 세 가지 랜덤 문구
    private string _missionMessage03_0 = MissionPrefix + "4컷: 자유 포즈~~";
    private string _missionMessage03_1 = MissionPrefix + "4컷: 제일 웃긴 포즈!";
    private string _missionMessage03_2 = MissionPrefix + "4컷: 단체 포즈 찰칵!";

    // 5컷: 세 가지 랜덤 문구
    private string _missionMessage04_0 = MissionPrefix + "5컷: 귀엽게 브이~";
    private string _missionMessage04_1 = MissionPrefix + "5컷: 양손 브이 포즈~";
    private string _missionMessage04_2 = MissionPrefix + "5컷: 살짝 고개 갸우뚱~";

    // 6컷: 세 가지 랜덤 문구
    private string _missionMessage05_0 = MissionPrefix + "6컷: 시크하게 정면 응시~";
    private string _missionMessage05_1 = MissionPrefix + "6컷: 살짝 옆모습 포즈~";
    private string _missionMessage05_2 = MissionPrefix + "6컷: 턱 괴고 생각하는 표정~";

    // 7컷: 세 가지 랜덤 문구
    private string _missionMessage06_0 = MissionPrefix + "7컷: 서로를 바라보기~";
    private string _missionMessage06_1 = MissionPrefix + "7컷: 한 사람만 카메라 보기~";
    private string _missionMessage06_2 = MissionPrefix + "7컷: 서로 마주 보고 웃기~";

    // 8컷: 세 가지 랜덤 문구
    private string _missionMessage07_0 = MissionPrefix + "8컷: 마지막은 단체 포즈!";
    private string _missionMessage07_1 = MissionPrefix + "8컷: 다 같이 점프하는 느낌~";
    private string _missionMessage07_2 = MissionPrefix + "8컷: 오늘 최고의 표정으로!";

    /// <summary>
    /// 촬영 단계(stepIndex)에 맞는 미션 문구를 랜덤으로 하나 반환
    /// - stepIndex: 0 → 1컷, 1 → 2컷, ... , 7 → 8컷
    /// - 그 외 값은 빈 문자열 반환
    /// </summary>
    /// <param name="stepIndex">0~7 사이의 촬영 단계 인덱스</param>
    /// <returns>해당 컷의 미션 문구(랜덤) 또는 빈 문자열</returns>
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

            case 4:
                return GetRandomFrom(
                    _missionMessage04_0,
                    _missionMessage04_1,
                    _missionMessage04_2);

            case 5:
                return GetRandomFrom(
                    _missionMessage05_0,
                    _missionMessage05_1,
                    _missionMessage05_2);

            case 6:
                return GetRandomFrom(
                    _missionMessage06_0,
                    _missionMessage06_1,
                    _missionMessage06_2);

            case 7:
                return GetRandomFrom(
                    _missionMessage07_0,
                    _missionMessage07_1,
                    _missionMessage07_2);

            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 전달된 문자열 후보들 중 하나를 랜덤으로 선택해서 반환
    /// </summary>
    /// <param name="candidates">랜덤 선택 대상 문자열 배열</param>
    /// <returns>랜덤으로 고른 문자열, 없으면 빈 문자열</returns>
    private string GetRandomFrom(params string[] candidates)
    {
        if (candidates == null || candidates.Length == 0)
            return string.Empty;

        int index = Random.Range(0, candidates.Length);
        return candidates[index];
    }
}
