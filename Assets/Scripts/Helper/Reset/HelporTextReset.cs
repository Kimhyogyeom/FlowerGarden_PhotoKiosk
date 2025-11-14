using TMPro;
using UnityEngine;

/// <summary>
/// 헬프/안내 텍스트들을 한 번에 초기화하는 스크립트
/// - 등록된 TextMeshProUGUI 배열의 내용을 모두 빈 문자열로 리셋
/// </summary>
public class HelporTextReset : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI[] _resetTexts;   // 리셋 대상 텍스트들

    /// <summary>
    /// 등록된 모든 텍스트를 빈 문자열로 초기화
    /// </summary>
    public void ResetTexts()
    {
        for (int i = 0; i < _resetTexts.Length; i++)
        {
            _resetTexts[i].text = "";
        }
    }
}
